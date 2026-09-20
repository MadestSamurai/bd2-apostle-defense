using System.Runtime.CompilerServices;
using Proto.Design.common;
namespace Proto.Design.common
{
 public sealed class MGDCharTable
 {
  public int Id,Grade,Weight;
  public int SummonRatio {[MethodImpl(MethodImplOptions.NoInlining)]get{return Weight;}}
 }
}
public sealed class RawDataManager
{
 public static readonly RawDataManager Instance=new();
 public List<MGDCharTable> Rows=Enumerable.Range(1,8).Select(i=>new MGDCharTable{Id=i,Grade=i,Weight=i==1?5000:i==2?3000:i<7?400:200}).ToList();
 public List<MGDCharTable> GetMGDCharTableListByGroupId(int group)=>Rows;
}
public static class NativeSummon
{
 public static float Total=10000,Draw;
 public static int Calls;
 [MethodImpl(MethodImplOptions.NoInlining)]public static int Select()
 {
  Calls++;float cumulative=0;
  foreach(var row in RawDataManager.Instance.Rows){cumulative+=row.SummonRatio;if(Draw<=cumulative)return row.Id;}
  return RawDataManager.Instance.Rows[0].Id;
 }
}
internal static class LuckyNative
{
 public static void Run(Action<bool,string> check)
 {
  var original=RawDataManager.Instance.Rows.Select(r=>r.SummonRatio).ToArray();
  NativeSummon.Draw=4500;check(NativeSummon.Select()==1,"unpatched summon");
  BD2ApostleDefense.Runtime.LuckySummon.Install();
  check(NativeSummon.Select()==1,"installed mode never affects ordinary clicks");
  BD2ApostleDefense.Runtime.LuckySummon.Run(false,()=>check(NativeSummon.Select()==1,"disabled automation uses original weights"));
  var histogram=new int[8];
  BD2ApostleDefense.Runtime.LuckySummon.Run(true,()=>{for(int i=0;i<10000;i++){NativeSummon.Draw=i+.5f;histogram[NativeSummon.Select()-1]++;}});
  check(histogram.SequenceEqual(new[]{3500,2100,800,800,800,800,600,600}),"actual patched native sampler full distribution");
  check(RawDataManager.Instance.Rows.Select(r=>r.SummonRatio).SequenceEqual(original),"no native table mutation");
  NativeSummon.Draw=4500;
  try{BD2ApostleDefense.Runtime.LuckySummon.Run(true,()=>{check(NativeSummon.Select()==2,"enabled scope applies");throw new Exception("test");});}catch(Exception e)when(e.Message=="test"){}
  check(NativeSummon.Select()==1,"exception restores original scoped behavior");
  int calls=NativeSummon.Calls;NativeSummon.Total=12000;bool rejected=false;
  try{BD2ApostleDefense.Runtime.LuckySummon.Run(true,()=>NativeSummon.Select());}catch(InvalidOperationException){rejected=true;}
  check(rejected&&NativeSummon.Calls==calls,"unknown native denominator rejects before spending");NativeSummon.Total=10000;
  BD2ApostleDefense.Runtime.LuckySummon.Remove();check(NativeSummon.Select()==1,"unload restores native sampler");
 }
}
