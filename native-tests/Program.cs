using BD2ApostleDefense;
using BD2ApostleDefense.Runtime;
using BDNetwork;
using Network.TCP;
int count=0;void Check(bool v,string label){if(!v)throw new Exception(label);count++;Console.WriteLine("PASS "+label);}
void Receive(NetworkTCPManager t,ENetMsg k)=>t.Client.Receive(new(){MessageId=(int)k});
var tcp=new NetworkTCPManager();Services.Defense=tcp;
var monitor=NetworkConnectivityMonitor.Instance;monitor.OnNetworkLost=tcp.OnNetworkLost;
NetworkGuard.Install();NetworkGuard.Pump();Check(monitor.connectionTimeout==3000,"actual guard relaxes owned probe timeout");
Receive(tcp,ENetMsg.CSPingResponse);tcp.OnNetworkLost();Check(tcp.LostCalls==0,"Harmony prefix preserves healthy TCP");
Receive(tcp,ENetMsg.CSPingResponse);tcp.OnNetworkRestored();Check(tcp.RestoreCalls==0&&tcp.Client.Queued==2,"paired restore cannot replace preserved connection or alter packets");
Snapshot s=new(){Room="room1",Stage="playing"};NetworkGuard.Apply(s);Check(s.NetworkHold&&s.NetworkAvoidedDisconnects==1,"recovery is exposed to runtime and diagnostics");
s.Stage="lobby";NetworkGuard.Apply(s);Check(!s.NetworkHold,"native return to lobby does not strand automation");
var chat=new NetworkTCPManager{Mode=NetKind.Chat};chat.OnNetworkLost();chat.OnNetworkRestored();Check(chat.LostCalls==1&&chat.RestoreCalls==1,"other connections retain native behavior");
Receive(tcp,ENetMsg.SCConnectCloseRequest);tcp.OnNetworkLost();tcp.OnNetworkRestored();Check(tcp.LostCalls==1&&tcp.RestoreCalls==1,"server close is delivered and retains native disconnect and restore");
NetworkGuard.Pump();s.Stage="playing";NetworkGuard.Apply(s);Check(s.NetworkHold,"new TCP identity cannot inherit old packet health");
long previousPacket=s.NetworkLastPacketAt;
var retired=new Transport{Owner=tcp};retired.Receive(new(){MessageId=(int)ENetMsg.CSPingResponse});
NetworkGuard.Apply(s);Check(s.NetworkLastPacketAt==previousPacket,"retired socket packets cannot prove current socket health");
tcp.PublishReceivedPacket(ENetMsg.CSPingResponse,new());NetworkGuard.Apply(s);Check(s.NetworkLastPacketAt==previousPacket,"cross-manager published notifications cannot fabricate received traffic");
Receive(tcp,ENetMsg.CSPingResponse);NetworkGuard.Pump();NetworkGuard.Apply(s);Check(s.NetworkHold&&s.NetworkState=="waiting-room","transport heartbeat alone cannot resume a reconnected match");
Receive(tcp,ENetMsg.CSDefenseReconnectResponse);NetworkGuard.Pump();NetworkGuard.Apply(s);Check(s.NetworkHold&&s.NetworkState=="recovering","room acknowledgment starts stable board recovery");
NetworkGuard.Flush();Check(Storage.Events.Any(e=>e.Kind=="probe-restored-kept-session")&&Storage.Events.Any(e=>e.Kind=="server-close-native"),"diagnostics distinguish avoided reconnect and server close");
monitor.OnNetworkLost=chat.OnNetworkLost;NetworkGuard.Pump();Check(monitor.connectionTimeout==1500,"shared monitor timeout restored outside owned defense callback");
monitor.OnNetworkLost=tcp.OnNetworkLost;NetworkGuard.Pump();NetworkGuard.Remove();Check(monitor.connectionTimeout==1500,"unload restores probe configuration");
int lost=tcp.LostCalls;tcp.OnNetworkLost();Check(tcp.LostCalls==lost+1,"unload removes only owned patch");
LuckyNative.Run(Check);
Console.WriteLine("TOTAL "+count+" PASS; actual Harmony adapter with isolated transport");

if(args.Length>0){Directory.CreateDirectory(args[0]);JsonFiles.Write(Path.Combine(args[0],"network-native.json"),new{status="passed",checks=count});}
