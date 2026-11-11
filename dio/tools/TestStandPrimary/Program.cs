using System.Reflection;
using LocalStandCombat;
using RoR2;
using UnityEngine;

int checks=0;
void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
CharacterBody Body(Vector3 position,TeamIndex team)
{
    var obj=new GameObject();obj.transform.position=position;
    var b=obj.AddComponent<CharacterBody>();b.teamComponent.teamIndex=team;
    b.healthComponent=new HealthComponent{body=b};
    b.mainHurtBox=obj.AddComponent<HurtBox>();b.mainHurtBox.healthComponent=b.healthComponent;
    return b;
}
void Invoke(object component,string method)=>component.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(component,null);
void Advance(StandPrimaryController c,float duration){for(float t=0;t<duration;t+=.02f){Time.time+=.02f;c.Tick(.02f);}}
Check(StandPrimaryRules.InRange(625f),"25m boundary should lock");
Check(!StandPrimaryRules.InRange(625.01f),"Outside 25m must not lock");
Check(!StandPrimaryRules.InRange(float.NaN),"Invalid distance must not lock");
Check(StandPrimaryRules.CrossesContact(0,.02f,.01f,.2f,.4f),"A fast punch must not skip its contact window");
Check(!StandPrimaryRules.CrossesContact(.5f,.6f,1f,.2f,.4f),"Recovery must not hit again");
Check(!StandPrimaryRules.CrossesContact(0,.1f,1f,.2f,.4f),"Windup must not damage");
var owner=Body(new Vector3(0,2,0),TeamIndex.Player);
owner.inputBank.skill1.down=true;
var enemy=Body(new Vector3(0,2,25),TeamIndex.Monster);
var ally=Body(new Vector3(0,2,5),TeamIndex.Player);
var neutral=Body(new Vector3(0,2,4),TeamIndex.Neutral);
var behind=Body(new Vector3(0,2,-8),TeamIndex.Monster);
var controller=StandPrimaryController.Of(owner.gameObject);
BullseyeSearch.Candidates.AddRange(new[]{ally.mainHurtBox,neutral.mainHurtBox,behind.mainHurtBox,enemy.mainHurtBox});
var aim=new Ray(owner.corePosition,Vector3.forward);
Check(controller.Acquire(aim)==enemy.mainHurtBox,"Acquire must ignore allies, neutral actors and targets behind the player");
Check(!controller.ValidTarget(ally.mainHurtBox),"Ally is never a damage candidate");
enemy.transform.position=new Vector3(0,2,25.01f);
Check(controller.Acquire(aim)==null,"Camera search cannot extend the 25m owner leash");
enemy.transform.position=new Vector3(0,2,12);
var side=Body(new Vector3(10,2,1),TeamIndex.Monster);BullseyeSearch.Candidates.Add(side.mainHurtBox);
Check(controller.Acquire(aim)==enemy.mainHurtBox,"Targets outside 25 degrees must not replace the aimed target");
Physics.BlockLine=(a,b)=>b.z>10;
Check(controller.Acquire(aim)==null,"A wall must prevent lock-on");Physics.BlockLine=(a,b)=>false;
Check(controller.Begin(enemy.mainHurtBox,Vector3.forward,.1f),"Begin valid lock");
Check(!controller.Arrived,"Stand cannot hit before it travels");
Advance(controller,.1f);
Check(!controller.Arrived,"Short windups must not teleport the stand into range");
Advance(controller,.6f);
Check(controller.Arrived && controller.CanHit(enemy.healthComponent),"Stand should reach the target with bounded travel");
Vector3 before=controller.Position;
owner.transform.position+=new Vector3(2,0,0);owner.characterDirection.forward=new Vector3(-1,0,0);
Advance(controller,.02f);
Check(Vector3.Distance(before,controller.Position)<.01f,"Moving/turning the owner must not drag the attacking stand");
enemy.transform.position+=new Vector3(1,0,0);Advance(controller,.2f);
Check(controller.Position.x>.9f && controller.Arrived,"Stand follows the locked enemy");
Check(controller.TryAttackRay(out var ray) && ray.origin.z>9f,"Damage ray must originate at the stand");
enemy.healthComponent.alive=false;Advance(controller,.02f);
Check(!controller.Active && controller.Returning && !controller.CanHit(enemy.healthComponent),"Dead targets release without further damage");
for(int i=0;i<65;i++){Time.time+=.02f;Invoke(controller,"FixedUpdate");}
Check(!controller.Visible,"Stand completes its return");
enemy.healthComponent.alive=true;enemy.transform.position=new Vector3(0,2,10);
controller.Begin(enemy.mainHurtBox,Vector3.forward,.1f);
Physics.BlockMovement=true;Advance(controller,.2f);
Check(!controller.Arrived && !controller.CanHit(enemy.healthComponent),"Blocked stand cannot hit through the wall");Physics.BlockMovement=false;
controller.Cancel();Check(!controller.Visible && !controller.TargetedAction,"Interrupted attack clears movement and targeting");
controller.Begin(enemy.mainHurtBox,Vector3.forward,.1f);Advance(controller,.1f);
enemy.transform.position=new Vector3(0,2,30);Advance(controller,.02f);
Check(!controller.Active && controller.Returning,"Target leaving the leash releases the stand");
enemy.transform.position=new Vector3(0,2,10);controller.Begin(enemy.mainHurtBox,Vector3.forward,.1f);
Physics.BlockLine=(a,b)=>true;Advance(controller,.02f);
Check(!controller.Active,"New cover releases an existing lock");Physics.BlockLine=(a,b)=>false;
controller.Begin(enemy.mainHurtBox,Vector3.forward,.1f);owner.healthComponent.alive=false;Invoke(controller,"FixedUpdate");
Check(!controller.Visible,"Owner death clears the stand");
var sequence=new Diavolo.ComboSequence();
for(int i=0;i<10;i++){Check(sequence.Take(i*.2f)==i%5,"Five-hit combo order must be preserved");sequence.Finish(i*.2f,true);}
sequence.Finish(5f,false);Check(sequence.Take(5.1f)==0,"Interrupted combo resets");
owner.healthComponent.alive=true;controller.Cancel();enemy.characterMotor=new CharacterMotor();enemy.transform.position=new Vector3(0,.5f,10);
controller.Begin(enemy.mainHurtBox,Vector3.forward,.1f);Advance(controller,.6f);
Check(controller.Position.y>=enemy.footPosition.y+1.39f,"Grounded small enemies must not bury the stand's chest");

