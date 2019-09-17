using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public int lastAltAct = 0;
        public TimeSpan LastElapsed;
        public int minAlt = 20;
        public int altMargin = 5;
        public int thrustduration = 3000;
        public float thrustoverride = 0.40f;

        public void correctAlt()
        {
            Me.CustomData = "PROCESSING";
            IMyRemoteControl RC = getRC();
            double altitude;
            RC.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
            var lifts = new List<IMyThrust>();
            var drops = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(lifts, lift => lift.Orientation.Forward.ToString() == "Down");
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(drops, drop => drop.Orientation.Forward.ToString() == "Up");
            if (Runtime.TimeSinceLastRun < LastElapsed.Add(TimeSpan.FromMilliseconds(thrustduration))) { lastAltAct = 0; Me.CustomData = "READY"; return; }//Only take action after thrustduration delay
            switch (lastAltAct)
            {
                case 0:
                    if (altitude <= (minAlt - altMargin)) { Echo("Alt low."); engageThrust(lifts); break; }
                    if (altitude >= (minAlt + altMargin)) { Echo("Alt high."); engageThrust(drops); break; }
                    if (altitude > (minAlt - altMargin) && altitude < (minAlt + altMargin)) { lastAltAct = 0; LastElapsed = Runtime.TimeSinceLastRun; Echo("Alt OK."); break; }
                    break;

                case 1:
                    resetThrust();
                    break;

                case 2:
                    resetThrust();
                    break;
                default:
                    lastAltAct = 0;
                    break;
            }
            Me.CustomData = "READY";
        }

        public void engageThrust(List<IMyThrust> targets)
        {
            IMyRemoteControl RC = getRC();
            RC.DampenersOverride = false;
            foreach (IMyThrust thruster in targets) { thruster.ThrustOverridePercentage = thrustoverride; LastElapsed = Runtime.TimeSinceLastRun; lastAltAct = 0; }
            return;
        }
        public void resetThrust()
        {
            IMyRemoteControl RC = getRC();
            RC.DampenersOverride = true;
            var targets = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(targets);
            foreach (IMyThrust lthruster in targets) { lthruster.ThrustOverridePercentage = 0; }
            lastAltAct = 0;
            LastElapsed = Runtime.TimeSinceLastRun;
            return;
        }
        public IMyRemoteControl getRC()
        {
            IMyRemoteControl RC;
            var tRC = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(tRC);
            RC = tRC[0];
            return RC;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            char[] split = ",".ToCharArray();
            var args = new List<string>(argument.Split(split));
            if(args[0] == "INIT")
            {
                minAlt = Convert.ToInt32(args[1]);
                altMargin = Convert.ToInt32(args[2]);
                thrustduration = Convert.ToInt32(args[3]);
                thrustoverride = float.Parse(args[4]);
                Me.CustomData = "READY";
                return;
            }
            if(args[0] == "RUN")
            {
                correctAlt();
            }
        }
    }
}
