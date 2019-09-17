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
        #region mdk preserve
        public const string telemetryScreenName = "Drone Telemetry"; //name of screen(s) to write data to
        public const string nameBase = "FD"; //Naming convention of drones
        public Dictionary<string, string[]> droneTelemetry = new Dictionary<string, string[]>();
        #endregion
        public void Main(string argument, UpdateType updateSource)
        {
            string[] data = argument.Split(',');
            string id = data[0];
            for (int i = 0; i < 10;)
            {
                id = id.Replace(i.ToString(),"");
                i++;
            }
            if (id == nameBase)
            {
                if (droneTelemetry.ContainsKey(data[0]))
                {
                    droneTelemetry[data[0]] = data;
                }
                if (!droneTelemetry.ContainsKey(data[0]))
                {
                    droneTelemetry.Add(data[0], data);
                }
                updateScreen();
                return;
            }
            else { return; }
        }
        public IMyTextPanel addScreen(int ID)
        {
            IMyTextPanel screen = GridTerminalSystem.GetBlockWithName(telemetryScreenName + ID.ToString()) as IMyTextPanel;
            return screen;
        }
        public void updateScreen()
        {
            int screen = 0;
            int prntCt = 0;
            int prntMx = 9; //How many sets of telemetry fit per screen
            List<string> drones = new List<string>(droneTelemetry.Keys);
            for (int c = 0; c < drones.Count; c++)
            {
                if (prntCt < prntMx) { addScreen(screen).WriteText(parseTelemetry(droneTelemetry[drones[c]])); prntCt++; }
                if(prntCt >= prntMx) { prntCt = 0; screen++; }
            }
        }
        public string parseTelemetry(string[] screaming)
        {
            string telemetry = "Drone ";
            var screaminglouder = new List<string[]>();
            if (screaming.Length < 2) { return String.Format("Drone {0}:\n    No telemetry!\n", screaming[0].Remove(0, 2)); }
            //what the fuck am I doing
            foreach (string str in screaming) { screaminglouder.Add(str.Split(':')); }
            telemetry += String.Format("{0}:\n    Altitude: {1}m\n    Velocity: {2}m/s\n    Uranium : {3}kg\n    Fuel    : {4}%\n    Battery: {5}%\n    Status  :{6}\n",screaming[0],screaminglouder[8],screaminglouder[10],screaminglouder[4],screaminglouder[2],screaminglouder[6],screaminglouder[12]);
            /* Theoretical output: [given "FD001,h2:80,U:3,batt:90,alt:20,vel:5,status:Stable" in Main()]
             * Drone 001:
             *     Altitude: 20m
             *     Velocity: 5m/s
             *     Uranium : 3kg
             *     Fuel    : 80%
             *     Battery : 90%
             *     Status  : Stable
             */
            return telemetry;
        }
    }
}
