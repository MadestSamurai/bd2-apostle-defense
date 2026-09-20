using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Proto.Design.common;
using B=BD2ApostleDefense.Runtime.Bindings;
namespace BD2ApostleDefense.Runtime
{
 internal static class LuckySummon
 {
  private const string PatchId="bd2.apostle-defense.lucky.v1";
  private static Harmony patch;
  [ThreadStatic] private static Dictionary<int,int> weights;
  internal static void Install()
  {
   if(patch!=null)return;patch=new Harmony(PatchId);
   try{patch.Patch((MethodInfo)B.Api("Summon.Select"),transpiler:new HarmonyMethod(typeof(LuckySummon),nameof(Rewrite)));}catch{Remove();throw;}
  }
  internal static void Remove(){weights=null;if(patch!=null){patch.Unpatch((MethodInfo)B.Api("Summon.Select"),HarmonyPatchType.Transpiler,PatchId);patch=null;}}
  private static IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> input)
  {
   var code=input.ToList();var getter=typeof(MGDCharTable).GetProperty("SummonRatio").GetGetMethod();
   var matches=code.Where(i=>i.Calls(getter)).ToArray();
   if(matches.Length!=1)throw new InvalidOperationException("Lucky mode: native selector changed");
   matches[0].opcode=OpCodes.Call;matches[0].operand=AccessTools.Method(typeof(LuckySummon),nameof(Weight));return code;
  }
  private static int Weight(MGDCharTable row){int value;return weights!=null&&weights.TryGetValue(row.Id,out value)?value:row.SummonRatio;}
  internal static void Run(bool enabled,Action summon)
  {
   if(!enabled){summon();return;}
   if(patch==null)throw new InvalidOperationException("Lucky mode: component not ready");
   var raw=(RawDataManager)B.Singleton(typeof(RawDataManager));
   var rows=raw.GetMGDCharTableListByGroupId(1).Select(r=>new UnitDef{Id=r.Id,Grade=r.Grade,Weight=r.SummonRatio}).ToArray();
   var adjusted=LuckyWeights.Create(rows);
   int total=(int)Math.Max(1000f,Convert.ToSingle(B.Read("Summon.Total",null)));
   if(rows.Sum(r=>(long)r.Weight)!=total)throw new InvalidOperationException("Lucky mode: native total changed");
   var previous=weights;
   try{weights=rows.Select((r,i)=>new{r.Id,Weight=adjusted[i]}).ToDictionary(r=>r.Id,r=>r.Weight);summon();}
   finally{weights=previous;}
  }
 }
}
