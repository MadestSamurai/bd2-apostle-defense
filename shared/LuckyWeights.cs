using System;
using System.Linq;
namespace BD2ApostleDefense
{
 public static class LuckyWeights
 {
  // Work on a detached copy. Low grades share the deduction proportionally;
  // largest remainders preserve an exact integer total without touching game tables.
  public static int[] Create(UnitDef[] rows)
  {
   if(rows==null||rows.Length==0||rows.Any(r=>r==null||r.Weight<0||r.Grade<1||r.Grade>8)||rows.Select(r=>r.Id).Distinct().Count()!=rows.Length)
    throw new InvalidOperationException("Lucky mode: unsupported summon table");
   long total=rows.Sum(r=>(long)r.Weight),low=rows.Where(r=>r.Grade<=2).Sum(r=>(long)r.Weight);
   long extra=rows.Where(r=>r.Grade>=3).Sum(r=>(long)r.Weight*(r.Grade>=7?2:1));
   if(total<=0||total>int.MaxValue||extra>low)throw new InvalidOperationException("Lucky mode: insufficient weight budget");
   var result=new int[rows.Length];var remainders=new long[rows.Length];long used=0,remaining=low-extra;
   for(int i=0;i<rows.Length;i++)
   {
    var r=rows[i];
    if(r.Grade>2)result[i]=checked(r.Weight*(r.Grade>=7?3:2));
    else if(low>0){long numerator=(long)r.Weight*remaining;result[i]=(int)(numerator/low);remainders[i]=numerator%low;used+=result[i];}
   }
   foreach(int i in Enumerable.Range(0,rows.Length).Where(i=>rows[i].Grade<=2&&rows[i].Weight>0).OrderByDescending(i=>remainders[i]).ThenBy(i=>rows[i].Id).Take((int)(remaining-used)))result[i]++;
   if(result.Sum(x=>(long)x)!=total)throw new InvalidOperationException("Lucky mode: invalid weight total");
   return result;
  }
 }
}
