using BD2ApostleDefense;
internal static class NetworkRecoveryTests
{
 public static void Run(Action<bool,string> check,long now)
 {
  long S(double value)=>now+TimeSpan.FromSeconds(value).Ticks;
  var p=new NetworkRecoveryPolicy();p.Packet(S(0));
  check(!p.Lost(S(1),true),"healthy transport defers external probe loss");
  check(p.Hold(true),"probe uncertainty pauses spending until confirmed server traffic");
  p.Packet(S(1.2));p.Tick(S(1.2),true);p.Tick(S(3.3),true);
  check(!p.Hold(true)&&p.Deferred,"ongoing server traffic resumes during external probe outage");
  check(!p.Restored(S(3.4),true)&&p.AvoidedDisconnects==1,"paired restore does not rebuild a preserved session");
  p.Tick(S(3.4),true);p.Packet(S(4));p.Tick(S(5.5),true);
  check(!p.Hold(true),"stable live connection resumes automation");
  p=new();check(!p.Lost(S(0),true),"unknown connected socket gets finite grace");
  check(!p.Tick(S(5),true)&&p.Hold(true),"socket flag alone cannot resume operations");
  check(p.Tick(S(6),true)&&p.NativeDisconnects==1,"silent socket eventually invokes real disconnection without second lost event");
  check(!p.Tick(S(7),true)&&!p.Tick(S(100),true),"expired grace is dispatched once");
  check(p.Restored(S(7),false),"real disconnect retains original restore");
  p.NewConnection(true);p.Packet(S(10));p.Tick(S(10),true);p.Tick(S(12.1),true);check(p.Hold(true)&&p.AwaitingRound,"transport handshake cannot confirm match recovery");
  p.Packet(S(13),true);p.Tick(S(13),true);check(p.Hold(true),"room response still requires stable recovery window");p.Tick(S(15.1),true);check(!p.Hold(true),"room response and stable connection confirm recovery");
  p=new();p.Packet(S(0));p.Lost(S(1),true);
  check(!p.Tick(S(7),true),"recent response retains session through grace deadline");
  check(p.Tick(S(8.1),true),"old response cannot conceal ongoing silence");
  p=new();p.Packet(S(0));p.Lost(S(1),true);check(p.Tick(S(1.1),false),"real socket loss bypasses grace");
  p=new();p.Packet(S(0));p.ClosedByServer();check(p.Lost(S(1),true)&&p.Restored(S(2),true)&&p.Hold(true),"explicit server close always follows native handling");p.Packet(S(3));check(!p.Fresh(S(3)),"late packets do not reverse explicit server close");
  p.NewConnection(true);check(!p.Fresh(S(3))&&p.Hold(true),"new client cannot inherit old packets");
  p=new();for(int i=0;i<4;i++){double t=100*i;p.Packet(S(t));check(!p.Lost(S(t+.1),true),"recorded short outage is deferred "+i);p.Packet(S(t+.4));check(!p.Restored(S(t+.7),true),"recorded half second restoration retains TCP "+i);}
  check(p.AvoidedDisconnects==4&&p.NativeDisconnects==0,"four short outages avoid four forced reconnects");
  p=new();p.Packet(S(10));check(!p.Fresh(S(9)),"backward clock cannot fabricate freshness");
  p.Lost(S(11),true);p.Lost(S(16),true);check(p.Tick(S(20),true),"duplicate losses cannot extend grace forever");
  var port=new Port();var controller=new Controller(port);var s=PublicFixtures.Board(now);s.NetworkHold=true;s.NetworkMessage="network hold";
  controller.Start(s,new(),now);controller.Poll(s,now);
  check(controller.Running&&port.Last!.Command==0&&port.Last.Enabled,"network hold retains enabled intent with no new operation");
  s.At=S(90);controller.Poll(s,s.At);check(controller.Running&&port.Last!.Expires>s.At,"long network wait renews lease without stopping GUI");
  s.NetworkHold=false;controller.Poll(s,s.At);var sent=port.Last!;check(sent.Command>0,"recovery automatically resumes current-board planning");
  s.Owner=sent.Owner;s.NetworkHold=true;s.At=S(91);controller.Poll(s,s.At);check(port.Last!.Command==sent.Command,"inflight command identity survives network wait");
  s.Ack=sent.Command;s.AckResult="ok";s.NetworkHold=false;s.At=S(95);controller.Poll(s,s.At);check(port.Last!.Command==0&&controller.Running,"recovered receipt reconciles once before next decision");
  s.NetworkHold=true;var c=JsonFiles.Clone(sent);c.SnapshotAt=s.At;c.Expires=s.At+TimeSpan.FromSeconds(10).Ticks;check(Guards.Reject(c,s,s.At)!="","native guard blocks stale spending during recovery");
  controller.SetLuckyMode(true);check(port.Last!.LuckyMode,"confirmed mode reaches active lease immediately");
  controller.SetLuckyMode(false);check(!port.Last!.LuckyMode,"disabling mode reaches active lease immediately");
  controller.Stop();s.NetworkHold=false;s.At=S(100);controller.Poll(s,s.At);check(!port.Last!.Enabled&&!controller.Running,"manual stop remains authoritative through recovery");
 }
 private sealed class Port:IClientPort
 {
  public Control? Last;public GameProcess? Find()=>new(10,20,"fake");public Snapshot? Read()=>null;
  public void Write(Control c)=>Last=JsonFiles.Clone(c);public Task ConnectAsync(Action<string> p,CancellationToken t)=>Task.CompletedTask;
 }
}
