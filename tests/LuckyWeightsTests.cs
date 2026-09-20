using BD2ApostleDefense;
internal static class LuckyWeightsTests
{
 public static void Run(Action<bool,string> check)
 {
  var rows=Enumerable.Range(1,8).Select(i=>new UnitDef{Id=i,Grade=i,Weight=i==1?5000:i==2?3000:i<7?400:200}).ToArray();
  var result=LuckyWeights.Create(rows);
  check(result.Sum()==rows.Sum(r=>r.Weight),"lucky exact total conserved");
  check(result.Skip(2).Take(4).All(x=>x==800)&&result.Skip(6).All(x=>x==600),"lucky requested high-grade multipliers");
  check(result[0]==3500&&result[1]==2100,"low grades fund increase proportionally");
  check(rows[0].Weight==5000&&LuckyWeights.Create(rows).SequenceEqual(result),"original table stays unchanged across repeated summons");
  var rng=new Random(193);
  for(int test=0;test<100;test++)
  {
   var generated=Enumerable.Range(1,12).Select(i=>new UnitDef{Id=i,Grade=i<5?1+i%2:3+i%6,Weight=i<5?rng.Next(1000,3000):rng.Next(0,100)}).ToArray();
   var adjusted=LuckyWeights.Create(generated);
   check(adjusted.Sum()==generated.Sum(r=>r.Weight)&&adjusted.All(x=>x>=0),"generated exact nonnegative budget "+test);
   check(generated.Select((r,i)=>r.Grade<=2||adjusted[i]==r.Weight*(r.Grade>=7?3:2)).All(x=>x),"generated high grades unchanged by rounding "+test);
  }
  foreach(var invalid in new[]{Array.Empty<UnitDef>(),new[]{new UnitDef{Id=1,Grade=8,Weight=100}},new[]{new UnitDef{Id=1,Grade=1,Weight=-1}},new[]{new UnitDef{Id=1,Grade=9,Weight=100}},new[]{new UnitDef{Id=1,Grade=1,Weight=100},new UnitDef{Id=1,Grade=2,Weight=100}}})
  {bool rejected=false;try{LuckyWeights.Create(invalid);}catch(InvalidOperationException){rejected=true;}check(rejected,"invalid lucky distribution rejected before action");}
  var edge=new[]{new UnitDef{Id=1,Grade=1,Weight=2},new UnitDef{Id=2,Grade=2,Weight=0},new UnitDef{Id=3,Grade=8,Weight=1}};
  check(LuckyWeights.Create(edge).SequenceEqual(new[]{0,0,3}),"exhausted low budget and zero-weight entries supported");
 }
}
