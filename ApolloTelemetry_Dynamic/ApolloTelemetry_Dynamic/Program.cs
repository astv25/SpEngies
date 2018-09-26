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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region mdk preserve
        public string[] allowedAPs = new string[] { "AP11", "AP12", "AP13", "AP14", "AP15", "AP16", "AP17", "AP18", "AP19" };//currently active Apollo units
        public Dictionary<string, string[]> apTelemetry = new Dictionary<string, string[]>();
        public const string telemetryScreenName = "Apollo Telemetry ";
        #endregion
        public Program()
        {
            //Initialize apTelemetry
            for (int AP = 0; AP < allowedAPs.Length; AP++)
            {
                apTelemetry.Add(allowedAPs[AP], new string[] {allowedAPs[AP]});
            }
            //Wall of the fallen
            apTelemetry["AP16"] = new string[] { "AP16", "SD", "0" };
            apTelemetry["AP18"] = new string[] { "AP18", "SD", "0" };
        }
        
        public void Main(string argument, UpdateType updateSource)
        {
            string[] data = argument.Split('|');
            if (apTelemetry.ContainsKey(data[0])) //If received message contains an allowed AP in the correct format
            {
                apTelemetry[data[0]] = data; //Set message as that key's value                
            }
            else { return; } //Not Apollo telmetry, don't care
            updateScreen();
        }
        public string parseTelemetry(string[] telBrick)
        {
            string parsed = "Apollo ";
            //DEBUG: Echo(string.Join("|",telBrick));
            if (telBrick.Length < 2)
            {
                parsed += telBrick[0].Remove(0, 2) + ":\n    No telemetry!\n";
                return parsed;
            }
            if (telBrick[1] == "SD") //If Apollo unit reports Self Destruct in progress (e.g. AP8|SD|5 => Apollo 8, self destruct in progress, 5 seconds)
            {
                parsed += telBrick[0].Remove(0, 2) + ":\n    Self destruct initiated.";
                parsed += "\n    " + telBrick[2] + " seconds remain.\n";
                return parsed;
            }
            parsed += telBrick[0].Remove(0, 2) + ":";
            parsed += "\n    Altitude:  " + telBrick[1];
            if (telBrick[1] != "Space") { parsed += "m"; }
            parsed += "\n    Velocity:  " + telBrick[2];
            if (telBrick[2] != "Stable") { parsed += "m/s"; }
            parsed += "\n    Uranium:  " + telBrick[3] + "kg";
            parsed += "\n    Fuel:  " + telBrick[4];
            if (telBrick[4] != "WRN") { parsed += "%"; }
            parsed += "\n    Battery:  " + telBrick[5];
            if (telBrick[5] != "WRN") { parsed += "%"; }
            parsed += "\n    Distance to Relay:  " + telBrick[6] + "m\n";
            return parsed;
        }
        public void updateScreen()
        {
            int screen = 0;
            int prntCt = 0;
            foreach( KeyValuePair<string, string[]> kvp in apTelemetry)
            {
                if (prntCt > 0 && prntCt < 3)
                {
                    addScreen(screen).WritePublicText(parseTelemetry(kvp.Value), true);
                    addScreen(screen).ShowPublicTextOnScreen();
                    prntCt++;
                    continue;
                }
                if (prntCt >= 3)
                {
                    prntCt = 0;
                    screen++;
                    addScreen(screen).WritePublicText(parseTelemetry(kvp.Value));
                    addScreen(screen).ShowPublicTextOnScreen();
                    prntCt++;                    
                }                
                if (prntCt == 0)
                {
                    addScreen(screen).WritePublicText(parseTelemetry(kvp.Value));
                    addScreen(screen).ShowPublicTextOnScreen();
                    prntCt++;
                }                
            }
        }
        public IMyTextPanel addScreen(int ID)
        {
            IMyTextPanel screen = GridTerminalSystem.GetBlockWithName(telemetryScreenName + ID.ToString()) as IMyTextPanel;
            return screen;
        }
    }
}