// Actual production travel plus the contact clock, at slow and very fast attacks.
owner.transform.position=new Vector3(0,2,0);owner.characterDirection.forward=Vector3.forward;
enemy.characterMotor=null;enemy.transform.position=new Vector3(0,2,25);
foreach(float contact in new[]{.025f,.12f,.4f}) {
    controller.Cancel();controller.Begin(enemy.mainHurtBox,Vector3.forward,contact);
    var clock=new StandAttackClock();bool hitBeforeArrival=false,travelBounded=true;int contacts=0;
    for(int i=0;i<100;i++) {
        var was=controller.Position;Time.time+=.02f;controller.Tick(.02f);
        travelBounded &= Vector3.Distance(was,controller.Position)<=StandPrimaryRules.MaxTravelSpeed*.02f+.0001f;
        clock.Advance(.02f,contact,controller.Active,controller.Arrived);
        if(clock.Previous<contact && clock.Age>=contact){contacts++;hitBeforeArrival |= !controller.Arrived;}
    }
    Check(travelBounded,"Travel speed is bounded even with a 25ms windup");
    Check(!hitBeforeArrival && contacts==1,"Exactly one contact begins after physical arrival");
    Check(clock.Waited>.1f && !clock.TimedOut,"Long-range windup waits and then resumes");
}
var anchor=controller.Position;
controller.Release();Time.time+=.02f;Invoke(controller,"FixedUpdate");
Check(Vector3.Distance(anchor,controller.Position)<.001f,"Combo grace preserves the reached anchor");
Check(controller.Acquire(new Ray(owner.corePosition,Vector3.forward))==enemy.mainHurtBox,"Combo retains a valid aimed enemy");
controller.Begin(enemy.mainHurtBox,Vector3.forward,.05f);
Check(Vector3.Distance(anchor,controller.Position)<.001f,"Next combo hit never resets the stand to its owner");
controller.Release();for(int i=0;i<18;i++){Time.time+=.02f;Invoke(controller,"FixedUpdate");}
Check(controller.Visible,"Long returns stay visible beyond the old 350ms cutoff");
for(int i=0;i<100;i++){Time.time+=.02f;Invoke(controller,"FixedUpdate");}
Check(!controller.Visible,"Long return hides only after arriving home");
var blockedClock=new StandAttackClock();
for(int i=0;i<100;i++)blockedClock.Advance(.02f,.1f,true,false);
Check(blockedClock.TimedOut && blockedClock.Age<.1f,"Blocked travel times out without striking");
blockedClock.Advance(.1f,.1f,false,false);
Check(blockedClock.Age>=.1f,"Lost targets do not leave a stuck windup");
Check(StandPrimaryRules.AnimationSpeed(100f)==2f,"Extreme attack speed retains readable animation");
var root=new GameObject();var bone=new Transform{localPosition=new Vector3(1,0,0)};root.bones=new[]{root.transform,bone};
root.transform.localPosition=new Vector3(20,0,0);
Time.time=10;Time.frameCount=1;
var pose=new EyesOfHeavenDio.DioPoseBlend(root);pose.Begin(.1f);bone.localPosition=new Vector3(3,0,0);pose.Apply();
Check(Math.Abs(bone.localPosition.x-1)<.001f,"DIO blend starts from the displayed pose");
Check(root.transform.localPosition.x==20,"Pose smoothing must not move the gameplay root");
Time.time=10.05f;bone.localPosition=new Vector3(3,0,0);pose.Apply();
Check(Math.Abs(bone.localPosition.x-2)<.002f,"DIO entrance blends between old and new pose");
Time.time=10.11f;bone.localPosition=new Vector3(3,0,0);pose.Apply();Check(bone.localPosition.x==3,"Blend ends without delaying the authored attack");
Time.frameCount=2;pose.Begin(.1f);bone.localPosition=new Vector3(10,0,0);pose.Begin(.1f);pose.Apply();
Check(bone.localPosition.x==3,"Repeated same-frame clip changes must preserve the original pose snapshot");
var crimson=new Diavolo.PoseTransition(root);crimson.BeforeSample("idle",0,0,.1f);
bone.localPosition=new Vector3(1,0,0);crimson.BeforeSample("chain1",1,1,.1f);bone.localPosition=new Vector3(5,0,0);crimson.AfterSample(1.05f);
Check(Math.Abs(bone.localPosition.x-3)<.002f,"Diavolo attack entrance blends its previous pose");
crimson.BeforeSample("chain1",2,2,.1f);bone.localPosition=new Vector3(7,0,0);crimson.AfterSample(2);
Check(Math.Abs(bone.localPosition.x-3)<.002f,"Repeated Diavolo attacks use a new transition even with the same clip");
controller.Cancel();enemy.healthComponent.alive=true;enemy.transform.position=new Vector3(0,2,12);
controller.Begin(enemy.mainHurtBox,Vector3.forward,.1f);Advance(controller,.6f);
Check(controller.Acquire(new Ray(owner.corePosition,new Vector3(0,0,-1)))==enemy.mainHurtBox,"Held M1 retains its target when looking behind");
controller.Release();Invoke(controller,"FixedUpdate");
Check(controller.Acquire(new Ray(owner.corePosition,new Vector3(0,0,-1)))==enemy.mainHurtBox,"Held lock persists across combo states");
owner.inputBank.skill1.down=false;Invoke(controller,"FixedUpdate");
Check(controller.Acquire(new Ray(owner.corePosition,new Vector3(0,0,-1)))==behind.mainHurtBox,"Releasing M1 permits a fresh aimed target");
controller.Cancel();controller.BeginContinuous(new Ray(owner.corePosition,Vector3.forward));
Advance(controller,.7f);
Check(controller.Active && controller.Arrived,"Barrage travels to the aimed opponent");
Check(controller.Acquire(new Ray(owner.corePosition,new Vector3(0,0,-1)))==enemy.mainHurtBox,"Barrage retains lock without using the player's primary input");
enemy.healthComponent.alive=false;
Time.time+=.2f;controller.TickContinuous(new Ray(owner.corePosition,new Vector3(0,0,-1)),.02f);
Check(controller.Active && controller.Target!=enemy.mainHurtBox,"A held barrage reacquires a living enemy after a kill");
var stoppedAt=controller.Position;controller.StopContinuous();
Check(!controller.Active && !controller.Continuous && controller.Returning,"Barrage release stops damage and starts a visible return");
Check(Vector3.Distance(stoppedAt,controller.Position)<.001f,"Barrage release never teleports the stand home");
for(int i=0;i<100;i++){Time.time+=.02f;Invoke(controller,"FixedUpdate");}
Check(!controller.Visible,"Released barrage finishes returning home");
controller.Cancel();enemy.healthComponent.alive=true;
controller.BeginContinuous(new Ray(owner.corePosition,Vector3.forward),false);
controller.TickContinuous(new Ray(owner.corePosition,Vector3.forward),.02f,false);
Check(!controller.Active,"Remote barrage waits for the authoritative target");
controller.FollowContinuous(enemy.mainHurtBox);
Check(controller.Target==enemy.mainHurtBox,"Remote barrage accepts the shared target");
controller.StopContinuous();controller.FollowContinuous(enemy.mainHurtBox);
Check(!controller.Active,"Late target packets cannot restart a released barrage");
Console.WriteLine($"PASS: {checks} stand lock-on, movement, contact, combo and pose-transition checks.");
