using System.Reflection;
namespace BD2ApostleDefense.Runtime;
internal static class Bindings
{
 static readonly Dictionary<string,MemberInfo> apis=new(){
  ["Net.Defense"]=typeof(Services).GetField("Defense"),["Net.Mode"]=typeof(BDNetwork.NetworkTCPManager).GetField("Mode"),
  ["Net.Client"]=typeof(BDNetwork.NetworkTCPManager).GetField("Client"),["Net.Owner"]=typeof(Transport).GetField("Owner"),
  ["Net.Message"]=typeof(Packet).GetField("MessageId"),["Net.Receive"]=typeof(Transport).GetMethod("Receive"),
  ["Net.Lost"]=typeof(BDNetwork.NetworkTCPManager).GetMethod("OnNetworkLost"),["Net.Restored"]=typeof(BDNetwork.NetworkTCPManager).GetMethod("OnNetworkRestored"),
  ["Net.Connected"]=typeof(BDNetwork.NetworkTCPManager).GetMethod("IsConnect"),["Net.Monitor"]=typeof(NetworkConnectivityMonitor).GetField("Instance"),
  ["Net.Timeout"]=typeof(NetworkConnectivityMonitor).GetField("connectionTimeout"),["Net.LostCallback"]=typeof(NetworkConnectivityMonitor).GetField("OnNetworkLost"),
  ["Summon.Select"]=typeof(NativeSummon).GetMethod("Select"),["Summon.Total"]=typeof(NativeSummon).GetField("Total")
 };
 internal static MemberInfo Api(string role)=>apis[role];
 internal static object Read(string role,object owner)=>((FieldInfo)Api(role)).GetValue(owner);
 internal static object InvokeOn(string role,object owner,params object[] args)=>((MethodInfo)Api(role)).Invoke(owner,args);
 internal static object EnumObject(string role,string name)=>Enum.Parse(typeof(NetKind),name);
 internal static object Singleton(Type t)=>RawDataManager.Instance;
}
