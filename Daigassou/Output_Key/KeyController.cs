﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Daigassou.Input_Midi;
using Daigassou.Properties;
using Daigassou.Utils;

namespace Daigassou
{
    public class KeyController
    {
        private static Keys _lastCtrlKey;
        public static  volatile bool  isBackGroundKey = false;
        [DllImport("User32.dll")]
        public static extern void keybd_event(Keys bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        private static object keyLock = new object();
        public static void KeyboardPress(int pitch)
        {
            if (pitch<=84 && pitch >=48){

                if (Settings.Default.IsEightKeyLayout)
                    KeyboardPress(KeyBinding.GetNoteToCtrlKey(pitch), KeyBinding.GetNoteToKey(pitch));
                else if(isBackGroundKey)
                    BackgroundKey.BackgroundKeyPress(KeyBinding.GetNoteToKey(pitch));
                else
                    KeyboardPress(KeyBinding.GetNoteToKey(pitch));
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")},{(pitch-24).ToString("X2")} Note On");
                ParameterController.GetInstance().CheckSyncStatus(); 
            }

        }

        public static void KeyboardPress(Keys ctrKeys, Keys viKeys)
        {

            keybd_event(_lastCtrlKey, (byte) MapVirtualKey((uint) _lastCtrlKey, 0), 2, 0);
            Thread.Sleep(1);
            keybd_event(ctrKeys, (byte) MapVirtualKey((uint) ctrKeys, 0), 0, 0);
            Thread.Sleep(15);
            keybd_event(viKeys, (byte) MapVirtualKey((uint) viKeys, 0), 0, 0);
            _lastCtrlKey = ctrKeys;
            Thread.Sleep(15);
        }

        private static void KeyboardPress(Keys viKeys)
        {

            lock (keyLock)
            {
                keybd_event(viKeys, (byte)MapVirtualKey((uint)viKeys, 0), 0, 0);
                Thread.Sleep(1);
            }

        }


        public static void KeyboardRelease(int pitch)
        {
            lock (keyLock)
            {

                if (pitch <= 84 && pitch >= 48)
                {
                    if (Settings.Default.IsEightKeyLayout)
                        KeyboardRelease(KeyBinding.GetNoteToCtrlKey(pitch), KeyBinding.GetNoteToKey(pitch));
                    else if (isBackGroundKey)
                        BackgroundKey.BackgroundKeyRelease(KeyBinding.GetNoteToKey(pitch));
                    else
                        KeyboardRelease(KeyBinding.GetNoteToKey(pitch));
                    System.Diagnostics.Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")},{(pitch-24).ToString("X2")} Note Off");
                }
            }

        }

        public static void KeyboardRelease(Keys ctrKeys, Keys viKeys)
        {
            keybd_event(ctrKeys, (byte) MapVirtualKey((uint) ctrKeys, 0), 2, 0);
            Thread.Sleep(1);
            keybd_event(viKeys, (byte) MapVirtualKey((uint) viKeys, 0), 2, 0);
        }


        public static void KeyboardRelease(Keys viKeys)
        {
            keybd_event(viKeys, (byte) MapVirtualKey((uint) viKeys, 0), 2, 0);
        }

        public static void KeyPlayBack(Queue<KeyPlayList> keyQueue, double speed , CancellationToken token)
        {
            var startTime = Environment.TickCount;
            while (keyQueue.Any() && !token.IsCancellationRequested)
            {
                var nextKey = keyQueue.Dequeue();
               // var duration = tick * nextKey.TimeMs;
                //var targetTime = startTime + duration;
                var targetTime = startTime + nextKey.TimeMs* speed  ;
                while (true)
                    if (targetTime+ ParameterController.GetInstance().Offset <= Environment.TickCount)
                        break;

                //startTime = Environment.TickCount;

                if (nextKey.Ev == KeyPlayList.NoteEvent.NoteOn)
                    KeyboardPress(nextKey.Pitch);
                else
                    KeyboardRelease(nextKey.Pitch);

#if _log
                System.Diagnostics.Debug.WriteLine($@" i called function at {startTime} with target time is {targetTime}");
#endif
            }
        }

    }

    public class KeyPlayList
    {
        public enum NoteEvent
        {
            NoteOff,
            NoteOn
        }

        public NoteEvent Ev;
        public int Pitch;
        public long TimeMs;
        public KeyPlayList(NoteEvent ev, int pitch, long timeMs)
        {
            TimeMs = timeMs;
            Ev = ev;
            Pitch = pitch;
        }
    }
}