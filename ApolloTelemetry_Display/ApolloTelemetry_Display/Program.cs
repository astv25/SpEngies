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
        public Program()
        {
        }
        
        public string[] allowedAPs = new string[] { "AP11", "AP12", "AP13", "AP14", "AP15", "AP16", "AP17" };//currently active Apollo missions        
        public string[] AP11Tel = new string[] { "AP11" };
        public string[] AP12Tel = new string[] { "AP12" };
        public string[] AP13Tel = new string[] { "AP13" };
        public string[] AP14Tel = new string[] { "AP14" };
        public string[] AP15Tel = new string[] { "AP15" };
        public string[] AP16Tel = new string[] { "AP16" };
        public string[] AP17Tel = new string[] { "AP17" };
        public bool dataChanged = false;
        public const string telemetryScreenName = "Apollo Telemetry";
        IMyTextPanel telemetryScreen;
        IMyTextPanel telemetryScreen2;

        public void Main(string argument, UpdateType updateSource)
        {
            string parseMe = argument;            
            string[] data = parseMe.Split('|');            
            if (allowedAPs.Contains(data[0])) {                
                if (data[0] == "AP11") { AP11Tel = data; }
                if (data[0] == "AP12") { AP12Tel = data; }
                if (data[0] == "AP13") { AP13Tel = data; }
                if (data[0] == "AP14") { AP14Tel = data; }
                if (data[0] == "AP15") { AP15Tel = data; }
                if (data[0] == "AP16") { AP16Tel = data; }
                if (data[0] == "AP17") { AP17Tel = data; }
                dataChanged = true;                
            }
            else {
                dataChanged = false;
                return; //do nothing because the message isn't Apollo telemetry
            }
            if (dataChanged) { updateScreen();}

        }
        public string parseTelemetry(string[] telBrick)
        {
            string parsed = "Apollo ";
            if(telBrick.Length < 2)
            {
                parsed += telBrick[0].Remove(0, 2) + ":\n    No telemetry!\n";
                return parsed;
            }
            parsed += telBrick[0].Remove(0,2) + ":\n    Altitude:  " + telBrick[1];
            if (telBrick[1]!="Space") { parsed += "m"; }
            parsed += "\n    Velocity:  " + telBrick[2];
            if (telBrick[2]!="Stable") { parsed += "m/s"; }
            parsed += "\n    Uranium:  " + telBrick[3] + "kg";
            parsed += "\n    Fuel:  " + telBrick[4] + "%";
            parsed += "\n    Battery:  " + telBrick[5] + "%";
            parsed += "\n    Distance to Relay:  " + telBrick[6] + "m\n";
            return parsed;
        }
        public void updateScreen()
        {
            telemetryScreen = GridTerminalSystem.GetBlockWithName(telemetryScreenName) as IMyTextPanel;
            telemetryScreen2 = GridTerminalSystem.GetBlockWithName(telemetryScreenName + " 2") as IMyTextPanel;
            telemetryScreen.WritePublicText(parseTelemetry(AP11Tel));
            telemetryScreen.WritePublicText(parseTelemetry(AP12Tel), true);
            telemetryScreen.WritePublicText(parseTelemetry(AP13Tel), true);
            telemetryScreen.ShowPublicTextOnScreen();
            telemetryScreen2.WritePublicText(parseTelemetry(AP14Tel));
            telemetryScreen2.WritePublicText(parseTelemetry(AP15Tel), true);
            telemetryScreen2.WritePublicText(parseTelemetry(AP16Tel), true);
            telemetryScreen2.WritePublicText(parseTelemetry(AP17Tel), true);
            telemetryScreen2.ShowPublicTextOnScreen();
            dataChanged = false;
        }
    }
}