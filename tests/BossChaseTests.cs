using System.Diagnostics;
using BD2ApostleDefense;

internal static class BossChaseTests
{
 public static object Run(Action<bool,string> check, Catalog catalog, Snapshot geometry, long now)
 {
  Snapshot Scene(int wave=50)
  {
   var w=catalog.Waves.Single(w=>w.Id==wave);
   var s=new Snapshot{At=now,ProcessId=10,ProcessStart=20,Account=new('a',64),Room="boss-test",Stage="playing",Ready=true,MyView=true,Wave=wave,SecondsLeft=w.Duration,
    Boards=JsonFiles.Clone(geometry.Boards),Path=JsonFiles.Clone(geometry.Path),Catalog=JsonFiles.Clone(catalog),Levels=new[]{20,20,20,20,20}};
   s.Catalog.SummonCost=25;s.Catalog.GameOverCount=100;s.Catalog.MaxUpgrade=99;s.Catalog.Advantage=1;s.Catalog.Penalty=.5;
   s.Units=new[]{new Unit{Id=84,Index=1,Grid=s.Boards.OrderBy(b=>b.Z).ThenByDescending(b=>b.X).First().Id,Ready=true}};
   s.Enemies=new[]{new Enemy{Id=901,Table=w.EnemyTable,Element=w.Element,Hp=w.Hp,Speed=w.Speed,X=s.Path[0].X,Y=s.Path[0].Y,Z=s.Path[0].Z,NextPoint=s.Path[1].Id}};
   s.EnemyCount=1;s.BoardKey=Guards.BoardKey(s);return s;
  }
  Control Command(Snapshot s,Decision d)=>new(){Enabled=true,ProcessId=s.ProcessId,ProcessStart=s.ProcessStart,Owner=new('b',32),Account=s.Account,Room=s.Room,BoardKey=s.BoardKey,SnapshotAt=s.At,Expires=s.At+TimeSpan.FromSeconds(10).Ticks,Command=1,Action=d};
  var settings=new Settings();var s=Scene();var model=new BossChase();
  s.Units[0].TargetEnemy=901;s.Units[0].AttackState=0;s.Units[0].AttackElapsed=.09;
  using(var stream=new System.IO.MemoryStream())
  {
   new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Snapshot)).WriteObject(stream,s);stream.Position=0;
   var restored=System.Text.Json.JsonSerializer.Deserialize<Snapshot>(stream,JsonFiles.Options)!;
   check(restored.Units[0].TargetEnemy==901&&restored.Units[0].AttackState==0&&Math.Abs(restored.Units[0].AttackElapsed-.09)<1e-8&&restored.Enemies[0].Id==901&&restored.Enemies[0].NextPoint==s.Path[1].Id&&restored.Enemies[0].Speed==s.Enemies[0].Speed&&restored.Catalog.Waves.Last().EnemyTable>0,"Runtime4 attack phase, enemy identity, speed and route serialize across Mono/desktop");
  }
  s=Scene();
  var d=model.Decide(s,settings,now);
  check(d?.Kind=="move"&&d.Policy==BossChase.Policy&&d.TargetEnemy==901,"boss chase selects native move and exact live boss instance");
  check(d!=null&&Guards.Reject(Command(s,d),s,now)=="","boss chase command passes native guards");
  var p=new Planner();var planned=p.Decide(s,settings,now);check(planned.Policy==BossChase.Policy&&p.Focus.StartsWith("BOSS"),"boss chase connected to production planner");
  var native=Command(s,planned);
  check(Guards.Reject(native,s,now+TimeSpan.FromSeconds(1).Ticks).Contains("过期"),"native guard rejects late chase delivery");
  var changed=JsonFiles.Clone(s);changed.Wave=49;check(Guards.Reject(native,changed,now).Length>0,"native guard rejects changed boss wave");
  changed=JsonFiles.Clone(s);changed.Enemies[0].Hp=0;check(Guards.Reject(native,changed,now).Length>0,"native guard rejects dead chase target");
  changed=JsonFiles.Clone(s);changed.Enemies[0].Id++;check(Guards.Reject(native,changed,now).Length>0,"native guard rejects replaced boss instance");
  check(model.Decide(s,settings,now+TimeSpan.FromSeconds(1).Ticks)==null,"planner does not chase stale positions");
  changed=JsonFiles.Clone(s);changed.Blocker="NetworkErrorPopupUI";check(model.Decide(changed,settings,now)==null,"boss chase respects real popup");
  changed=JsonFiles.Clone(s);changed.MyView=false;check(model.Decide(changed,settings,now)==null,"boss chase never handles another player's board");
  changed=JsonFiles.Clone(s);changed.Enemies[0].NextPoint=-1;check(model.Decide(changed,settings,now)==null,"missing waypoint is not guessed");
  changed=JsonFiles.Clone(s);changed.Enemies[0].Speed=double.NaN;check(model.Decide(changed,settings,now)==null,"invalid speed cannot enter projection");
  changed=JsonFiles.Clone(s);changed.Enemies=changed.Enemies.Concat(changed.Enemies).ToArray();check(model.Decide(changed,settings,now)==null,"duplicate live identities cannot enter target model");
  changed=JsonFiles.Clone(s);changed.Wave=49;check(model.Decide(changed,settings,now)==null,"ordinary waves do not activate boss chase");
  changed=JsonFiles.Clone(s);changed.SecondsLeft=.1;check(model.Decide(changed,settings,now)==null,"deadline shorter than dispatch latency keeps firing");
  // Same boss wave, same layout: a held tactic must never be undone by static whole-loop assignment.
  var holding=JsonFiles.Clone(s);holding.Units[0].Grid=planned.TargetGrid;
  var holds=new Planner().Decide(holding,settings,now);
  check(holds.Kind!="move"||holds.Policy==BossChase.Policy,"boss phase excludes static pullback");
  // Confirmations, not proposed moves, activate cooldown. Also guard the exchanged unit.
  model=new();model.Confirm(planned,s,now);
  var opposite=JsonFiles.Clone(s);opposite.Units[0].Grid=planned.TargetGrid;var at=opposite.Path[4];opposite.Enemies[0].X=at.X;opposite.Enemies[0].Z=at.Z;opposite.Enemies[0].NextPoint=opposite.Path[5].Id;
  opposite.At=now+TimeSpan.FromMilliseconds(300).Ticks;check(model.Decide(opposite,settings,opposite.At)==null,"confirmed unit is not immediately moved again");
  opposite.At=now+TimeSpan.FromSeconds(1).Ticks;check(model.Decide(opposite,settings,opposite.At)?.Policy==BossChase.Policy,"confirmed unit becomes eligible after hold interval");
  opposite.Room="next-game";opposite.At=now+TimeSpan.FromMilliseconds(300).Ticks;check(model.Decide(opposite,settings,opposite.At)!=null,"new match does not inherit old instance cooldown");
  // Exchange accounting: moving an idle elite cannot displace a stronger short-range attacker.
  var pair=Scene();var near=pair.Boards.Single(b=>b.Id==planned.TargetGrid);var far=pair.Boards.Single(b=>b.Id==pair.Units[0].Grid);
  pair.Boards=new[]{far,near};var strong=JsonFiles.Clone(pair.Catalog.Units.Single(x=>x.Id==84));strong.Id=999;strong.Attack=100000;strong.UpAttack=0;
  pair.Catalog.Units=pair.Catalog.Units.Append(strong).ToArray();pair.Units=pair.Units.Append(new Unit{Id=999,Index=2,Grid=near.Id,Ready=true,TargetEnemy=901}).ToArray();pair.BoardKey=Guards.BoardKey(pair);
  check(new BossChase().Decide(pair,settings,now)==null,"boss chase rejects a damaging swap that sacrifices a stronger attacker");
  strong.Range=100;strong.Attack=500;pair.Enemies[0].Hp=10000000;
  var rangedSwap=new BossChase().Decide(pair,settings,now);check(rangedSwap?.UnitIndex==1&&rangedSwap.TargetIndex==2,"unchanged ranged coverage allows elite melee exchange");
  pair.Enemies[0].Hp=1;check(new BossChase().Decide(pair,settings,now)==null,"boss already covered by lethal damage does not cause a move");
  // Current-target stickiness and residual mobs are modelled from captured IDs, not table equality.
  var mixed=Scene();var trash=JsonFiles.Clone(mixed.Enemies[0]);trash.Id=902;trash.Table=1029;trash.Hp=9500;trash.Speed=4.2;trash.X=4;trash.Z=-4.88;trash.NextPoint=mixed.Path[4].Id;
  mixed.Enemies=mixed.Enemies.Append(trash).ToArray();mixed.EnemyCount=2;mixed.Units[0].TargetEnemy=902;mixed.Units[0].AttackState=0;mixed.Units[0].AttackElapsed=.09;
  var mixedMove=new BossChase().Decide(mixed,settings,now);check(mixedMove?.TargetEnemy==901,"mixed wave follows actual boss rather than a residual mob");
  var square=new[]{new Point{Id=90,X=0,Z=0},new Point{Id=4,X=1,Z=0},new Point{Id=22,X=1,Z=1},new Point{Id=7,X=0,Z=1}};
  var projected=BossChase.Predict(new Enemy{X=.9,Z=1,NextPoint=7,Speed=1},square,.1,25);
  check(projected[8].X<.2&&projected[8].Z>.9&&projected[17].X<.1&&projected[17].Z<.3,"prediction uses captured list direction through corner");
  check(projected[24].X>.3&&projected[24].Z<.1,"prediction wraps closed route with noncontiguous waypoint IDs");
  // Full protocol dispatch, acknowledgement, cooldown and stop/reset use production controller.
  var port=new Port();var controller=new Controller(port);s=Scene();controller.Start(s,settings,now);controller.Poll(s,now);
  var command=port.Last!;check(command.Action.Policy==BossChase.Policy,"production controller sends tactical native command");
  s.Units[0].Grid=command.Action.TargetGrid;s.Owner=command.Owner;s.Ack=command.Command;s.AckResult="ok";s.BoardKey=Guards.BoardKey(s);s.At=now+TimeSpan.FromMilliseconds(200).Ticks;controller.Poll(s,s.At);
  check(port.Last!.Command==0,"tactical acknowledgement clears pending command before another move");controller.Stop();
  port=new Port();controller=new Controller(port);s=Scene();controller.Start(s,settings,now);controller.Poll(s,now);command=port.Last!;
  s.Owner=command.Owner;s.Ack=command.Command;s.AckResult="rejected";s.At=now+TimeSpan.FromMilliseconds(50).Ticks;controller.Poll(s,s.At);
  s.At=now+TimeSpan.FromMilliseconds(600).Ticks;controller.Poll(s,s.At);
  check(controller.Running&&port.Last!.Command>command.Command&&port.Last.Action.Policy==BossChase.Policy,"rejected native chase re-evaluates without phantom move cooldown");controller.Stop();
  // Quantitative regression on generated geometry and rules, separate 50 Hz native-like runner.
  var simulations=new List<object>();
  foreach(var (id,wave,count) in new[]{(84,50,1),(82,50,1),(84,30,2),(84,40,3)})
  {
   var scene=Scene(wave);scene.Units[0].Id=id;
   var w=scene.Catalog.Waves.Single(w=>w.Id==wave);
   scene.Enemies=Enumerable.Range(0,count).Select(i=>{var e=JsonFiles.Clone(scene.Enemies[0]);e.Id=901+i;var pt=scene.Path[(i*2)%scene.Path.Length];e.X=pt.X;e.Z=pt.Z;e.NextPoint=scene.Path[(i*2+1)%scene.Path.Length].Id;return e;}).ToArray();scene.EnemyCount=count;
   // Stronger HP preserves the measurement horizon; this is an output test, not a win claim.
   foreach(var e in scene.Enemies)e.Hp=1e9;
   var fixedRun=RunBattle(scene,false,36);var chaseRun=RunBattle(scene,true,36);
   Console.WriteLine($"Boss {id}/W{wave}: static={fixedRun.Damage:0} chase={chaseRun.Damage:0} ratio={chaseRun.Damage/Math.Max(1,fixedRun.Damage):0.000} moves={chaseRun.Moves} mean={chaseRun.MeanMs:0.00}ms max={chaseRun.MaxMs:0.00}ms");
   check(chaseRun.Damage>fixedRun.Damage*(count==1?1.5:1.05),$"{id}/wave{wave} chase improves useful damage over optimal static placement");
   check(chaseRun.Moves>0&&chaseRun.Moves<45,$"{id}/wave{wave} chase has bounded move frequency");
   simulations.Add(new{unit=id,wave,bosses=count,seconds=36,staticDamage=fixedRun.Damage,chaseDamage=chaseRun.Damage,ratio=chaseRun.Damage/Math.Max(1,fixedRun.Damage),chaseRun.Moves,chaseRun.MeanMs,chaseRun.MaxMs});
  }
  var full=Scene();full.Units=full.Boards.Select((b,i)=>new Unit{Id=full.Catalog.Units[i%full.Catalog.Units.Length].Id,Index=i+1,Grid=b.Id,Ready=true}).ToArray();
  var watch=Stopwatch.StartNew();for(int i=0;i<10;i++)new BossChase().Decide(full,settings,now);watch.Stop();
  check(watch.Elapsed.TotalMilliseconds/10<150,"full 68-unit live boss tactics under 150 ms mean");
  var pressure=new List<object>();
  foreach(int count in new[]{5,30,90})
  {
   var x=JsonFiles.Clone(full);x.Enemies=Enumerable.Range(0,count).Select(i=>{var e=JsonFiles.Clone(full.Enemies[0]);e.Id=901+i;var a=x.Path[i%x.Path.Length];e.X=a.X;e.Z=a.Z;e.NextPoint=x.Path[(i+1)%x.Path.Length].Id;return e;}).ToArray();x.EnemyCount=count;
   var clock=Stopwatch.StartNew();new BossChase().Decide(x,settings,now);clock.Stop();Console.WriteLine($"Boss full 68 x {count}: {clock.Elapsed.TotalMilliseconds:0.00} ms");pressure.Add(new{enemies=count,ms=clock.Elapsed.TotalMilliseconds});
   check(clock.Elapsed.TotalMilliseconds<200,$"68 units/{count} enemy tactics latency bounded");
  }
  return new{simulations,fullBoardMeanMs=watch.Elapsed.TotalMilliseconds/10,pressure};
 }
 private sealed class Port:IClientPort
 {
  public Control? Last;
  public GameProcess? Find()=>new(10,20,"fake");public Snapshot? Read()=>null;
  public void Write(Control c)=>Last=JsonFiles.Clone(c);
  public Task ConnectAsync(Action<string> progress,CancellationToken token)=>Task.CompletedTask;
 }
 private sealed record BattleResult(double Damage,int Moves,double MeanMs,double MaxMs);
 private static BattleResult RunBattle(Snapshot input,bool chase,double seconds)
 {
  var s=JsonFiles.Clone(input);var unit=s.Units[0];var d=s.Catalog.Units.Single(x=>x.Id==unit.Id);var whole=new Planner();
  unit.Grid=s.Boards.OrderByDescending(b=>whole.Coverage(d,b,s.Path)).ThenBy(b=>b.Id).First().Id;
  var model=new BossChase();int moves=0,calls=0;double total=0,maxMs=0,sumMs=0,nextPoll=0,nextCommand=0;Decision? pending=null;double applyAt=0;
  long start=s.At;int target=-1,phase=2;double clock=0;
  const double dt=.02;var hp=s.Enemies.Select(e=>e.Hp).ToArray();
  // Position stepping is intentionally independent of the candidate predictor.
  for(int tick=0;tick<(int)(seconds/dt);tick++)
  {
   double time=tick*dt;s.At=start+(long)(time*TimeSpan.TicksPerSecond);s.SecondsLeft=seconds-time;s.BoardKey=Guards.BoardKey(s);
   unit.TargetEnemy=target<0?-1:s.Enemies[target].Id;unit.AttackState=phase;unit.AttackElapsed=clock;
   if(pending!=null&&time>=applyAt)
   {unit.Grid=pending.TargetGrid;model.Confirm(pending,s,s.At);pending=null;moves++;nextCommand=time+.5;}
   if(chase&&pending==null&&time>=nextPoll&&time>=nextCommand)
   {
    nextPoll=time+.15;var watch=Stopwatch.StartNew();var move=model.Decide(s,new(),s.At);watch.Stop();calls++;sumMs+=watch.Elapsed.TotalMilliseconds;maxMs=Math.Max(maxMs,watch.Elapsed.TotalMilliseconds);
    if(move!=null){pending=move;applyAt=time+.25;}
   }
   foreach(var e in s.Enemies)
   {
    int next=Array.FindIndex(s.Path,p=>p.Id==e.NextPoint);var to=s.Path[next];double dist=Planner.Distance(e,to),step=e.Speed*dt;
    double ratio=dist<1e-9?1:Math.Min(1,step/dist);e.X+=(to.X-e.X)*ratio;e.Y+=(to.Y-e.Y)*ratio;e.Z+=(to.Z-e.Z)*ratio;
    if(Planner.Distance(e,to)<.1)e.NextPoint=s.Path[(next+1)%s.Path.Length].Id;
   }
   var location=s.Boards.Single(b=>b.Id==unit.Grid);
   if(phase==0)
   {
    clock+=dt;if(clock+1e-9<d.Interval)continue;
    if(target>=0&&hp[target]>0&&Planner.Distance(location,s.Enemies[target])<=d.Range)phase=1;
    else{clock=0;phase=2;target=-1;}
   }
   else if(phase==2)
   {
    target=Enumerable.Range(0,s.Enemies.Length).Where(e=>hp[e]>0&&Planner.Distance(location,s.Enemies[e])<=d.Range).OrderBy(e=>Planner.Distance(location,s.Enemies[e])).DefaultIfEmpty(-1).First();if(target>=0)phase=1;
   }
   else
   {
    double hit=(d.Attack+(double)d.UpAttack*s.Levels[d.Element])*Planner.ElementMultiplier(d.Element,s.Enemies[target].Element,s.Catalog);
    double damage=Math.Min(hp[target],hit);total+=damage;hp[target]-=damage;
    if(d.SplashRange>0)for(int e=0;e<s.Enemies.Length;e++)if(e!=target&&hp[e]>0&&Planner.Distance(s.Enemies[e],s.Enemies[target])<=d.SplashRange){damage=Math.Min(hp[e],hit);total+=damage;hp[e]-=damage;}
    clock=0;phase=0;
   }
  }
  return new(total,moves,sumMs/Math.Max(1,calls),maxMs);
 }
}
