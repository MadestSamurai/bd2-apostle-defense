using BD2ApostleDefense;
using System.Text;
internal static class AtomicFilesTests
{
 public static void Run(Action<bool,string> check)
 {
  string root=Path.Combine(AppContext.BaseDirectory,"test-data","io-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);string path=Path.Combine(root,"control.json");
  JsonFiles.Write(path,new Control{Command=1});
  using(var reader=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))
  {JsonFiles.Write(path,new Control{Command=2});check(JsonFiles.Read<Control>(path)!.Command==2,"atomic publish works with normal live reader");}
  using(var held=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
  {
   Exception? failure=null;var timer=System.Diagnostics.Stopwatch.StartNew();try{JsonFiles.Write(path,new Control{Command=3});}catch(IOException e){failure=e;}
   check(failure!=null&&failure.Message.Contains(path)&&failure.InnerException!=null,"locked target retains path and native exception");
   check(timer.Elapsed<TimeSpan.FromSeconds(3),"file retries are bounded");
   check(JsonFiles.Read<Control>(path)!.Command==2,"failed replacement preserves last complete command");
  }
  check(Directory.GetFiles(root,"*.tmp").Length==0,"failed atomic publication leaves no temporary command");
  JsonFiles.Write(path,new Control{Command=4});check(JsonFiles.Read<Control>(path)!.Command==4,"publication resumes once external lock is released");
  try{AtomicFiles.Write(path,f=>{f.WriteByte(123);throw new InvalidDataException("synthetic serialization failure");});}catch(IOException){}
  check(JsonFiles.Read<Control>(path)!.Command==4,"partial serialization never overwrites valid command");
  check(Directory.GetFiles(root,"*.tmp").Length==0,"serialization failure cleans temporary file");
  var errors=new System.Collections.Concurrent.ConcurrentQueue<Exception>();
  Parallel.For(0,40,i=>{try{if(i%2==0)JsonFiles.Write(path,new Control{Command=10+i});else{var c=JsonFiles.Read<Control>(path);if(c==null)throw new Exception("partial/missing command");}}catch(Exception e){errors.Enqueue(e);}});
  check(errors.IsEmpty,"concurrent readers and publishers observe whole JSON documents");
  bool ran=false;check(!IoDiagnostics.Attempt(root,"first","blocked.json",()=>throw new UnauthorizedAccessException("synthetic deny")),"failed diagnostic channel is isolated");
  IoDiagnostics.Attempt(root,"second","snapshot.json",()=>ran=true);check(ran,"later snapshot/heartbeat channel still runs after failure");
  string log=Directory.GetFiles(root,"io-errors-*.log").Single();long size=new FileInfo(log).Length;
  for(int i=0;i<10;i++)IoDiagnostics.Attempt(root,"first","blocked.json",()=>throw new UnauthorizedAccessException("synthetic deny"));
  check(new FileInfo(log).Length==size,"repeated I/O failures do not flood the log");
  var text=File.ReadAllText(log);check(text.Contains("blocked.json")&&text.Contains("HResult=0x")&&text.Contains("UnauthorizedAccessException")&&text.Contains("runtime="),"I/O evidence includes path, code, exception and component identity");
 }
}
