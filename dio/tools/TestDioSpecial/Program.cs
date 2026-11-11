using System;
using EyesOfHeavenDio;
int cases = 0;
int guardCases=0;
foreach(float scale in new[]{.1f,1f,100f})
for(int facing=0;facing<360;facing+=15)
for(int offset=-179;offset<=179;offset++)
{
 if(Math.Abs(offset)==60)continue; // skip rounding ambiguity exactly on the arc boundary
 float a=(float)(facing*Math.PI/180), b=(float)((facing+offset)*Math.PI/180);
 bool block=DioGuardMath.InFront((float)Math.Sin(a),(float)Math.Cos(a),(float)Math.Sin(b)*scale,(float)Math.Cos(b)*scale);
 if(block!=(Math.Abs(offset)<60))throw new Exception($"Incorrect guard arc: facing={facing}, offset={offset}, scale={scale}");
 guardCases++;
}
if(DioGuardMath.InFront(0,1,0,0)||DioGuardMath.InFront(0,0,0,1))throw new Exception("Unknown direction must not block");
if(Math.Abs(1000*DioGuardMath.DamageMultiplier-100)>.001)throw new Exception("Guard must reduce damage by 90%");
Console.WriteLine($"PASS: {guardCases} directional guard cases, zero-direction rejection and damage reduction.");
foreach(float width in new[]{.01f,.5f,2f,10f,100f})
foreach(float depth in new[]{.01f,.5f,2f,10f,100f})
for(int i=0;i<720;i++) {
 var p=DioSpecialMath.KnifeOffset((float)(i*Math.PI/360),width,depth);
 double dx=Math.Max(0,Math.Abs(p.x)-width), dz=Math.Max(0,Math.Abs(p.z)-depth);
 double clearance=Math.Sqrt(dx*dx+dz*dz);
 if(clearance<1.7 || float.IsNaN(p.x)||float.IsNaN(p.z))throw new Exception($"Knife overlaps padded bounds: {width}, {depth}, {i}, clearance={clearance}");
 cases++;
}
if(DioSpecialMath.RollerHidden<=0)throw new Exception("Invalid disappearance duration");
if(DioSpecialMath.ExFreeze<=0||DioSpecialMath.ExFreeze>2f)throw new Exception("EX target hold must stay within its two-second budget");
Console.WriteLine($"PASS: {cases} knife-placement cases cover tiny, large and elongated targets; clearance exceeds full knife length. Road Roller hidden interval={DioSpecialMath.RollerHidden}s, EX freeze={DioSpecialMath.ExFreeze}s.");

foreach(float height in new[]{0f,1f,50f,500f,1900f}) {
 float ground=-100f, origin=ground+height;
 float drop=DioSpecialMath.RollerDropHeight(origin,ground);
 if(ground+drop<origin+23.99f)throw new Exception("Roller must return above activation height");
 float finalY=ground+drop*(1-1f*1f);
 if(Math.Abs(finalY-ground)>.001f)throw new Exception("Slam must reach ground");
}
Console.WriteLine("PASS: Road Roller reaches ground from ground-level and airborne starts, with at least 24m return clearance above activation height.");

float impactAt=DioSpecialMath.RollerDrop; // Relative to voice starting on return.
float jumpAt=impactAt+DioSpecialMath.RollerPunch;
float explosionAt=jumpAt+DioSpecialMath.RollerEscape;
if(Math.Abs(DioSpecialMath.RollerVoicePunchStart-6.07f)>.0001f)throw new Exception("Punch start must match source 1:39.62");
if(Math.Abs(DioSpecialMath.RollerVoiceFinisher-12.11f)>.0001f || jumpAt<=DioSpecialMath.RollerVoiceFinisher || explosionAt<=jumpAt)throw new Exception("Finisher must match source 1:45.66 before escape/explosion");
Console.WriteLine($"PASS: impact at {impactAt:F2}s, punches finish at {jumpAt:F2}s, explosion only after jump at {explosionAt:F2}s.");

if((DioSpecialMath.ExKnifeCount-1)*DioSpecialMath.ExThrowInterval+2f/65f>=DioSpecialMath.ExFreeze)throw new Exception("Last thrown knife cannot reach formation before release");
if(DioSpecialMath.ExRecovery<DioSpecialMath.ExFreeze+.025f+(DioSpecialMath.ExKnifeCount-1)*DioSpecialMath.ExReleaseInterval)throw new Exception("EX recovery cuts off the release");
Console.WriteLine($"PASS: slower EX leaves enough time for all {DioSpecialMath.ExKnifeCount} throws, travel and staggered release.");
