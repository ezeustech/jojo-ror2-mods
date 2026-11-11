using System;
using System.IO;
using System.Reflection;
using System.Threading;
using EyesOfHeavenDio;

// Exercise the actual native playback class against the Windows audio device.
internal static class Program
{
    private static void Main(string[] args)
    {
        using var reader = new BinaryReader(File.OpenRead(args[0]));
        reader.BaseStream.Position = 12;
        byte[] pcm = null; int channels = 0, rate = 0;
        while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
        {
            string tag = new string(reader.ReadChars(4)); int length = reader.ReadInt32();
            long end = reader.BaseStream.Position + length + (length & 1);
            if (tag == "fmt ") { if (reader.ReadInt16() != 1) throw new Exception("Not PCM"); channels = reader.ReadInt16(); rate = reader.ReadInt32(); }
            if (tag == "data") pcm = reader.ReadBytes(length);
            reader.BaseStream.Position = end;
        }
        var playback = new DioAudioPlayback();
        try
        {
            if (!playback.Begin(pcm, channels, rate, .8f)) throw new Exception("Playback rejected");
            Thread.Sleep(350);
            float first = playback.PlaybackSeconds;
            Thread.Sleep(350);
            float second = playback.PlaybackSeconds;
            Console.WriteLine("Native position: " + first + " -> " + second + " seconds");
            if (first <= 0 || second <= first) throw new Exception("Audio clock did not advance");
            Thread.Sleep(500);
            Console.WriteLine("PASS: actual PCM stream accepted; playback clock advances.");
        }
        finally { typeof(DioAudioPlayback).GetMethod("Cleanup", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(playback, null); }
    }
}
namespace EyesOfHeavenDio
{
    internal static class DioPlugin { internal static TestLog Log = new TestLog(); }
    internal sealed class TestLog { public void LogInfo(string text) => Console.WriteLine(text); public void LogError(string text) => Console.WriteLine(text); }
}
namespace UnityEngine
{
    public class MonoBehaviour { public string name = "audio smoke test"; public object gameObject; public static void Destroy(object obj) {} }
    public static class Time { private static readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew(); public static float time => (float)clock.Elapsed.TotalSeconds; public static float timeScale = 1; }
    public static class Mathf { public static int RoundToInt(float value) => (int)Math.Round(value); public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max); }
}
