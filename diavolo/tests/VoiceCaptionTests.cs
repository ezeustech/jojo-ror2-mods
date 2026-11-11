using System;
using System.IO;
using Diavolo;
public static class VoiceCaptionTests
{
 public static void Main(string[] args){int count=0;foreach(string file in Directory.GetFiles(args[0],"*_voice.wav")){
  string key=Path.GetFileNameWithoutExtension(file);if(string.IsNullOrEmpty(VoiceCaptionText.Get(key)))throw new Exception("Missing voice caption "+key);count++;
 }if(VoiceCaptionText.Get("erase_sfx")!=null)throw new Exception("Sound effect incorrectly subtitled as speech");
 Console.WriteLine("PASS: captions cover all "+count+" installed voice clips; SFX do not invent dialogue.");}
}
