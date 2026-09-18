using System.IO;
using BD2ApostleDefense.Localization;
using System.Windows;
using BD2ApostleDefense.Compatibility;
namespace BD2ApostleDefense.Desktop;
public partial class App:Application
{
 private Mutex? mutex;
 protected override void OnStartup(StartupEventArgs e)
 {
  base.OnStartup(e);Ui.Catalog=new(LanguagePreference.Read(Identity.Root));
  if(e.Args.Length==3&&e.Args[0]=="--check-client")
  {try{var p=HookCompiler.Prepare(e.Args[1]);Directory.CreateDirectory(e.Args[2]);File.WriteAllBytes(Path.Combine(e.Args[2],Identity.Runtime+".dll"),p.Payload);JsonFiles.Write(Path.Combine(e.Args[2],"compatibility.json"),p.Report);Shutdown();}catch(Exception ex){JsonFiles.Write(Path.Combine(e.Args[2],"error.json"),new{error=ex.ToString()});Shutdown(1);}return;}
  if(e.Args.Length==2&&e.Args[0]=="--identity"){JsonFiles.Write(e.Args[1],new{version=AppVersion.Current,runtime=Identity.Runtime,fingerprint=HookCompiler.ToolFingerprint});Shutdown();return;}
  try
  {
   if(e.Args.Length is 2 or 3&&e.Args[0]=="--smoke"){string root=Path.GetFullPath(e.Args[1]);var fixture=e.Args.Length==3?(JsonFiles.Read<Snapshot>(Path.GetFullPath(e.Args[2]))??throw new InvalidOperationException("无法读取验收盘面")):null;new MainWindow(new DemoPort(fixture),root,true).Show();return;}
   if(e.Args.Length!=0){Shutdown(2);return;}
   mutex=new Mutex(true,@"Local\BD2ApostleDefense-v1",out bool first);if(!first){MessageBox.Show(Ui.Text("工具已打开，请检查任务栏。"),Ui.Text("使徒防守助手"));Shutdown();return;}
   new MainWindow(new GamePort(),Identity.Root).Show();
  }catch(Exception ex){MessageBox.Show(Ui.Text(ex.Message),Ui.Text("启动失败"),MessageBoxButton.OK,MessageBoxImage.Error);Shutdown(1);}
 }
 protected override void OnExit(ExitEventArgs e){mutex?.Dispose();base.OnExit(e);}
}
