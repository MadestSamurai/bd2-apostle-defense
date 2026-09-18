using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.Text.Json;
using BD2ApostleDefense;

if(args.Length==2&&args[0]=="--export-strings"){File.WriteAllText(args[1],JsonSerializer.Serialize(LocalizationTests.Sources(""),new JsonSerializerOptions{WriteIndented=true,Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping}));return;}
int passed=0;var checks=new List<string>();
void Check(bool ok,string label){if(!ok)throw new Exception(label);passed++;checks.Add(label);}
long now=DateTime.UtcNow.Ticks;
var notice=new SurfaceInfo{Type="NoticeUI",RegisteredName="",Active=true,Popup=true,GameHud=false,EmbeddedDefenseNotice=true};
Check(!PopupPolicy.IsBlocking(notice),"embedded DefenseHUD notice with no registry name does not block");
notice.EmbeddedDefenseNotice=false;Check(PopupPolicy.IsBlocking(notice),"unknown notice is not globally ignored");
notice.GameHud=true;Check(!PopupPolicy.IsBlocking(notice),"registered HUD remains nonmodal");
var modal=new SurfaceInfo{Type="NetworkErrorPopupUI",Active=true,Popup=true};Check(PopupPolicy.IsBlocking(modal),"real network modal still blocks");
modal.Active=false;Check(!PopupPolicy.IsBlocking(modal),"inactive popup does not block");
modal.Active=true;modal.NativeFlow=true;Check(!PopupPolicy.IsBlocking(modal),"explicit native flow remains dispatched by stage guards");
Snapshot Base()=>new(){At=now,ProcessId=10,ProcessStart=20,Account=new('a',64),Room="test",BoardKey="board",Ready=true,MyView=true,Stage="playing",Gold=45,CanSummon=true,Wave=1,SecondsLeft=30,
 Boards=Enumerable.Range(0,9).Select(i=>new Board{Id=i,X=(i%3-1)*3,Z=(i/3-1)*3}).ToArray(),Path=new[]{new Point{X=-5,Z=-5},new Point{X=5,Z=-5},new Point{X=5,Z=5},new Point{X=-5,Z=5}},
 Catalog=new(){SummonCost=25,MaxGrade=8,MaxUpgrade=99,GameOverCount=100,Advantage=1,Penalty=.5,Key="test",Units=new[]{new UnitDef{Id=1,Name="core",Element=0,Grade=5,Attack=100,UpAttack=100,Interval=.5,Range=20,Weight=100,Sell=50},new UnitDef{Id=2,Name="low",Element=1,Grade=1,Attack=5,UpAttack=5,Interval=2,Range=20,Weight=100,Sell=8}},Waves=new[]{new WaveDef{Id=1,Element=1,Count=60,Hp=100,Duration=30,Speed=1,SpawnInterval=.5},new WaveDef{Id=10,Element=1,Count=1,Hp=10000,Duration=60,Boss=true,Speed=1,SpawnInterval=1}}}};
