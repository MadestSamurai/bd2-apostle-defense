using BD2ApostleDefense;

internal static class RecoveryTests
{
 public static void Run(Action<bool,string> check,long now)
 {
  var playing=PublicFixtures.Board(now);var planner=new Planner();
  var lobby=JsonFiles.Clone(playing);lobby.Stage="lobby";lobby.Room="";lobby.Ready=false;
  var ended=JsonFiles.Clone(playing);ended.Stage="result";ended.Dead=true;
  lobby.Blocker="EventPopupUI";lobby.EventPopupOpen=true;lobby.CanDismissEvent=true;
  check(GameFlow.Receipt("settle",ended,lobby),"lobby popup does not invalidate a completed exit");
  check(planner.Decide(lobby,new()).Kind=="close-event","dismissible lobby event resumes flow");
  Control C(Snapshot s,string kind)=>new(){Enabled=true,ProcessId=s.ProcessId,ProcessStart=s.ProcessStart,Owner=new('b',32),Account=s.Account,Room=s.Room,BoardKey=s.BoardKey,SnapshotAt=s.At,Expires=s.At+TimeSpan.FromSeconds(10).Ticks,Action=new(){Kind=kind}};
  check(Guards.Reject(C(lobby,"close-event"),lobby,now)=="","native lobby event close guarded");
  foreach(string stage in new[]{"playing","ended","result","confirm-exit","matching"})
  {var x=JsonFiles.Clone(lobby);x.Stage=stage;check(Guards.Reject(C(x,"close-event"),x,now)!="","never dismiss event outside lobby: "+stage);}
  lobby.CanDismissEvent=false;lobby.Blocker="NetworkErrorPopupUI";
  check(planner.Decide(lobby,new()).Kind=="wait","unrelated popup waits without arbitrary close");
  check(Guards.Reject(C(lobby,"close-event"),lobby,now)!="","unknown overlay blocks close command");
  check(GameFlow.Receipt("settle",ended,lobby),"network modal cannot undo lobby arrival");
  check(!GameFlow.Receipt("close-event",ended,lobby),"event remains pending while native surface remains");
  lobby.EventPopupOpen=false;check(GameFlow.Receipt("close-event",ended,lobby),"event disappearance receipt");
  check(GameFlow.Superseded("match-start",playing,lobby),"cancelled matching returns to lobby instead of timeout");
  lobby.Stage="match-failed";
  foreach(string kind in new[]{"move","summon","sell","upgrade","match-start"})check(GameFlow.Superseded(kind,playing,lobby),"match failure supersedes "+kind);
  lobby.Stage="waiting";check(!GameFlow.Superseded("match-start",playing,lobby),"loading does not invent matching cancellation");
  check(GameFlow.TimeoutSeconds("move")==2&&GameFlow.TimeoutSeconds("settle")==30,"local move and server exit have distinct deadlines");
  var backoff=new RecoveryBackoff();backoff.Sync(playing);var move=new Decision{Kind="move",UnitIndex=1};
  foreach(int delay in new[]{2,4,8,16,30,30})
  {backoff.Record(move,false,now);check(!backoff.CanMove(1,now+TimeSpan.FromSeconds(delay).Ticks-1),"failed move cooling "+delay);check(backoff.CanMove(1,now+TimeSpan.FromSeconds(delay).Ticks),"failed move retry allowed "+delay);}
  check(backoff.CanMove(2,now)&&backoff.Ready(new(){Kind="summon"},now),"move failure leaves other units and economy available");
  backoff.Record(move,true,now);check(backoff.CanMove(1,now),"success resets recovery delay");
  backoff.Record(move,false,now);var otherRoom=JsonFiles.Clone(playing);otherRoom.Room="next";backoff.Sync(otherRoom);check(backoff.CanMove(1,now),"unit index reuse in new room does not inherit delay");
  foreach(string result in new[]{"timeout","retry"})
  {
   var port=new Port();var controller=new Controller(port);var s=JsonFiles.Clone(playing);controller.Start(s,new(),now);controller.Poll(s,now);
   var sent=port.Last!;check(sent.Action.Kind=="move","recovery scenario begins with movement");
   s.Owner=sent.Owner;s.Ack=sent.Command;s.AckResult=result;s.State="ready";s.At=now+TimeSpan.FromSeconds(3).Ticks;controller.Poll(s,s.At);
   check(controller.Running&&port.Last!.Command==0,result+" keeps automation intent");
   s.Gold=45;s.CanSummon=true;s.Units=s.Units.Where(u=>u.Index==sent.Action.UnitIndex).ToArray();s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);
   check(port.Last!.Action.Kind=="summon",result+" cannot starve next summon behind failed placement");
   sent=port.Last!;s.Ack=sent.Command;s.AckResult="superseded";s.Stage="result";s.Dead=true;s.Ready=false;s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);
   check(controller.Running&&port.Last!.Action.Kind=="settle",result+" later loss still exits automatically");
   sent=port.Last!;s.Ack=sent.Command;s.AckResult="timeout";s.Exiting=true;s.Stage="settling";s.At+=TimeSpan.FromSeconds(31).Ticks;controller.Poll(s,s.At);s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);
   check(controller.Running&&port.Last!.Command==0,"slow native exit not submitted twice");
   s.Exiting=false;s.Stage="lobby";s.Room="";s.Dead=false;s.Blocker="EventPopupUI";s.EventPopupOpen=true;s.CanDismissEvent=true;s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);
   check(port.Last!.Action.Kind=="close-event","late lobby recovery dismisses only native event");
   sent=port.Last!;s.Ack=sent.Command;s.AckResult="ok";s.CanDismissEvent=false;s.EventPopupOpen=false;s.Blocker="";s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);
   check(port.Last!.Action.Kind=="refresh","recovered settlement refreshes server achievements");
   controller.Stop();s.At+=TimeSpan.FromSeconds(60).Ticks;controller.Poll(s,s.At);check(!port.Last!.Enabled&&!controller.Running,"manual stop cancels all recovery");
  }
  var fatalPort=new Port();var fatal=new Controller(fatalPort);var broken=JsonFiles.Clone(playing);fatal.Start(broken,new(),now);fatal.Poll(broken,now);broken.Owner=fatalPort.Last!.Owner;broken.State="error";fatal.Poll(broken,now);
  check(!fatal.Running,"unclassified component errors still stop safely");
 }
 private sealed class Port:IClientPort
 {
  public Control? Last;public GameProcess? Find()=>new(10,20,"fake");public Snapshot? Read()=>null;
  public void Write(Control c)=>Last=JsonFiles.Clone(c);
  public Task ConnectAsync(Action<string> progress,CancellationToken token)=>Task.CompletedTask;
 }
}
