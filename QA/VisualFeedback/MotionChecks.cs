using System;
using System.Collections.Generic;
using GrowingCity;
static class MotionChecks
{
 public static int Run(){int passed=0;Action<bool,string> a=(v,m)=>{if(!v)throw new Exception(m);passed++;Console.WriteLine("PASS: "+m);};
 var modes=new HashSet<int>();for(int i=-100;i<=100;i++)modes.Add(DestructionMotion.Mode(i));a(modes.Count==3,"three collapse variants across positive and negative seeds");
 for(int mode=0;mode<3;mode++){var start=DestructionMotion.Pose(mode,0,50,16);a(start.Angle==0&&start.Travel==0&&start.Lift==0&&start.Drop==0,"collapse begins at original pose "+mode);bool finite=true;for(int i=-10;i<2000;i++){var p=DestructionMotion.Pose(mode,i*.002,50,16);finite&=!double.IsNaN(p.Angle)&&!double.IsInfinity(p.Travel)&&p.Angle>=0&&p.Travel>=0&&p.Drop>=0;}a(finite,"collapse samples finite and bounded "+mode);a(DestructionMotion.BreakFraction(mode)>0&&DestructionMotion.BreakFraction(mode)<1,"fracture occurs before cleanup "+mode);}
 var hinge=DestructionMotion.Pose(0,DestructionMotion.Duration(0)*DestructionMotion.BreakFraction(0),50,16);a(hinge.Angle>65,"sideways collapse visibly tips before fragmentation");a(DestructionMotion.Pose(1,.5,50,16).Yaw>0,"shear collapse twists sideways");a(DestructionMotion.Pose(2,.2,50,16).Lift>0,"blast collapse lifts before breaking");
 var velocities=new HashSet<string>();double maxIdealApex=0,maxDistance=0;for(int i=0;i<6;i++){double h=16*DestructionMotion.HorizontalSpeed(i,7),v=16*DestructionMotion.UpSpeed(i,7);velocities.Add(h+"/"+v);maxIdealApex=Math.Max(maxIdealApex,v*v/(2*9.81));maxDistance=Math.Max(maxDistance,h*4.2);a(h>15&&v>20,"structural fragment has strong nonzero launch "+i);}a(velocities.Count==6,"six structural fragment velocity pairs differ");a(maxIdealApex>70&&maxDistance>150,"launch parameters allow high and distant ideal ballistic paths");
 return passed;}
}
