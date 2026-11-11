using System;
using Diavolo;
public static class GamepadChordTests
{
 public static void Main(){
  ushort[] masks={1,8,4,2,136,132,129,130,192};int checks=0;
  foreach(ushort input in masks){int triggered=0;foreach(ushort move in masks)if(GamepadChordRules.Pressed(input,0,move))triggered++;if(triggered!=1)throw new Exception("Conflicting moves for chord "+input);checks++;}
  foreach(ushort input in masks){if(GamepadChordRules.Pressed(input,input,input))throw new Exception("Held chord repeated");checks++;}
  if(GamepadChordRules.Pressed(1,129,1))throw new Exception("Releasing modifier retriggered Epitaph");checks++;
  if(GamepadChordRules.Held(0,0))throw new Exception("Disabled mapping active");checks++;
  if(GamepadChordRules.Pressed(193,129,129))throw new Exception("Ultimate should suppress competing chord");checks++;
  ushort[] bindings={1,1152,2176,640,132,384,130,192,136,4224};
  foreach(ushort input in bindings){int triggered=0;foreach(ushort move in bindings)if(GamepadChordRules.Pressed(input,0,move,bindings,192))triggered++;if(triggered!=1)throw new Exception("New binding conflict "+input);checks++;}
  if(GamepadChordRules.Held(136,8,bindings,192)||!GamepadChordRules.Held(136,136,bindings,192))throw new Exception("Block chord also fired Blood Throw");checks++;
  if(GamepadChordRules.Held(132,4,bindings,192))throw new Exception("Cancel chord kept Barrage held");checks++;
  if(GamepadChordRules.Pressed(8,136,8,bindings,192))throw new Exception("Releasing R3 fired Blood Throw");checks++;
  if(GamepadChordRules.Pressed(193,129,129,bindings,192)||!GamepadChordRules.Pressed(193,129,192,bindings,192))throw new Exception("Ultimate priority lost");checks++;
  ushort[] custom={1,65,4,68,512,640};
  if(GamepadChordRules.Held(65,1,custom)||!GamepadChordRules.Pressed(65,64,65,custom))throw new Exception("L3 remapping conflict");checks++;
  if(GamepadChordRules.Held(640,512,custom)||!GamepadChordRules.Held(640,640,custom))throw new Exception("RB remapping conflict");checks++;
  if(GamepadChordRules.Held(1152,1024,bindings,192)||GamepadChordRules.Held(2176,2048,bindings,192))throw new Exception("R3 trigger also fired plain trigger mapping");checks++;
  if(GamepadChordRules.Pressed(2048,2176,2048,bindings,192))throw new Exception("Releasing R3 retriggered RT");checks++;
  if(!GamepadChordRules.Pressed(2176,128,2176,bindings,192)||!GamepadChordRules.Held(2176,2176,bindings,192))throw new Exception("R3 RT cannot start/hold barrage");checks++;
  if(GamepadChordRules.Held(129,1,bindings,192))throw new Exception("Unmodified extra move active during R3");checks++;
  Console.WriteLine("PASS: "+checks+" controller chord checks");
 }
}
