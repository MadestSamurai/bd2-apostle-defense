namespace BD2ApostleDefense
{
 public static class GameFlow
 {
  public static bool RoundFinished(Snapshot s){return s.Win||s.Dead||s.RoomEnded;}
  public static bool BoardAction(string kind){return kind=="summon"||kind=="sell"||kind=="move"||kind=="upgrade";}
  public static bool CanSettle(Snapshot s){return RoundFinished(s)&&!s.Exiting;}
  // Native death clears my units and changes the watched player. Their old receipts can no longer arrive.
  public static bool Superseded(string kind,Snapshot before,Snapshot after)
  {
   return BoardAction(kind)&&(RoundFinished(after)||after.Exiting||after.Stage=="lobby"||
    (after.Room.Length>0&&after.Room!=before.Room));
  }
  public static bool Receipt(string kind,Snapshot before,Snapshot after)
  {
   if(before.Account!=after.Account||after.Blocker.Length>0)return false;
   bool lobby=after.Stage=="lobby"&&!after.Exiting;
   bool sameRoom=before.Room==after.Room;
   switch(kind)
   {
    case "start":return after.Stage=="matching"||after.Stage=="match-failed"||after.Stage=="waiting-round"||after.Stage=="playing";
    case "retry":return lobby||after.Stage=="matching";
    case "match-start":return after.Stage=="playing"||after.Stage=="waiting-round"||(sameRoom&&RoundFinished(after));
    case "open-result":return sameRoom&&CanSettle(after)&&(after.Stage=="result"||after.Stage=="confirm-exit");
    case "settle":return lobby||(sameRoom&&CanSettle(after)&&after.Stage=="confirm-exit");
    case "confirm-exit":return lobby;
    default:return false;
   }
  }
 }
}
