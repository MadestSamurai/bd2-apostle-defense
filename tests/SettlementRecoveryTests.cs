using BD2ApostleDefense;
internal static class SettlementRecoveryTests
{
 public static void Run(Action<bool,string> check,long now)
 {
  long S(double sec)=>now+TimeSpan.FromSeconds(sec).Ticks;
  Snapshot End(){var s=PublicFixtures.Board(now);s.Stage="ended";s.RoomEnded=true;s.Win=true;s.NetworkHold=true;return s;}
  Control C(Snapshot s,string kind)=>new(){Enabled=true,ProcessId=s.ProcessId,ProcessStart=s.ProcessStart,Owner=new('b',32),Account=s.Account,Room=s.Room,BoardKey=s.BoardKey,SnapshotAt=s.At,Expires=s.At+TimeSpan.FromSeconds(10).Ticks,Action=new(){Kind=kind}};
  foreach(var pair in new[]{("ended","open-result"),("result","settle"),("confirm-exit","confirm-exit")})
  {
   var s=End();s.Stage=pair.Item1;
   check(!GameFlow.NeedsBattleConnection(s)&&!GameFlow.NetworkBlocked(s),"completed room needs no battle socket: "+s.Stage);
   check(Guards.Reject(C(s,pair.Item2),s,now)=="","normal end disconnect permits "+pair.Item2);
   s.RoomEnded=false;s.Win=false;s.Dead=true;
   check(GameFlow.NetworkBlocked(s)&&Guards.Reject(C(s,pair.Item2),s,now)!="","own death in unfinished disconnected room retains network protection: "+pair.Item2);
   s.NetworkHold=false;check(Guards.Reject(C(s,pair.Item2),s,now)=="","own death with healthy room still uses native exit: "+pair.Item2);
  }
  var port=new Port();var controller=new Controller(port);var ended=End();controller.Start(ended,new(),now);controller.Poll(ended,now);
  var sent=port.Last!;check(sent.Action.Kind=="open-result","terminal disconnect cannot prevent opening result");
  ended.Owner=sent.Owner;ended.AcceptedCommand=sent.Command;ended.At=S(.2);controller.Poll(ended,ended.At);
  check(port.Last!.CancelThrough==0,"accepted result opening is not prematurely cancelled");
  ended.Stage="result";ended.Ack=sent.Command;ended.AckResult="ok";ended.AcceptedCommand=0;ended.At=S(.3);controller.Poll(ended,ended.At);
  ended.At=S(1);controller.Poll(ended,ended.At);sent=port.Last!;check(sent.Action.Kind=="settle","terminal result automatically proceeds to exit");
  ended.Stage="settling";ended.Exiting=true;ended.Ack=sent.Command;ended.AckResult="timeout";ended.At=S(35);controller.Poll(ended,ended.At);ended.At=S(40);controller.Poll(ended,ended.At);
  check(port.Last!.Command==0&&controller.Running,"submitted native exit is never clicked twice");
  ended.Stage="lobby";ended.Room="";ended.Exiting=false;ended.At=S(41);controller.Poll(ended,ended.At);
  check(port.Last!.Action.Kind=="refresh","ended room returns to server achievement read without battle reconnect");
  var before=End();var after=End();after.Stage="lobby";after.Blocker="EventPopupUI";
  foreach(string kind in new[]{"open-result","settle","confirm-exit"})check(GameFlow.Receipt(kind,before,after),"lobby with modal completes "+kind);
  after.Stage="settling";after.Exiting=true;check(GameFlow.Receipt("open-result",before,after),"native exit already submitted completes obsolete opening");
  after.Room="next-room";after.Exiting=false;after.Stage="playing";
  foreach(string kind in new[]{"open-result","settle","confirm-exit"})check(GameFlow.Superseded(kind,before,after),"new room supersedes old settlement: "+kind);
  after.Account=new('c',64);check(!GameFlow.Superseded("settle",before,after),"account change cannot be mistaken for settlement completion");
  // Reproduce the supplied four-hour wait; cancellation must be acknowledged before reuse.
  port=new();controller=new(port);ended=End();controller.Start(ended,new(),now);controller.Poll(ended,now);sent=port.Last!;
  ended.Owner=sent.Owner;ended.At=S(4.1);controller.Poll(ended,ended.At);
  check(port.Last!.CancelThrough==sent.Command&&port.Last.Command==sent.Command,"missing acceptance requests cancellation, not duplicate input");
  ended.At=S(4*3600);controller.Poll(ended,ended.At);
  check(port.Last!.CancelThrough==sent.Command&&port.Last.SnapshotAt==sent.SnapshotAt,"four-hour wait never renews stale decision timestamp");
  ended.Ack=sent.Command;ended.AckResult="retry";controller.Poll(ended,ended.At);ended.At+=TimeSpan.FromSeconds(3).Ticks;controller.Poll(ended,ended.At);
  check(port.Last!.Command>sent.Command&&port.Last.SnapshotAt==ended.At&&controller.Running,"confirmed cancellation replans one fresh command");
  sent=port.Last!;ended.AcceptedCommand=sent.Command;ended.At+=TimeSpan.FromSeconds(22).Ticks;controller.Poll(ended,ended.At);
  check(port.Last!.CancelThrough==sent.Command,"accepted action with missing completion has a host watchdog");
  controller.Stop();ended.At+=TimeSpan.FromHours(1).Ticks;controller.Poll(ended,ended.At);check(!controller.Running&&!port.Last!.Enabled,"manual stop cannot be revived by late result");
  // A real outage pauses only an accepted command's execution clock, not its receipts.
  port=new();controller=new(port);var live=PublicFixtures.Board(now);controller.Start(live,new(),now);controller.Poll(live,now);sent=port.Last!;
  live.Owner=sent.Owner;live.AcceptedCommand=sent.Command;live.NetworkHold=true;live.At=S(3600);controller.Poll(live,live.At);
  check(port.Last!.CancelThrough==0&&port.Last.Command==sent.Command,"accepted live action remains paused during true outage");
  live.Ack=sent.Command;live.AckResult="ok";controller.Poll(live,live.At);check(port.Last!.Command==0,"receipt reconciled even while live network is recovering");
  check(controller.Running,"live connection recovery keeps user intent enabled");
 }
 private sealed class Port:IClientPort
 {
  public Control? Last;public GameProcess? Find()=>new(10,20,"fake");public Snapshot? Read()=>null;
  public void Write(Control c)=>Last=JsonFiles.Clone(c);public Task ConnectAsync(Action<string> p,CancellationToken t)=>Task.CompletedTask;
 }
}