Control Command(Snapshot s,string kind)=>new(){Enabled=true,ProcessId=s.ProcessId,ProcessStart=s.ProcessStart,Owner=new('b',32),Account=s.Account,Room=s.Room,BoardKey=s.BoardKey,SnapshotAt=s.At,Expires=now+TimeSpan.FromSeconds(10).Ticks,Command=1,Action=new(){Kind=kind}};
var s=Base();var c=Command(s,"summon");
Check(Guards.Reject(c,s,now)=="","legal summon");
c.Expires=now-1;Check(Guards.Reject(c,s,now)!="","expired lease");c=Command(s,"summon");
c.Account=new('c',64);Check(Guards.Reject(c,s,now)!="","account switch");c=Command(s,"summon");
c.ProcessStart++;Check(Guards.Reject(c,s,now)!="","PID reuse");c=Command(s,"summon");
c.Room="other";Check(Guards.Reject(c,s,now)!="","different match");c=Command(s,"summon");
c.BoardKey="changed";Check(Guards.Reject(c,s,now)!="","manual board change");c=Command(s,"summon");
c.SnapshotAt=now-TimeSpan.FromSeconds(4).Ticks;Check(Guards.Reject(c,s,now)!="","stale decision");c=Command(s,"summon");
s.Blocker="PurchasePopup";Check(Guards.Reject(c,s,now)!="","unrelated popup");s.Blocker="";
s.MyView=false;Check(Guards.Reject(c,s,now)!="","spectator board");s.MyView=true;
s.Gold=24;Check(Guards.Reject(c,s,now)!="","insufficient gold");s.Gold=45;
s.Units=new[]{new Unit{Index=1,Id=1,Grid=0,Ready=true}};c=Command(s,"sell");c.Action.UnitIndex=1;c.Action.UnitId=1;c.Action.Grid=0;
Check(Guards.Reject(c,s,now)!="","protect last unit");s.Units=s.Units.Concat(new[]{new Unit{Index=2,Id=2,Grid=1,Ready=true}}).ToArray();
Check(Guards.Reject(c,s,now)=="","confirmed sell identity");s.Units[0].Id=2;Check(Guards.Reject(c,s,now)!="","replaced target identity");s.Units[0].Id=1;
c.Action.Kind="move";c.Action.TargetGrid=1;c.Action.TargetIndex=-1;Check(Guards.Reject(c,s,now)!="","swap destination changed");c.Action.TargetIndex=2;
Check(Guards.Reject(c,s,now)=="","confirmed swap");
c=Command(s,"upgrade");c.Action.Element=0;s.UpgradeCosts[0]=18;Check(Guards.Reject(c,s,now)!="","disabled upgrade button");s.CanUpgrade[0]=true;Check(Guards.Reject(c,s,now)=="","legal upgrade");
Check(!Guards.Fresh(s,10,21,now),"freshness process identity");Check(Guards.Fresh(s,10,20,now),"freshness valid");
s.ClearProgress=1;s.RareProgress=2;s.RoundRare=1;s.AchievementsKnown=true;Check(!Guards.GoalsMet(s,new()),"pending rare is not server completion");s.RareProgress=3;Check(Guards.GoalsMet(s,new()),"both confirmed achievements");s.AchievementsKnown=false;Check(!Guards.GoalsMet(s,new()),"unknown progress never complete");
Check(Planner.ElementMultiplier(4,3,s.Catalog)==2&&Planner.ElementMultiplier(3,4,s.Catalog)==2,"mutual light dark advantage");Check(Planner.ElementMultiplier(0,2,s.Catalog)==.5,"tri-element penalty");
double[,] score={{10,9,0},{9,0,0},{0,8,7}};var layout=Planner.Assignment(score);Check(layout.Distinct().Count()==3&&Enumerable.Range(0,3).Sum(i=>score[i,layout[i]])==25,"exact assignment beats row greedy");
var rng=new Random(1826);int assignmentCases=0;
for(int n=0;n<=5;n++)for(int m=Math.Max(1,n);m<=7;m++)for(int run=0;run<10;run++){
 var matrix=new double[n,m];for(int i=0;i<n;i++)for(int j=0;j<m;j++)matrix[i,j]=rng.Next(-100,101)/10.0;
 double Brute(int row,int used){if(row==n)return 0;double best=double.NegativeInfinity;for(int col=0;col<m;col++)if((used&(1<<col))==0)best=Math.Max(best,matrix[row,col]+Brute(row+1,used|1<<col));return best;}
 var plan=Planner.Assignment(matrix);
 if(plan.Length!=n||plan.Distinct().Count()!=n||plan.Any(c=>c<0||c>=m)||Math.Abs(Enumerable.Range(0,n).Sum(i=>matrix[i,plan[i]])-Brute(0,0))>1e-8)throw new Exception("rectangular assignment brute-force parity");assignmentCases++;}
