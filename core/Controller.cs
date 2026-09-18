namespace BD2ApostleDefense;
public sealed class Controller
{
 private readonly object sync=new();private readonly IClientPort port;private readonly Planner planner=new();private Control? active;private Settings settings=new();private long command,nextAction;
 private bool refillPending;private string refillRoom="";
 public bool Running {get{lock(sync)return active!=null;}}
 public Decision? PendingDecision {get{lock(sync)return active?.Command>0?JsonFiles.Clone(active.Action):null;}}
 public string Message {get;private set;}="请连接游戏后进入使徒运气防守大厅";
 public string Focus=>planner.Focus;
 public event Action<string>? Diagnostic;
 public Controller(IClientPort port){this.port=port;}
 public void Start(Snapshot s,Settings p,long now){lock(sync)StartLocked(s,p,now);}
 private void StartLocked(Snapshot s,Settings p,long now)
 {
  var game=port.Find();if(game==null||!Guards.Fresh(s,game.Id,game.Start,now)||s.Account.Length!=64)throw new InvalidOperationException("请先连接游戏并等待账号和盘面同步");
  if(p.Validate()!="")throw new ArgumentException(p.Validate());
  planner.ResetTactics();settings=JsonFiles.Clone(p);active=new(){Enabled=true,Owner=Guid.NewGuid().ToString("N"),Account=s.Account,ProcessId=game.Id,ProcessStart=game.Start,Expires=now+TimeSpan.FromSeconds(10).Ticks};command=0;nextAction=0;refillPending=false;refillRoom="";port.Write(active);Message="已开启，准备读取游戏状态";
 }
 public void Update(Settings p){lock(sync){if(p.Validate()!="")throw new ArgumentException(p.Validate());settings=JsonFiles.Clone(p);}}
 public void Stop(string reason="已暂停；游戏仍会继续运行") {lock(sync){active=null;refillPending=false;port.Write(new Control());Message=reason;Diagnostic?.Invoke(reason);}}
 public void Poll(Snapshot? s,long now){lock(sync)PollLocked(s,now);}
 private void PollLocked(Snapshot? s,long now)
 {
  if(active==null)return;var game=port.Find();
  if(game==null||game.Id!=active.ProcessId||game.Start!=active.ProcessStart){Stop("游戏进程已退出或更换，请重新连接");return;}
  // Keep the GUI enabled through transient loading / file contention. Do not renew a stale action.
  if(s==null||!Guards.Fresh(s,game.Id,game.Start,now)){Message="等待组件心跳；暂停发出操作，恢复后继续";return;}
  if(s.Account.Length==64&&s.Account!=active.Account){Stop("账号已切换，请核对目标后重新开启");return;}
  if(s.Account.Length==0){Message="等待账号加载";return;}
  active.Expires=now+TimeSpan.FromSeconds(10).Ticks;
  if(s.Owner==active.Owner&&s.State=="error"){Stop("组件已暂停："+s.Message);return;}
  if(active.Command>0)
  {
   if(s.Owner!=active.Owner||s.Ack<active.Command){port.Write(active);Message="等待操作确认："+active.Action.Reason;return;}
   Diagnostic?.Invoke($"#{active.Command} {s.AckResult} {s.AckMessage}");
   if(s.AckResult is "error" or "timeout"){Stop(s.AckMessage);return;}
   if(active.Action.Kind=="sell"&&s.AckResult=="ok"){refillPending=true;refillRoom=active.Room;}
   if(active.Action.Kind=="summon"&&s.AckResult=="ok")refillPending=false;
   if(s.AckResult=="ok")planner.Confirm(active.Action,s,now);
   active.Command=0;active.Action=new();nextAction=now+TimeSpan.FromMilliseconds(settings.IntervalMs).Ticks;
  }
  if(now<nextAction){port.Write(active);return;}
  var action=Plan(s,now);Message=action.Reason;
  if(action.Kind=="complete"){Stop(action.Reason);return;}
  if(action.Kind!="wait")
  {
   active.Command=++command;active.Action=action;active.Room=s.Room;active.BoardKey=s.BoardKey;active.SnapshotAt=s.At;
   Diagnostic?.Invoke($"#{command} {action.Kind} W{s.Wave} gold={s.Gold} enemy={s.EnemyCount}: {action.Reason}");
  }
  port.Write(active);
 }
 private Decision Plan(Snapshot s,long now)
 {
  if(refillPending&&((s.Room.Length>0&&s.Room!=refillRoom)||GameFlow.RoundFinished(s)||s.Exiting||s.Stage=="lobby"))refillPending=false;
  if(!refillPending)return planner.Decide(s,settings,now);
  if(s.Stage=="playing"&&s.Ready&&s.MyView&&s.Blocker.Length==0)
  {
   if(s.Units.Length>=s.Boards.Length){refillPending=false;return planner.Decide(s,settings,now);}
   if(s.CanSummon&&s.Gold>=s.Catalog.SummonCost)return new(){Kind="summon",Reason="出售已确认，优先补抽填回空位"};
  }
  return new(){Reason=s.Blocker.Length>0?"等待关闭弹窗："+s.Blocker:"出售已确认，等待金币和召唤按钮就绪后补位"};
 }
}
