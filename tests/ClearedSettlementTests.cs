using BD2ApostleDefense;
internal static class ClearedSettlementTests
{
 public static void Run(Action<bool,string> check,long now)
 {
  Snapshot S(string stage,bool dead=false,bool end=false,string room="round")=>new(){Account=new('a',64),ProcessId=10,ProcessStart=20,Stage=stage,Dead=dead,RoomEnded=end,Room=room,At=now,Ready=false};
  var tracker=new SettlementTracker();var ended=S("ended",true,true);tracker.Observe(ended);
  var initial=S("result",true,true);tracker.Observe(initial);
  var port=new Port();var controller=new Controller(port);controller.Start(initial,new(),now);controller.Poll(initial,now);var first=port.Last!;
  check(first.Action.Kind=="settle","log sequence: first exit submitted");
  var loading=S("waiting",room:"");tracker.Observe(loading);
  check(loading.SettlementPending&&loading.SettlementRoomEnded,"terminal facts survive scene teardown");
  var residual=S("result",room:"");tracker.Observe(residual);
  check(!residual.Dead&&!residual.Win&&!residual.RoomEnded&&GameFlow.CanSettle(residual),"cleared native flags cannot strand result popup");
  residual.Owner=first.Owner;residual.Ack=first.Command;residual.AckResult="timeout";residual.At=now+TimeSpan.FromSeconds(30).Ticks;controller.Poll(residual,residual.At);
  residual.At+=TimeSpan.FromSeconds(3).Ticks;controller.Poll(residual,residual.At);
  check(port.Last!.Command>first.Command&&port.Last.Action.Kind=="settle","timeout replans exit instead of waiting 14 minutes");
  check(Guards.Reject(port.Last!,residual,residual.At)=="","retained evidence passes runtime action guard");
  var confirm=S("confirm-exit",room:"");tracker.Observe(confirm);
  check(new Planner().Decide(confirm,new()).Kind=="confirm-exit","cleared flags also recover native exit confirmation");
  check(GameFlow.Receipt("settle",initial,confirm),"room teardown does not reject observed confirm receipt");
  var net=S("result");tracker.Observe(net);net.NetworkHold=true;
  check(!GameFlow.NetworkBlocked(net),"known completed room does not reconnect after flags clear");
  residual.Exiting=true;check(new Planner().Decide(residual,new()).Kind=="wait","active native exit is never resubmitted");
  residual.Exiting=false;residual.Blocker="NetworkErrorPopupUI";check(new Planner().Decide(residual,new()).Kind=="wait","unknown popup is not blindly dismissed");
  foreach(string boundary in new[]{"lobby","matching","match-failed","login","playing"})
  {
   tracker=new();tracker.Observe(S("ended",true,true));var next=S(boundary);tracker.Observe(next);
   check(!next.SettlementPending&&!next.SettlementRoomEnded,"clear terminal memory on "+boundary);
   var menu=S("result");tracker.Observe(menu);check(!GameFlow.CanSettle(menu),"menu in unfinished round is not terminal after "+boundary);
  }
  foreach(string identity in new[]{"account","pid","start","room"})
  {
   tracker=new();tracker.Observe(S("ended",true,true));var next=S("result");
   if(identity=="account")next.Account=new('b',64);if(identity=="pid")next.ProcessId++;if(identity=="start")next.ProcessStart++;if(identity=="room")next.Room="new";
   tracker.Observe(next);check(!GameFlow.CanSettle(next),"terminal evidence never crosses "+identity);
  }
  tracker=new();var liveMenu=S("result");tracker.Observe(liveMenu);
  check(!GameFlow.CanSettle(liveMenu)&&new Planner().Decide(liveMenu,new()).Kind=="wait","opening result menu is not evidence of defeat");
  tracker.Observe(S("ended",true,false));liveMenu=S("result");liveMenu.NetworkHold=true;tracker.Observe(liveMenu);
  check(GameFlow.NetworkBlocked(liveMenu),"own defeat does not invent server room end");
 }
 private sealed class Port:IClientPort
 {
  public Control? Last;public GameProcess? Find()=>new(10,20,"fake");public Snapshot? Read()=>null;
  public void Write(Control c)=>Last=JsonFiles.Clone(c);public Task ConnectAsync(Action<string> p,CancellationToken t)=>Task.CompletedTask;
 }
}
