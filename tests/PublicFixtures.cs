using BD2ApostleDefense;
// Generated scenarios: no account IDs, sessions, inventories, private matches or game tables.
internal static class PublicFixtures
{
 public static Catalog Catalog()=>new(){GameOverCount=100,SummonCost=25,MaxGrade=8,MaxUpgrade=99,Advantage=1,Penalty=.5,Key="generated-v1",
  Units=Enumerable.Range(1,8).SelectMany(g=>Enumerable.Range(0,5).Select(e=>new UnitDef{Id=g*10+e,Grade=g,Element=e,Attack=g*10,UpAttack=g*10,Interval=.5,Range=g<5?2:3.75,Weight=10*(9-g),Sell=g*5,Name=""})).ToArray(),
  Waves=Enumerable.Range(1,50).Select(i=>new WaveDef{Id=i,EnemyTable=1000+i,Element=i%5,Hp=i*42000,Count=i%10==0?i==30?2:i==40?3:1:30,Boss=i%10==0,Duration=120,SpawnInterval=.5,Speed=3,Gold=1}).ToArray()};
 public static Snapshot Board(long now)
 {
  var boards=new List<Board>();
  for(int c=0;c<10;c++){int inset=c is 0 or 9?2:c is 1 or 8?1:0;for(int r=inset;r<8-inset;r++)boards.Add(new(){Id=boards.Count+1,X=(c-4.5)*1.084638,Z=(3.5-r)*1.084638});}
  var s=new Snapshot{At=now,ProcessId=10,ProcessStart=20,Account=new('a',64),Room="generated",Stage="playing",MyView=true,Ready=true,Wave=7,SecondsLeft=30,Catalog=Catalog(),Boards=boards.ToArray(),Gold=0,
   Path=new[]{new Point{Id=0,X=-3.8,Z=4.88},new Point{Id=1,X=-5.96,Z=2.71},new Point{Id=2,X=-5.96,Z=-2.71},new Point{Id=3,X=-3.8,Z=-4.88},new Point{Id=4,X=3.8,Z=-4.88},new Point{Id=5,X=5.96,Z=-2.71},new Point{Id=6,X=5.96,Z=2.71},new Point{Id=7,X=3.8,Z=4.88}}};
  s.Units=new[]{new Unit{Id=14,Index=1,Grid=30,Ready=true},new Unit{Id=80,Index=2,Grid=1,Ready=true}};s.BoardKey=Guards.BoardKey(s);return s;
 }
}
