using System;
using System.IO;
using System.Threading;
using UnityEngine;
public static class ValidateAudio25 {
 public static void Run(){
  var a=new GameObject("Muted whole-clip check").AddComponent<Diavolo.DiavoloAudioPlayback>();
  var b=new GameObject("Muted overlapping check").AddComponent<Diavolo.DiavoloAudioPlayback>();
  try {
   var begin=typeof(Diavolo.DiavoloAudioPlayback).GetMethod("Begin",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
   if(!(bool)begin.Invoke(a,new object[]{new byte[48000*2*2*3],2,48000,1f})||!(bool)begin.Invoke(b,new object[]{new byte[22050*2*3],1,22050,1f}))throw new Exception("Native audio open/write failed");
   Thread.Sleep(300);float first=a.PlaybackSeconds;
   Thread.Sleep(900);float next=a.PlaybackSeconds;
   if(next-first<.65f || b.PlaybackSeconds<.8f)throw new Exception("Playback stopped when main thread was stalled");
   File.WriteAllText("../docs/audio-validation-2.5.txt","PASS: muted stereo 48 kHz and mono 22.05 kHz whole-clip streams overlap and advance through a 900 ms main-thread stall. Audible quality and gameplay still need user testing.\n");
  }finally{UnityEngine.Object.DestroyImmediate(a.gameObject);UnityEngine.Object.DestroyImmediate(b.gameObject);}
  VerifyReleaseBundle.Run();
 }
}
