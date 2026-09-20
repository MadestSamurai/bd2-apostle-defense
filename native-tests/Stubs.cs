using System.Runtime.CompilerServices;
using BD2ApostleDefense;
public enum NetKind{Chat,RandomDefense}
public class Packet {public int MessageId;}
public class Transport {
 public bool Connected=true;public int Queued;public BDNetwork.NetworkTCPManager Owner;
 [MethodImpl(MethodImplOptions.NoInlining)] public void Receive(Packet p){Queued++;}
}
public class NetworkConnectivityMonitor {
 public static NetworkConnectivityMonitor Instance=new();
 public int connectionTimeout=1500;public Action OnNetworkLost;
}
public static class Services {public static BDNetwork.NetworkTCPManager Defense;}
namespace Network.TCP {public enum ENetMsg{CSDefenseReconnectResponse,SCRelayRequest,SCConnectCloseRequest,CSPingResponse,SCDefenseGameStartRequest}}
namespace BDNetwork {
 public class NetworkTCPManager {
  public NetKind Mode=NetKind.RandomDefense;
  public Transport Client;public int LostCalls,RestoreCalls,PacketCalls;
  public NetworkTCPManager(){Client=new(){Owner=this};}
  public bool IsConnect(NetKind kind)=>Mode==kind&&Client?.Connected==true;
  [MethodImpl(MethodImplOptions.NoInlining)] public void OnNetworkLost(){LostCalls++;if(Client!=null)Client.Connected=false;}
  [MethodImpl(MethodImplOptions.NoInlining)] public void OnNetworkRestored(){RestoreCalls++;Client=new(){Owner=this};}
  [MethodImpl(MethodImplOptions.NoInlining)] public void PublishReceivedPacket(Network.TCP.ENetMsg kind,object packet){PacketCalls++;}
 }
}
namespace BD2ApostleDefense.Runtime {
 internal static class Storage {internal static List<NetworkEvidence> Events=new();internal static void AppendNetwork(NetworkEvidence e)=>Events.Add(e);}
}
