using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Diavolo
{
    // RoR2's Wwise build does not provide usable Unity AudioClip buffers.
    // Use independent waveOut streams: overlapping impacts do not cut off callouts.
    public sealed class DiavoloAudioPlayback : MonoBehaviour
    {
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct Format
        {
            public ushort tag, channels;
            public uint rate, bytesPerSecond;
            public ushort alignment, bits, extra;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct Header
        {
            public IntPtr data;
            public uint length, recorded;
            public UIntPtr user;
            public uint flags, loops;
            public IntPtr next;
            public UIntPtr reserved;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct Position { public uint type, value, reserved; }
        [DllImport("winmm.dll")] private static extern uint waveOutOpen(out IntPtr device, uint id, ref Format format, IntPtr callback, IntPtr instance, uint flags);
        [DllImport("winmm.dll")] private static extern uint waveOutPrepareHeader(IntPtr device, IntPtr header, uint size);
        [DllImport("winmm.dll")] private static extern uint waveOutWrite(IntPtr device, IntPtr header, uint size);
        [DllImport("winmm.dll")] private static extern uint waveOutGetPosition(IntPtr device, ref Position position, uint size);
        [DllImport("winmm.dll")] private static extern uint waveOutPause(IntPtr device);
        [DllImport("winmm.dll")] private static extern uint waveOutRestart(IntPtr device);
        [DllImport("winmm.dll")] private static extern uint waveOutReset(IntPtr device);
        [DllImport("winmm.dll")] private static extern uint waveOutUnprepareHeader(IntPtr device, IntPtr header, uint size);
        [DllImport("winmm.dll")] private static extern uint waveOutClose(IntPtr device);
        private IntPtr device, header, data;
        private uint rate, bytesPerSecond;
        private bool prepared, paused;
        private float started, length;
        private static readonly uint HeaderSize = (uint)Marshal.SizeOf<Header>();
        public float PlaybackSeconds
        {
            get
            {
                var position = new Position { type = 2 }; // TIME_SAMPLES; driver may return another unit.
                if (device != IntPtr.Zero && waveOutGetPosition(device, ref position, (uint)Marshal.SizeOf<Position>()) == 0)
                {
                    if (position.type == 2) return (float)position.value / rate;
                    if (position.type == 4) return (float)position.value / bytesPerSecond;
                    if (position.type == 1) return position.value / 1000f;
                }
                return Time.time - started;
            }
        }
        internal bool Begin(byte[] pcm, int channels, int sampleRate, float volume)
        {
            try
            {
                rate = (uint)sampleRate; bytesPerSecond = rate * (uint)channels * 2;
                length = (float)pcm.Length / bytesPerSecond; started = Time.time;
                var format = new Format { tag = 1, channels = (ushort)channels, rate = rate,
                    bytesPerSecond = bytesPerSecond, alignment = (ushort)(channels * 2), bits = 16 };
                Check(waveOutOpen(out device, uint.MaxValue, ref format, IntPtr.Zero, IntPtr.Zero, 0), "open");
                data = Marshal.AllocHGlobal(pcm.Length); Marshal.Copy(pcm, 0, data, pcm.Length);
                header = Marshal.AllocHGlobal((int)HeaderSize);
                Marshal.StructureToPtr(new Header { data = data, length = (uint)pcm.Length }, header, false);
                Check(waveOutPrepareHeader(device, header, HeaderSize), "prepare"); prepared = true;
                Check(waveOutWrite(device, header, HeaderSize), "write");

                return true;
            }
            catch (Exception error) { Debug.LogError("Diavolo audio playback failed: " + error.Message); Cleanup(); return false; }
        }
        private static void Check(uint result, string operation)
        {
            if (result != 0) throw new InvalidOperationException("waveOut " + operation + " error " + result);
        }
        private void Update()
        {
            if (device == IntPtr.Zero) { Destroy(gameObject); return; }
            bool shouldPause = Time.timeScale == 0;
            if (shouldPause != paused)
            {
                if (shouldPause) waveOutPause(device); else waveOutRestart(device);
                paused = shouldPause;
            }
            if ((Marshal.PtrToStructure<Header>(header).flags & 1) != 0 || Time.time - started > length + 5)
                Destroy(gameObject);
        }
        private void Cleanup()
        {
            if (device != IntPtr.Zero)
            {
                waveOutReset(device);
                if (prepared) waveOutUnprepareHeader(device, header, HeaderSize);
                waveOutClose(device); device = IntPtr.Zero; prepared = false;
            }
            if (header != IntPtr.Zero) { Marshal.FreeHGlobal(header); header = IntPtr.Zero; }
            if (data != IntPtr.Zero) { Marshal.FreeHGlobal(data); data = IntPtr.Zero; }
        }
        private void OnDestroy() { Cleanup(); }
        private void OnApplicationQuit() { Cleanup(); }
    }
}
