using BD2ApostleDefense;

internal static class OpeningEconomyTests
{
 public static void Run(Action<bool,string> check,long now)
 {
  Snapshot Sample(int count=1)=>new(){At=now,Stage="playing",Ready=true,MyView=true,Wave=0,SecondsLeft=30,
   Gold=20,CanSummon=false,Levels=new int[5],UpgradeCosts=new[]{12,12,12,12,12},CanUpgrade=Enumerable.Repeat(true,5).ToArray(),
   Boards=Enumerable.Range(0,8).Select(i=>new Board{Id=i,X=i/10.0}).ToArray(),
   Path=new[]{new Point{X=-1,Z=-1},new Point{X=1,Z=-1},new Point{X=1,Z=1},new Point{X=-1,Z=1}},
   Units=Enumerable.Range(0,count).Select(i=>new Unit{Index=i+1,Id=1,Grid=i,Ready=true}).ToArray(),
   Catalog=new(){Key="generated-opening",SummonCost=25,GameOverCount=100,MaxUpgrade=99,MaxGrade=8,Advantage=1,Penalty=.5,
    Units=new[]{new UnitDef{Id=1,Element=4,Grade=1,Attack=12,UpAttack=12,Interval=1.5,Range=20,Weight=1}},
    Waves=new[]{new WaveDef{Id=1,Element=0,Hp=9,Count=60,Duration=30,Gold=1},new WaveDef{Id=2,Element=2,Hp=18,Count=60,Duration=30,Gold=1},
     new WaveDef{Id=10,Element=1,Hp=5000,Count=1,Duration=60,Boss=true},new WaveDef{Id=50,Element=3,Hp=100000,Count=1,Duration=60,Boss=true}}}};
  var s=Sample();var p=new Planner();var unit=s.Catalog.Units[0];var wave=s.Catalog.Waves[0];
  check(p.ClearRate(unit,s.Boards[0],0,wave,s)==p.ClearRate(unit,s.Boards[0],1,wave,s),"opening upgrade of one-shot attack has zero clear-rate gain");
  check(Math.Abs(p.ClearRate(unit,s.Boards[0],0,wave,s)-1/(1.5+1.0/60))<1e-9,"single-target one-shot rate is attack-speed bounded");
  wave.Hp=30;
  check(Math.Abs(p.ClearRate(unit,s.Boards[0],1,wave,s)/p.ClearRate(unit,s.Boards[0],0,wave,s)-1.5)<1e-9,"three-hit to two-hit upgrade gains real kill throughput");
  foreach(bool rare in new[]{false,true})foreach(int count in new[]{0,1,2})foreach(int gold in new[]{0,12,20,24,25,45,100})
  {
   s=Sample(count);s.Gold=gold;s.CanSummon=gold>=25;s.CanUpgrade=Enumerable.Repeat(true,5).ToArray();s.EnemyCount=81;
   var d=new Planner().Decide(s,new(){Clear50=!rare,Rare=rare});
   check(d.Kind==(gold>=25?"summon":"wait"),$"opening count {count}, gold {gold}, rare {rare} preserves next summon despite urgency");
  }
  s=Sample(1);s.Gold=45;s.CanSummon=false;
  check(new Planner().Decide(s,new()).Kind=="wait","temporarily unavailable summon never diverts bootstrap money to upgrades");
  s=Sample(3);s.Wave=1;s.Gold=100;s.CanSummon=true;
  check(new Planner().Decide(s,new()).Kind=="summon","distant boss or next wave cannot justify a current one-shot upgrade on sparse board");
  s.Gold=24;s.CanSummon=false;s.EnemyCount=90;
  check(new Planner().Decide(s,new()).Kind=="wait","urgent low-gold state does not purchase zero-clearing-gain upgrade");
  s=Sample(3);s.Catalog.Units[0].Attack=4;s.Catalog.Units[0].UpAttack=8;s.Catalog.Waves[0].Hp=7;
  s.Gold=20;
  check(new Planner().Decide(s,new()).Kind=="wait","profitable damage upgrade still waits if it delays next summon income");
  s.Gold=10;s.UpgradeCosts[4]=5;s.Catalog.Waves[0].Count=120;
  check(new Planner().Decide(s,new()).Kind=="upgrade","income-positive breakpoint upgrade may accelerate the next summon after bootstrap");
  s.Gold=40;s.CanSummon=true;
  check(new Planner().Decide(s,new()).Kind=="upgrade","useful affordable multi-unit upgrade remains available after bootstrap");
  s.Levels[4]=1;
  check(new Planner().Decide(s,new()).Kind=="summon","after reaching one-hit breakpoint do not keep upgrading the same element");
  // Never value a summon only at the first native spawn slot when we can move it.
  s=Sample(3);s.Wave=10;s.Gold=100;s.CanSummon=true;s.UpgradeCosts[4]=50;
  s.Catalog.Units[0].Range=3;s.Boards[3].X=100;
  s.Catalog.Units[0].UpAttack=1;
  var first=new Planner().Decide(s,new());
  (s.Boards[3],s.Boards[4])=(s.Boards[4],s.Boards[3]);
  var reordered=new Planner().Decide(s,new());
  check(first.Kind=="summon"&&reordered.Kind=="summon","summon comparison uses reachable destination independent of empty-slot order");
  s=Sample(1);s.Boards=s.Boards.Take(1).ToArray();s.Wave=10;s.Gold=50;
  check(new Planner().Decide(s,new()).Kind=="upgrade","small full board is not stuck in bootstrap reserve");
 }
}
