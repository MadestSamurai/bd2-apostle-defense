using System.Diagnostics;
using System.Text.Json;
using BD2ApostleDefense.Compatibility;
using SharpMonoInjector;
namespace BD2ApostleDefense;
public static class JsonFiles
{
    public static readonly JsonSerializerOptions Options=new(){IncludeFields=true,WriteIndented=true};
    public static T? Read<T>(string path)where T:class{try{using var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);return JsonSerializer.Deserialize<T>(f,Options);}catch(FileNotFoundException){return null;}catch(DirectoryNotFoundException){return null;}catch(Exception e)when(e is IOException or UnauthorizedAccessException or JsonException){IoDiagnostics.Record(Path.GetDirectoryName(Path.GetFullPath(path))!,"read",path,e);return null;}}
    public static void Write<T>(string path,T value)=>AtomicFiles.Write(path,f=>JsonSerializer.Serialize(f,value,Options));
    public static T Clone<T>(T value)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value,Options),Options)!;
}
public sealed record GameProcess(int Id,long Start,string File);
public interface IClientPort
{
    GameProcess? Find();Snapshot? Read();void Write(Control command);
    Task ConnectAsync(Action<string> progress,CancellationToken cancellation);
}
public sealed class GamePort:IClientPort
{
    private readonly string root;
    public GamePort(string? root=null){this.root=root??Identity.Root;}
    public GameProcess? Find(){var all=Process.GetProcessesByName("BrownDust II");try{if(all.Length>1)throw new InvalidOperationException("检测到多个游戏进程，请只保留一个。");if(all.Length==0)return null;var p=all[0];return new(p.Id,p.StartTime.ToUniversalTime().Ticks,p.MainModule?.FileName??throw new InvalidOperationException("无法读取游戏路径，请使用与游戏相同的权限。"));}finally{foreach(var p in all)p.Dispose();}}
    public Snapshot? Read()=>JsonFiles.Read<Snapshot>(Path.Combine(root,"snapshot.json"));
    public void Write(Control command)=>JsonFiles.Write(Path.Combine(root,"control.json"),command);
    private sealed class Connection
    {public int ProcessId{get;set;}public long Start{get;set;}public string Fingerprint{get;set;}="";public long Address{get;set;}public string Error{get;set;}="";}
    public async Task ConnectAsync(Action<string> progress,CancellationToken cancellation)
    {
        var game=Find()??throw new InvalidOperationException("请先启动并登录游戏。");var path=Path.Combine(root,"connection.json");var old=JsonFiles.Read<Connection>(path);
        if(File.Exists(path)&&old==null)throw new InvalidDataException("连接记录暂时无法读取，请关闭占用它的程序后重试。");
        if(old?.ProcessId==game.Id&&old.Start==game.Start)
        {
            if(old.Fingerprint!=HookCompiler.ToolFingerprint)throw new InvalidOperationException("游戏已加载另一版本组件，请正常重启游戏后连接。");
            if(old.Error!="")throw new InvalidOperationException(old.Error+"；请正常重启游戏后重试。");
            var status=JsonFiles.Read<RuntimeStatus>(Path.Combine(root,"runtime.json"));
            if(status?.ProcessId==game.Id&&status.ProcessStart==game.Start&&status.State=="error")throw new InvalidOperationException(status.Error);
            progress("已连接组件，等待游戏状态");return;
        }
        progress("解析本机客户端接口并准备组件…");
        string managed=Path.Combine(Path.GetDirectoryName(game.File)!,Path.GetFileNameWithoutExtension(game.File)+"_Data","Managed");
        var prepared=await Task.Run(()=>HookCompiler.Prepare(managed),cancellation);JsonFiles.Write(Path.Combine(root,"compatibility.json"),prepared.Report);
        cancellation.ThrowIfCancellationRequested();if(Find()!=game)throw new InvalidOperationException("游戏进程已经变化，请重新连接。");
        using(var p=Process.GetProcessById(game.Id))if(!p.Modules.Cast<ProcessModule>().Any(m=>m.ModuleName.Equals("mono-2.0-bdwgc.dll",StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("游戏引擎仍在加载，请稍后连接。");
        var current=new Connection{ProcessId=game.Id,Start=game.Start,Fingerprint=HookCompiler.ToolFingerprint};JsonFiles.Write(path,current);progress("连接组件…");
        try{await Task.Run(()=>{using var injector=new Injector(game.Id);current.Address=injector.Inject(prepared.Payload,"BD2ApostleDefense.Runtime","Loader","Load").ToInt64();},CancellationToken.None);JsonFiles.Write(path,current);}
        catch(Exception e){current.Error=e.Message;JsonFiles.Write(path,current);throw;}
        cancellation.ThrowIfCancellationRequested();
    }
}
