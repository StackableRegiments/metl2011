﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SandRibbon.Providers;
using SandRibbon.Utils;

namespace SandRibbon.Components.Utility
{
    public static class GlobalTimers
    {
        public static Timer SyncTimer = null;
        private static List<Action> timedActions = new List<Action>();
        public static void SetSyncTimer(Action timedAction)
        {
            if (Globals.MeTLType == "MeTL Demonstrator") return;
            timedActions.Add(timedAction);
            if(SyncTimer == null)
                SyncTimer = new Timer(delegate
                                          {
                                              timedActions.Last()();
                                              timedActions = new List<Action>();
                                              SyncTimer = null;
                                          },null, 500, Timeout.Infinite );
        }
        public static void resetSyncTimer()
        {
            if(SyncTimer != null)
            {
                SyncTimer.Change(500, Timeout.Infinite);
            }
        }
    }
}
