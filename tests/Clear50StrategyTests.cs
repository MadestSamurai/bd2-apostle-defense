using BD2ApostleDefense;
internal static class Clear50StrategyTests
{
 internal static void Run(Action<bool,string> check,long now)
 {
  Snapshot Sample()=>new(){At=now,ProcessId=10,ProcessStart=20,Account=new('a',64),Room="opening",Stage="playing",Ready=true,MyView=true,Wave=1,Gold=100,CanSummon=true,SecondsLeft=30,
   Boards=Enumerable.Range(0,8).Select(i=>new Board{Id=i,X=i*.01}).ToArray(),Path=[new(){X=-1,Z=-1},new(){X=1,Z=-1},new(){X=1,Z=1},new(){X=-1,Z=1}],
   CanUpgrade=Enumerable.Repeat(true,5).ToArray(),UpgradeCosts=[1,1,1,1,1],Units=[new(){Index=1,Id=1,Grid=0,Ready=true}],
   Catalog=new(){Key="opening-core",SummonCost=25,GameOverCount=100,MaxGrade=8,MaxUpgrade=99,Advantage=1,Penalty=.5,
    Units=[new(){Id=1,Element=1,Grade=1,Attack=8,UpAttack=999,Interval=1,Range=20,Sell=8,Weight=100},
     new(){Id=70,Element=0,Grade=7,Attack=100,UpAttack=100,Interval=.5,Range=20,Sell=200,Weight=15},
     new(){Id=84,Element=4,Grade=8,Attack=100,UpAttack=100,Interval=.1,Range=20,Sell=300,Weight=6}],
    Waves=[new(){Id=1,Hp=1000,Count=60,Duration=30,Speed=1,SpawnInterval=.5},new(){Id=11,Hp=10000,Count=60,Duration=30,Speed=1,SpawnInterval=.5},new(){Id=50,Hp=1000000,Count=1,Duration=60,Boss=true,Speed=1}]}};
  Control Command(Snapshot s,Decision a)=>new(){Enabled=true,Owner=new('b',32),Account=s.Account,Room=s.Room,ProcessId=s.ProcessId,ProcessStart=s.ProcessStart,Expires=now+TimeSpan.FromSeconds(10).Ticks,Action=a};
  var p=new Planner();var s=Sample();var settings=new Settings{Rare=false};
  for(int wave=1;wave<=10;wave++){s.Wave=wave;check(p.Decide(s,settings).Kind=="summon","opening roll precedes even extremely profitable off-colour upgrades, wave "+wave);}
  s.Gold=24;s.CanSummon=false;check(p.Decide(s,settings).Kind=="wait","opening insufficient gold does not divert to upgrades");
  var income=Sample();income.Gold=17;income.CanSummon=false;
  income.Catalog.Units=income.Catalog.Units.Append(new UnitDef{Id=50,Element=0,Grade=5,Attack=100,Interval=.2,Range=20,Sell=50}).ToArray();
  income.Catalog.Waves[0].Hp=10;
  income.Units=[new(){Id=50,Index=1,Grid=0,Ready=true},new(){Id=50,Index=2,Grid=1,Ready=true},new(){Id=1,Index=3,Grid=2,Ready=true}];
  check(new Planner().Decide(income,settings) is {Kind:"sell",UnitIndex:3},"spare slots do not prevent recycling a weak card while strong earners remain");
  income.EnemyCount=60;check(new Planner().Decide(income,settings).Kind=="wait","opening recycling waits during high pressure");
  income.EnemyCount=0;income.Catalog.Waves[0].Hp=100000;
  check(new Planner().Decide(income,settings).Kind=="wait","opening recycling preserves income when estimated damage is insufficient");
  income.Catalog.Waves[0].Hp=10;income.Catalog.Waves[0].Boss=true;
  check(new Planner().Decide(income,settings).Kind=="wait","opening recycling does not interrupt a boss wave");
  s.Units=s.Boards.Select(b=>new Unit{Index=b.Id+1,Id=1,Grid=b.Id,Ready=true}).ToArray();s.Gold=17;
  var reroll=p.Decide(s,settings);check(reroll.Kind=="sell"&&reroll.Policy=="clear50-opening","full opening board sells an affordable weakest card to reroll");
  s.Gold=0;check(p.Decide(s,settings).Kind=="wait","full opening board does not sell without replacement funds");
  s.Wave=11;var decision=p.Decide(s,settings);check(decision.Kind=="restart","observed opening without core restarts only after ten waves");
  check(OpeningRestart.Reject(decision,s)=="","native restart guard accepts current validated opening");
  check(new Planner().Decide(s,settings).Kind!="restart","late attachment never abandons a round retrospectively");
  check(p.Decide(s,new(){AutoNext=false}).Kind!="restart"&&p.Decide(s,new(){StopAfterRound=true}).Kind!="restart","auto-next and stop-after-round settings suppress restart");
  s.Units[0].Id=70;check(OpeningRestart.Reject(decision,s)!="","newly arrived seven-tier core cancels stale restart");
  var coreDecision=p.Decide(s,settings);check(coreDecision.Kind!="restart"&&!(coreDecision.Kind=="upgrade"&&coreDecision.Element==1),"high tier core keeps run and off-colour upgrade is forbidden");
  s.Units[0].Id=84;s.Wave=50;s.Gold=100;s.EnemyCount=1;
  check(p.Decide(s,settings) is not {Kind:"upgrade",Element:1},"single-target top core does not buy random element upgrades at boss");
  var reserve=Sample();reserve.Units=[new(){Id=84,Index=1,Grid=0,Ready=true}];reserve.Wave=11;
  reserve.Gold=25;reserve.UpgradeCosts=[60,1,1,1,60];reserve.CanUpgrade[4]=false;
  check(new Planner().Decide(reserve,settings) is {Kind:"wait"},"core planning saves toward an unaffordable upgrade instead of repeatedly spending 25 on rolls");
  reserve.Gold=60;reserve.CanUpgrade[4]=true;
  check(new Planner().Decide(reserve,settings) is {Kind:"upgrade",Element:4,Policy:"clear50-core"},"saved gold funds the core upgrade once available");
  reserve.Levels[4]=reserve.Catalog.MaxUpgrade;
  check(new Planner().Decide(reserve,settings).Kind!="wait","maxed core does not reserve for an impossible upgrade");
  reserve=Sample();reserve.Units=[new(){Id=84,Index=1,Grid=0,Ready=true}];reserve.Catalog.Waves=reserve.Catalog.Waves.Take(1).ToArray();reserve.Catalog.Waves[0].Hp=1;reserve.UpgradeCosts[4]=60;reserve.Gold=25;
  check(new Planner().Decide(reserve,settings).Kind=="summon","adequate future output permits additional rolls instead of hoarding indefinitely");
  var recover=Sample();recover.Wave=11;recover.Gold=25;recover.UpgradeCosts=[60,60,60,60,60];recover.CanUpgrade=Enumerable.Repeat(false,5).ToArray();
  recover.Units=[new(){Id=70,Index=1,Grid=0,Ready=true},new(){Id=1,Index=2,Grid=1,Ready=true}];
  check(new Planner().Decide(recover,settings) is {Kind:"sell",UnitIndex:2},"saving for core upgrades does not indefinitely block recovery of safe off-colour funds");
  var saleState=JsonFiles.Clone(recover);var port=new FakePort();var controller=new Controller(port);controller.Start(saleState,settings,now);controller.Poll(saleState,now);
  var sale=port.Last!;check(sale.Action is {Kind:"sell",Policy:"clear50-core"},"controller dispatches a core-consolidation sale");
  saleState.Units=saleState.Units.Where(u=>u.Index!=sale.Action.UnitIndex).ToArray();saleState.Gold+=8;saleState.Owner=sale.Owner;saleState.Ack=sale.Command;saleState.AckResult="ok";
  saleState.At=now+TimeSpan.FromSeconds(1).Ticks;controller.Poll(saleState,saleState.At);saleState.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(saleState,saleState.At);
  check(port.Last!.Command==0,"confirmed consolidation keeps the upgrade reserve instead of forcing a 25-gold replacement");
  saleState.Gold=60;saleState.CanUpgrade[0]=true;saleState.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(saleState,saleState.At);
  check(port.Last!.Action is {Kind:"upgrade",Element:0},"consolidation resumes the affordable core upgrade");controller.Stop();
  recover.EnemyCount=80;
  check(new Planner().Decide(recover,settings).Kind=="wait","saving does not force sales while the board is under pressure");
  var support=Sample();support.Wave=11;support.Units=[new(){Id=70,Index=1,Grid=0,Ready=true},new(){Id=1,Index=2,Grid=1,Ready=true}];
  support.Catalog.Units[0].Element=4;support.Catalog.Units[0].UpAttack=100;support.UpgradeCosts=[100,1,1,1,12];support.Gold=12;
  check(new Planner().Decide(support,settings) is {Kind:"upgrade",Element:4},"low-tier dark support is eligible when its marginal gain per gold beats the water core");
  var crowd=Sample();crowd.Wave=11;crowd.Gold=12;crowd.EnemyCount=66;crowd.UpgradeCosts=[12,1,1,1,132];
  crowd.CanUpgrade[4]=false;crowd.Units=[new(){Id=84,Index=1,Grid=0,Ready=true},new(){Id=70,Index=2,Grid=1,Ready=true}];
  crowd.Catalog.Units[1].UpAttack=1;
  check(new Planner().Decide(crowd,settings) is {Kind:"upgrade",Element:0},"live crowd pressure funds an affordable water improvement instead of waiting for the more efficient dark core upgrade");
  crowd.EnemyCount=0;check(new Planner().Decide(crowd,settings).Kind=="wait","safe lane retains the higher-efficiency future upgrade reserve");
  crowd.EnemyCount=66;crowd.Gold=11;check(new Planner().Decide(crowd,settings).Kind=="wait","pressure cannot spend unavailable gold or buy an unrelated cheap element");
  crowd.Gold=12;crowd.Catalog.Units[1].UpAttack=0;
  check(new Planner().Decide(crowd,settings).Kind=="wait","pressure never buys a zero-output improvement");
  var recruit=Sample();recruit.Wave=11;recruit.Units=[new(){Id=84,Index=1,Grid=0,Ready=true}];recruit.Levels[4]=3;
  recruit.Catalog.Waves=recruit.Catalog.Waves.Concat(new[]{12,36}.Select(id=>new WaveDef{Id=id,Hp=10000,Count=60,Duration=30,Speed=1,SpawnInterval=.5})).OrderBy(w=>w.Id).ToArray();
  recruit.UpgradeCosts[4]=60;recruit.CanUpgrade[4]=false;recruit.Gold=25;var recruitPlanner=new Planner();
  for(int roll=0;roll<2;roll++){var a=recruitPlanner.Decide(recruit,settings);check(a is {Kind:"summon",Policy:"clear50-recruit"},"missing complementary role gets a bounded roll, "+roll);recruitPlanner.Confirm(a,recruit,now);}
  check(recruitPlanner.Decide(recruit,settings).Kind=="wait","two confirmed complement rolls preserve the rest of this wave's upgrade budget");
  recruit.Wave=12;check(recruitPlanner.Decide(recruit,settings) is {Kind:"summon",Policy:"clear50-recruit"},"next safe wave renews the complement budget");
  recruit.EnemyCount=40;check(recruitPlanner.Decide(recruit,settings).Kind=="wait","accumulating enemies suppress speculative complement rolls");
  recruit.EnemyCount=0;recruit.Gold=60;recruit.CanUpgrade[4]=true;
  check(recruitPlanner.Decide(recruit,settings) is {Kind:"upgrade",Element:4},"affordable core investment precedes complement rolls");
  recruit.Gold=25;recruit.CanUpgrade[4]=false;recruit.Wave=36;
  check(recruitPlanner.Decide(recruit,settings).Kind=="wait","late waves preserve funds rather than chasing a new complement");
  var flexible=Sample();flexible.Wave=11;flexible.Units=[new(){Id=70,Index=1,Grid=0,Ready=true},new(){Id=1,Index=2,Grid=1,Ready=true}];flexible.Catalog.Units[0].Grade=5;
  check(new Planner().Decide(flexible,settings) is {Kind:"upgrade",Element:1},"useful tier-five third element is retained and upgraded instead of discarded as junk");
  s=Sample();s.Wave=11;var command=Command(s,decision);var tracker=new OpeningRestart();tracker.Begin(command,now);
  s.Stage="result";tracker.Observe(s,command,now);check(s.RestartPending&&OpeningRestart.Reject(decision,s)=="","owned intent survives native result menu");
  check(!GameFlow.CanSettle(s),"intent never forges a completed match or score");
  var unowned=Sample();unowned.Stage="result";check(OpeningRestart.Reject(decision,unowned)!="","manual live result popup cannot authorize giving up");
  s.Stage="confirm-exit";tracker.Observe(s,command,now);check(p.Decide(s,settings).Kind=="restart","owned restart can resume on native give-up confirmation");
  var result=Sample();result.Stage="result";result.Room=s.Room;
  check(GameFlow.Receipt("restart",result,s),"native give-up confirmation completes result-exit click");
  command.Enabled=false;tracker.Observe(s,command,now);check(!s.RestartPending&&OpeningRestart.Reject(decision,s)!="","pause cancels give-up authority");
  command.Enabled=true;tracker.Begin(command,now);command.Owner=new('c',32);tracker.Observe(s,command,now);
  check(!s.RestartPending,"mutating the original command cannot transfer restart authority");
  command=Command(s,decision);tracker.Begin(command,now);var replacement=Command(s,decision);replacement.Owner=new('d',32);tracker.Observe(s,replacement,now);
  check(!s.RestartPending,"new controller cannot inherit restart intent");
  tracker.Begin(command,now);s.Room="new-room";tracker.Observe(s,command,now);check(!s.RestartPending,"new match clears restart intent");
  s.Room=command.Room;tracker.Begin(command,now);s.Stage="lobby";tracker.Observe(s,command,now);check(!s.RestartPending,"lobby clears restart intent before next match");
  s.Stage="confirm-exit";tracker.Begin(command,now);command.Expires=now+TimeSpan.FromMinutes(3).Ticks;tracker.Observe(s,command,now+TimeSpan.FromMinutes(3).Ticks);
  check(!s.RestartPending,"abandoned restart intent expires instead of authorizing an unrelated future popup");
 }
}
