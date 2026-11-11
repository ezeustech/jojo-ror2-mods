using EyesOfHeavenDio;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using System.Reflection;
void Assert(bool condition,string text){if(!condition)throw new Exception(text);}
void Call(object o,string method)=>o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(o,null);
DioNetworkPresentation Make(bool local){var o=new DioNetworkPresentation();o.gameObject.components[typeof(CharacterBody)]=new CharacterBody{local=local};Call(o,"Awake");return o;}
byte[] Packet(DioNetworkPresentation server){var w=new NetworkWriter();server.OnSerialize(w,false);return w.stream.ToArray();}
void Reset(){Time.time=100;DioFeedback.transitions.Clear();DioFeedback.sounds=0;Run.instance=new Run();}
Reset();NetworkServer.active=true;var host=Make(false);host.ServerTimeStop(true,8);Assert(host.dirty!=0,"Host update must mark dirty");var start=Packet(host);
NetworkServer.active=false;var client=Make(true);client.OnDeserialize(new NetworkReader(start),false);Assert(DioFeedback.transitions.SequenceEqual(new[]{false}),"Remote owner must get opening");
Time.time=106;Call(client,"Update");Call(client,"Update");Assert(DioFeedback.sounds==0,"Time-stop presentation must not duplicate server-relayed resume audio");
NetworkServer.active=true;host.ServerTimeStop(false);var end=Packet(host);NetworkServer.active=false;client.OnDeserialize(new NetworkReader(end),false);Assert(DioFeedback.transitions.SequenceEqual(new[]{false,true}),"Host end must clear remote filter");
Reset();client=Make(true);client.OnDeserialize(new NetworkReader(start),false);Time.time=109;Call(client,"Update");Assert(DioFeedback.transitions.Last(),"Lost ending packet must expire");Call(client,"Update");Assert(DioFeedback.transitions.Count==2,"Cleanup must be idempotent");
Reset();client=Make(false);client.OnDeserialize(new NetworkReader(start),false);Call(client,"Update");Assert(DioFeedback.transitions.Count==0,"Other players' cameras must remain unaffected");
Reset();client=Make(true);client.OnDeserialize(new NetworkReader(start),false);client.GetComponent<CharacterBody>().healthComponent.alive=false;Call(client,"Update");Assert(DioFeedback.transitions.Last(),"Death must clean up");
Reset();client=Make(true);client.OnDeserialize(new NetworkReader(start),false);Call(client,"OnDisable");Assert(DioFeedback.transitions.Last(),"Despawn must clean up");
Reset();client=Make(true);client.OnDeserialize(new NetworkReader(end),true);Assert(DioFeedback.transitions.Count==0,"Late join with no stop must not flash");
Reset();NetworkServer.active=true;host.ServerTimeStop(true,999);start=Packet(host);NetworkServer.active=false;client=Make(true);client.OnDeserialize(new NetworkReader(start),false);Assert(DioFeedback.duration==15,"Stop safety cap");
client.ServerTimeStop(true,10);Assert(client.dirty==0,"Client must not author stop state");
ushort[] masks={4,8,2,192,1,130};int checks=0;
foreach(ushort chord in masks){Assert(masks.Count(m=>GamepadChordRules.Pressed(chord,0,m))==1,"One action per chord");Assert(!GamepadChordRules.Pressed(chord,chord,chord),"No repeat while held");checks+=2;}
Assert(!GamepadChordRules.Pressed(2,130,2),"Releasing R3 must not jump");
Assert(!GamepadChordRules.Pressed(194,130,130),"Ultimate must suppress guard chord");
Assert(!GamepadChordRules.Held(0,0),"Disabled mapping must not activate");
Console.WriteLine("PASS: host/client stop serialization, no duplicate resume audio, missing end, death/despawn, camera isolation, late join, duration cap, server ownership; "+(checks+3)+" controller chord cases.");