Check(assignmentCases==320,"320 empty/square/rectangular signed fractional matrices against exhaustive enumeration");
Check(Planner.Assignment(new double[0,0]).Length==0,"zero units and zero slots assignment");
foreach(int size in new[]{17,25,36,64,100})
{
 foreach(int count in new[]{0,1,size/2,size})
 {
  var matrix=new double[count,size];for(int i=0;i<count;i++){for(int j=0;j<size;j++)matrix[i,j]=-100;matrix[i,size-1-i]=1000+i;}
  var plan=Planner.Assignment(matrix);
  Check(plan.Length==count&&Enumerable.Range(0,count).All(i=>plan[i]==size-1-i),$"exact {count} units / {size} slots planted optimum");
 }
 var tied=new double[size,size];var first=Planner.Assignment(tied);
 Check(first.Distinct().Count()==size&&first.SequenceEqual(Planner.Assignment(tied)),$"{size} identical units preserve deterministic assignment");
}
bool invalid=false;try{Planner.Assignment(new double[3,2]);}catch(ArgumentException){invalid=true;}Check(invalid,"reject excess units without allocation");
foreach(double value in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity}){invalid=false;try{Planner.Assignment(new double[,]{{value}});}catch(ArgumentException){invalid=true;}Check(invalid,"nonfinite score rejected");}
Check(Planner.WorthMove(1,10),"move gate uses affected units only");
Check(Planner.WorthMove(.001,0),"zero-output unit can be rescued without whole-army threshold");
Check(!Planner.WorthMove(0,10)&&!Planner.WorthMove(-1,10),"neutral and losing swaps never qualify");
Check(!Planner.WorthMove(1e-8,0),"floating point noise never causes movement");
Check(!Planner.WorthMove(.025,1)&&Planner.WorthMove(.026,1),"local relative gate prevents trivial churn");
Check(Planner.CanReroll(0,3185,17500,21,100,false,1),"zero-output unit can reroll when absolute army estimate is below requirement");
Check(Planner.CanReroll(13,3185,17500,21,100,false,1),"ordinary low-pressure wave permits less than one percent temporary output loss");
Check(!Planner.CanReroll(40,3185,17500,21,100,false,1),"underpowered army retains units contributing over one percent");
Check(!Planner.CanReroll(13,3185,17500,50,100,false,30),"crowded route protects nonzero output");
Check(Planner.CanReroll(0,3185,17500,95,100,false,30),"crowding does not protect an unreachable unit");
Check(!Planner.CanReroll(13,3185,17500,1,100,true,60),"boss has no weak-unit exception without sufficient reserve");
Check(!Planner.CanReroll(13,30000,100,1,100,true,10),"boss last seconds retain useful output");
Check(Planner.CanReroll(0,3185,17500,1,100,true,10),"boss last seconds still permits zero-loss reroll");
Check(Planner.CanReroll(100,30000,17500,21,100,false,30),"old sufficient-reserve path remains available");
Board[] Grid(int count){int width=(int)Math.Ceiling(Math.Sqrt(count));return Enumerable.Range(0,count).Select(i=>new Board{Id=100+i*3,X=-3+6.0*(i%width)/Math.Max(1,width-1),Z=-3+6.0*(i/width)/Math.Max(1,width-1)}).ToArray();}
var planner=new Planner();
var occupiedEdge=Base();occupiedEdge.Boards=new[]{new Board{Id=0},new Board{Id=1,Z=4.5}};
occupiedEdge.Catalog.Units[0].Attack=1000000;occupiedEdge.Catalog.Units[0].Range=100;occupiedEdge.Catalog.Units[1].Range=1;
occupiedEdge.Units=new[]{new Unit{Index=1,Id=2,Grid=0,Ready=true},new Unit{Index=2,Id=1,Grid=1,Ready=true}};
var edgeSwap=planner.Decide(occupiedEdge,new());Check(edgeSwap.Kind=="move"&&edgeSwap.UnitIndex==1&&edgeSwap.TargetIndex==2,"strong ranged occupant with unchanged coverage does not block melee rescue");CheckDecision(edgeSwap,occupiedEdge);
foreach(int size in new[]{17,25,36,64,100})
{
 s=Base();s.Boards=Grid(size);Check(planner.Decide(s,new()).Kind=="summon",$"{size}-slot empty board starts summoning");
 s.Units=new[]{new Unit{Index=1,Id=1,Grid=s.Boards[0].Id,Ready=true}};
 Check(planner.Decide(s,new()).Kind=="summon",$"{size}-slot partial board keeps filling with noncontiguous IDs");
 s.Units=s.Boards.Select((b,i)=>new Unit{Index=i+1,Id=1,Grid=b.Id,Ready=true}).ToArray();
 s.CanSummon=false;s.CanUpgrade[0]=true;s.UpgradeCosts[0]=18;s.SecondsLeft=1;s.Wave=10;
 Check(planner.Decide(s,new()).Kind=="upgrade",$"{size}-slot full board proceeds to upgrades");
 var bigPort=new FakePort();var bigController=new Controller(bigPort);bigController.Start(s,new(),now);bigController.Poll(s,now);
 Check(bigPort.Last!.Action.Kind=="upgrade"&&Guards.Reject(bigPort.Last,s,now)=="",$"{size}-slot decision reaches command dispatch");bigController.Stop();
}
s=Base();s.Boards=Array.Empty<Board>();Check(planner.Decide(s,new()).Kind=="wait","incomplete board waits without throwing");
s=Base();s.Units=new[]{new Unit{Index=1,Id=1,Grid=999,Ready=true}};Check(planner.Decide(s,new()).Kind=="wait","missing unit position waits for synchronization");
s=Base();Check(planner.Decide(s,new()).Kind=="summon","opening fills empty board");s.Stage="lobby";Check(planner.Decide(s,new()).Kind=="refresh","lobby refresh precedes matching");s.AchievementsKnown=true;Check(planner.Decide(s,new()).Kind=="start","start after progress confirmed");Check(planner.Decide(s,new(){AutoNext=false}).Kind=="wait","manual next game");Check(planner.Decide(s,new(){StopAfterRound=true}).Kind=="complete","stop after settled round");
s.Stage="result";Check(planner.Decide(s,new()).Kind=="wait","never give up a live match");s.Dead=true;Check(planner.Decide(s,new()).Kind=="settle","native result settlement");
s=Base();s.Units=Enumerable.Range(0,9).Select(i=>new Unit{Index=i+1,Id=i<8?1:2,Grid=i,Ready=true}).ToArray();s.EnemyCount=90;
Check(planner.Decide(s,new()).Kind!="sell","protect army when overwhelmed");
s=Base();s.Units=new[]{new Unit{Index=1,Id=1,Grid=0,Ready=true}};s.AchievementsKnown=true;s.ClearProgress=1;planner.Decide(s,new());Check(planner.Focus.StartsWith("最高级"),"switch to rare after confirmed clear");s.RareProgress=2;s.RoundRare=1;planner.Decide(s,new());Check(!planner.Focus.StartsWith("最高级"),"no extra rare rolls after pending target reached");
var port=new FakePort();var controller=new Controller(port);s=Base();controller.Start(s,new(),now);controller.Poll(s,now);var sent=port.Last!;Check(sent.Command==1&&sent.Action.Kind=="summon","controller sends one command");controller.Poll(s,now+1000);Check(port.Last!.Command==1,"no duplicate sequence while waiting");controller.Poll(null,now+TimeSpan.FromSeconds(6).Ticks);Check(controller.Running,"missing snapshot retains enabled intent");
s.Owner=sent.Owner;s.Ack=1;s.AckResult="ok";s.At=now+TimeSpan.FromSeconds(7).Ticks;controller.Poll(s,s.At);Check(port.Last!.Command==0,"ack now waits configured interval");controller.Stop();Check(!port.Last!.Enabled,"pause disables lease");controller.Poll(s,s.At+1);Check(!port.Last.Enabled,"paused poll cannot restart");
controller.Start(s,new(),s.At);s.Account=new('c',64);controller.Poll(s,s.At);Check(!controller.Running,"account swap stops controller");
// Actual client flow: local death/win -> result -> exit confirmation -> EcsExit -> lobby;
// whole room end -> result -> lobby. Loading is not a completed receipt.
foreach(string ending in new[]{"dead","win","room-end"})
{
 var during=Base();during.Units=new[]{new Unit{Index=1,Id=1,Grid=0,Ready=true}};
 var ended=Base();ended.Ready=false;ended.MyView=false;ended.Stage="ended";
 ended.Dead=ending=="dead";ended.Win=ending=="win";ended.RoomEnded=ending=="room-end";
 foreach(var kind in new[]{"summon","sell","move","upgrade"})Check(GameFlow.Superseded(kind,during,ended),ending+" interrupts pending "+kind);
 Check(!GameFlow.Superseded("settle",during,ended),ending+" does not interrupt settlement");
 Check(planner.Decide(ended,new()).Kind=="open-result",ending+" opens result without board readiness");
 var result=JsonFiles.Clone(ended);result.Stage="result";
 Check(GameFlow.Receipt("open-result",ended,result),ending+" result popup receipt");
 Check(Guards.Reject(Command(result,"settle"),result,now)=="",ending+" valid settle without board");
 Check(planner.Decide(result,new(){AutoNext=false}).Kind=="settle",ending+" exits even with next match disabled");
 var loading=Base();loading.Stage="waiting";loading.Room="";loading.Ready=false;
 Check(!GameFlow.Receipt("settle",result,loading),ending+" does not acknowledge transient loading");
 Check(!GameFlow.Receipt("settle",result,result),ending+" waits for actual native change");
 var returnLobby=Base();returnLobby.Stage="lobby";returnLobby.Room="";returnLobby.Ready=false;
 var finalBefore=result;string exitKind="settle";
 if(ending!="room-end")
 {
  var confirm=JsonFiles.Clone(result);confirm.Stage="confirm-exit";
  Check(GameFlow.Receipt("settle",result,confirm),ending+" exit popup requires confirmation");
  Check(planner.Decide(confirm,new()).Kind=="confirm-exit",ending+" confirms native exit");
  Check(Guards.Reject(Command(confirm,"confirm-exit"),confirm,now)=="",ending+" confirmed exit allowed");
  var submitting=JsonFiles.Clone(confirm);submitting.Exiting=true;submitting.Dead=submitting.Win=false;submitting.Stage="settling";
  Check(planner.Decide(submitting,new()).Kind=="wait",ending+" EcsExit does not click twice");
  Check(!GameFlow.Receipt("confirm-exit",confirm,submitting),ending+" EcsExit is not returned lobby");
  Check(!GameFlow.Receipt("confirm-exit",confirm,loading),ending+" confirm loading not completed");
  Check(Guards.Reject(Command(submitting,"confirm-exit"),submitting,now)!="",ending+" rejects repeat exit");
  finalBefore=confirm;exitKind="confirm-exit";
 }
 Check(GameFlow.Receipt(exitKind,finalBefore,returnLobby),ending+" confirms real lobby return");
 returnLobby.Blocker="NetworkErrorPopupUI";Check(!GameFlow.Receipt(exitKind,finalBefore,returnLobby),ending+" real modal blocks completion");returnLobby.Blocker="";
 Check(planner.Decide(returnLobby,new()).Kind=="refresh",ending+" refreshes achievements before rematch");
 returnLobby.AchievementsKnown=true;Check(planner.Decide(returnLobby,new()).Kind=="start",ending+" rematches after server progress");
 Check(planner.Decide(returnLobby,new(){AutoNext=false}).Kind=="wait",ending+" disabled next match stays lobby");
 Check(planner.Decide(returnLobby,new(){StopAfterRound=true}).Kind=="complete",ending+" stop after round waits for lobby");
}
s=Base();s.Stage="confirm-exit";Check(planner.Decide(s,new()).Kind=="wait"&&Guards.Reject(Command(s,"confirm-exit"),s,now)!="","live match confirmation never abandoned");
s.Exiting=true;s.Dead=true;Check(!GameFlow.CanSettle(s),"exit already submitted cannot be clicked twice even with room end");
s.Stage="lobby";Check(planner.Decide(s,new()).Kind=="wait","resuming during native teardown does not start new lobby work");
s=Base();var stale=Base();stale.Stage="waiting";Check(!GameFlow.Receipt("match-start",s,stale),"empty loading screen does not acknowledge match start");
Check(!GameFlow.Superseded("summon",s,stale),"transient loading does not invent a finished round");
Check(!GameFlow.Superseded("upgrade",s,s),"ongoing match still awaits actual upgrade receipt");
// Exercise the desktop lease/controller after a board action is superseded by an ending.
port=new FakePort();controller=new Controller(port);s=Base();controller.Start(s,new(),now);controller.Poll(s,now);sent=port.Last!;
s.Stage="result";s.Dead=true;s.Ready=false;s.MyView=false;s.Owner=sent.Owner;s.Ack=sent.Command;s.AckResult="superseded";s.At=now+TimeSpan.FromSeconds(1).Ticks;
controller.Poll(s,s.At);s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);
Check(controller.Running&&port.Last!.Action.Kind=="settle","superseded action continues into settlement instead of stopping");
sent=port.Last!;s.Stage="confirm-exit";s.Ack=sent.Command;s.AckResult="ok";s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);
Check(port.Last!.Action.Kind=="confirm-exit","controller proceeds through native confirmation");
sent=port.Last!;s.Dead=false;s.Exiting=true;s.Stage="settling";s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);
Check(port.Last!.Command==sent.Command&&controller.Running,"controller does not replace a submitting exit command");
s.Exiting=false;s.Stage="lobby";s.Room="";s.Ack=sent.Command;s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);
Check(port.Last!.Action.Kind=="refresh","controller refreshes actual server goals after exit");
sent=port.Last!;s.AchievementsKnown=true;s.Ack=sent.Command;s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);
Check(port.Last!.Action.Kind=="start","controller restarts only after successful achievement refresh");controller.Stop();
Snapshot SellingScenario(){var x=Base();x.Units=Enumerable.Range(0,9).Select(i=>new Unit{Index=i+1,Id=i<8?1:2,Grid=i,Ready=true}).ToArray();return x;}
port=new FakePort();controller=new Controller(port);s=SellingScenario();controller.Start(s,new(),now);controller.Poll(s,now);sent=port.Last!;
Check(sent.Action.Kind=="sell","controller can choose low-value full-board reroll");
int soldGrid=sent.Action.Grid;s.Units=s.Units.Where(u=>u.Index!=sent.Action.UnitIndex).ToArray();s.Gold+=8;s.CanSummon=true;
s.Catalog.Units[0].UpAttack=1000000;s.CanUpgrade[0]=true;s.UpgradeCosts[0]=18;
Check(new Planner().Decide(s,new()).Kind=="upgrade","without refill intent competing upgrade would consume reroll money");
s.Owner=sent.Owner;s.Ack=sent.Command;s.AckResult="ok";s.At=now+TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);
s.Blocker="NetworkErrorPopupUI";s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);Check(port.Last!.Command==0,"refill respects blocking modal");
s.Blocker="";s.Stage="waiting";s.Room="";s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);Check(port.Last!.Command==0,"refill waits through transient loading");
s.Stage="playing";s.Room="test";s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);sent=port.Last!;
Check(sent.Action.Kind=="summon"&&sent.Action.Reason.Contains("出售已确认"),"successful sale prioritizes refill over moves and upgrades");
s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);Check(port.Last!.Command==sent.Command,"refill does not send duplicate summons before receipt");
s.Units=s.Units.Concat(new[]{new Unit{Index=100,Id=2,Grid=soldGrid,Ready=true}}).ToArray();s.Ack=sent.Command;s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);
Check(port.Last!.Action.Kind=="upgrade","refill confirmation resumes ordinary optimization");controller.Stop();
port=new FakePort();controller=new Controller(port);s=SellingScenario();controller.Start(s,new(),now);controller.Poll(s,now);sent=port.Last!;
s.Owner=sent.Owner;s.Ack=sent.Command;s.AckResult="ok";s.Dead=true;s.Stage="result";s.At=now+TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);s.At+=TimeSpan.FromSeconds(1).Ticks;controller.Poll(s,s.At);
Check(port.Last!.Action.Kind=="settle","round end supersedes refill intent");controller.Stop();
// DataContract (game Mono) and System.Text.Json (desktop) must agree on fields and inherited coordinates.
s=Base();using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(Snapshot)).WriteObject(stream,s);stream.Position=0;var decoded=JsonSerializer.Deserialize<Snapshot>(stream,JsonFiles.Options)!;Check(decoded.Boards.Length==9&&decoded.Boards[0].X==-3,"snapshot serialization parity");}
using(var stream=new MemoryStream(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Command(s,"summon"),JsonFiles.Options)))){var decoded=(Control)new DataContractJsonSerializer(typeof(Control)).ReadObject(stream)!;Check(decoded.Action.Kind=="summon"&&decoded.Account==s.Account,"control serialization parity");}
// Public CI uses generated fixtures only. No private captures or game table exports.
s=Base();s.Catalog=PublicFixtures.Catalog();
var captured=PublicFixtures.Board(now);var fixtureSolver=new Planner();var fixtureFirst=fixtureSolver.Decide(captured,new());
Check(fixtureFirst.Kind=="move"&&fixtureFirst.Score>0,"synthetic melee rescue yields positive native move");CheckDecision(fixtureFirst,captured);
var positions=JsonFiles.Clone(captured);var seenLayouts=new HashSet<string>();int placementMoves=0;
for(;placementMoves<200;placementMoves++)
{
 string key=string.Join(";",positions.Units.OrderBy(u=>u.Index).Select(u=>$"{u.Index}:{u.Grid}"));if(!seenLayouts.Add(key))throw new Exception("placement oscillated on generated board");
 var next=fixtureSolver.Decide(positions,new());if(next.Kind!="move")break;CheckDecision(next,positions);
 positions.Units.Single(u=>u.Index==next.UnitIndex).Grid=next.TargetGrid;
 if(next.TargetIndex>=0)positions.Units.Single(u=>u.Index==next.TargetIndex).Grid=next.Grid;
 positions.BoardKey=Guards.BoardKey(positions);
}
Check(placementMoves>0&&placementMoves<200,"generated 68-slot placement converges without repeated layouts");
Check(fixtureSolver.Decide(positions,new()).Kind!="move","settled generated layout remains stable");
var late=SellingScenario();var lateDecision=new Planner().Decide(late,new());int lateMoves=0;
Check(lateDecision.Kind=="sell"&&lateDecision.Score>0,"synthetic full-board positive-value reroll");
s.Units=Enumerable.Range(0,9).Select(i=>new Unit{Index=i+1,Id=s.Catalog.Units[10+i].Id,Grid=i,Ready=true}).ToArray();s.Gold=300;s.Levels=new[]{3,2,4,5,7};s.UpgradeCosts=new[]{45,36,54,36,48};s.CanUpgrade=Enumerable.Repeat(true,5).ToArray();
var watch=Stopwatch.StartNew();for(int i=0;i<200;i++){s.Wave=i%50+1;var decision=planner.Decide(s,new());CheckDecision(decision,s);}
watch.Stop();Check(watch.Elapsed.TotalMilliseconds/200<100,"planner <100ms average on generated rules");
var scaleBench=new List<object>();
foreach(int size in new[]{17,25,36,64,100})
{
 var sample=JsonFiles.Clone(s);sample.Boards=Grid(size);sample.Units=sample.Boards.Select((b,i)=>new Unit{Index=i+1,Id=sample.Catalog.Units[i%sample.Catalog.Units.Length].Id,Grid=b.Id,Ready=true}).ToArray();sample.CanSummon=false;
 var solver=new Planner();var elapsed=Stopwatch.StartNew();var decision=solver.Decide(sample,new());double cold=elapsed.Elapsed.TotalMilliseconds;CheckDecision(decision,sample);
 elapsed.Restart();for(int i=0;i<100;i++){sample.Wave=i%50+1;CheckDecision(solver.Decide(sample,new()),sample);}elapsed.Stop();double mean=elapsed.Elapsed.TotalMilliseconds/100;
 Check(cold<300&&mean<100,$"{size}-slot full board generated-rule planner latency");scaleBench.Add(new{slots=size,coldMs=cold,meanMs=mean});
}
void CheckDecision(Decision d,Snapshot state){if(d.Kind is "wait" or "complete")return;var command=Command(state,d.Kind);command.Action=d;var error=Guards.Reject(command,state,now);if(error!="")throw new Exception(d.Kind+": "+error);}
// The board inspector receives an isolated pending command, never the mutable controller action.
var inspectPort=new FakePort();var inspectController=new Controller(inspectPort);var inspectState=Base();
Check(inspectController.PendingDecision==null,"idle inspector has no pending command");
inspectController.Start(inspectState,new(),now);inspectController.Poll(inspectState,now);
var exposed=inspectController.PendingDecision;Check(exposed?.Kind=="summon","inspector sees dispatched command");
exposed!.Kind="sell";Check(inspectController.PendingDecision?.Kind=="summon"&&inspectPort.Last!.Action.Kind=="summon","inspection cannot mutate dispatched action");
inspectController.Stop();Check(inspectController.PendingDecision==null,"paused inspector clears pending command");
var bossChaseRegression=BossChaseTests.Run(Check,s.Catalog,captured,now);
int localizationChecks=LocalizationTests.Run();
string output=args.Length>0?args[0]:Path.Combine(AppContext.BaseDirectory,"test-data","results");Directory.CreateDirectory(output);JsonFiles.Write(Path.Combine(output,"tests.json"),new{status="passed",passed,localizationChecks,checks,assignmentCases,scaleBench,bossChaseRegression,meleeRegression=new{fixtureFirst,placementMoves},rerollRegression=new{lateDecision,lateMoves},plannerMeanMs=watch.Elapsed.TotalMilliseconds/200});Console.WriteLine($"PASS {passed} checks; planner mean {watch.Elapsed.TotalMilliseconds/200:0.00} ms; melee placement {placementMoves} steps; late reroll {lateDecision.Kind} after {lateMoves} moves; scaling {JsonSerializer.Serialize(scaleBench)}");

sealed class FakePort:IClientPort
{
 public Control? Last;public GameProcess? Find()=>new(10,20,"fake");public Snapshot? Read()=>null;
 public void Write(Control c){Last=JsonFiles.Clone(c);}public Task ConnectAsync(Action<string> p,CancellationToken token)=>Task.CompletedTask;
}
