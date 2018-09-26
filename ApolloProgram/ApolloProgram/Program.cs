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
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        #region mdk preserve
        public const int buildNumber = 0; //Change me        
        public bool bingoFuel = false;
        public bool bingoCharge = false;
        public bool inSpace = false;
        public bool isStable = false;
        public bool selfDestruct = false;
        public bool leapfrog = false;
        public float altThrust = 210000;
        public int maxAlt = 43000;
        public int maxDistToRelay = 40000;
        public double relayX = 58128.1557992112;//AP11
        public double relayY = 43883.8754036276;
        public double relayZ = 73665.0987876108;
        public Vector3D relayPos;
        public string APH2Name = "AP" + buildNumber + "-H2Tank";
        public string APBattName = "AP" + buildNumber + "-Batt";
        public string APRCName = "AP" + buildNumber + "-RC";
        public string APAntGrName = "Apollo " + buildNumber;
        public string APH2AltName = "AP" + buildNumber + "H2Alt";
        public string APH2CtrlName = "AP" + buildNumber + "H2Ctrl";
        public string APGyroName = "AP" + buildNumber + "-Gyro";
        public string APReactName = "AP" + buildNumber + "-Reactor";
        public string APScuttleGrName = "AP" + buildNumber + "-Scuttle";
        #endregion
        public Program()
        {         
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }
        public void Main()
        {
            relayPos.X = relayX;
            relayPos.Y = relayY;
            relayPos.Z = relayZ;
            IMyGasTank APH2;
            IMyBatteryBlock APBatt;
            IMyRadioAntenna APAnt;
            IMyRemoteControl APRC;
            IMyThrust APH2Alt;
            IMyBlockGroup APH2Ctrl;
            IMyGyro APGyro;
            IMyReactor APReact;
            IMyBlockGroup APScuttle;
            //Antenna fuckery
            IMyBlockGroup APAntGr = GridTerminalSystem.GetBlockGroupWithName(APAntGrName);
            List<IMyTerminalBlock> antennas = new List<IMyTerminalBlock>();
            APAntGr.GetBlocks(antennas);
            long APAntID = 0;//EntityID of the antenna on the grid, set to 0 here to avoid errors as it's set a few lines down
            foreach (var antenna in antennas) { APAntID = antenna.EntityId; }//There's only supposed to be one antenna per Apollo grid so fuck you
            APAnt = GridTerminalSystem.GetBlockWithId(APAntID) as IMyRadioAntenna;
            //Antenna fuckery
            APH2 = GridTerminalSystem.GetBlockWithName(APH2Name) as IMyGasTank;
            APBatt = GridTerminalSystem.GetBlockWithName(APBattName) as IMyBatteryBlock;
            APRC = GridTerminalSystem.GetBlockWithName(APRCName) as IMyRemoteControl;            
            APH2Alt = GridTerminalSystem.GetBlockWithName(APH2AltName) as IMyThrust;
            APH2Ctrl = GridTerminalSystem.GetBlockGroupWithName(APH2CtrlName);
            APGyro = GridTerminalSystem.GetBlockWithName(APGyroName) as IMyGyro;
            APReact = GridTerminalSystem.GetBlockWithName(APReactName) as IMyReactor;
            APScuttle = GridTerminalSystem.GetBlockGroupWithName(APScuttleGrName);
            /*
            //DEBUG:  Show reference block names
            Echo("Hydrogen Tank: " + APH2.CustomName);
            Echo("Battery: " + APBatt.CustomName);
            Echo("Antenna: " + APAnt.CustomName);
            Echo("Remote Control: " + APRC.CustomName);
            Echo("Parachute: " + APChute.CustomName);
            Echo("Altitude Thrust: " + APH2Alt.CustomName);
            Echo("Control Thrust: " + APH2Ctrl.Name);
            Echo("Gyro: " + APGyro.CustomName);
            Echo("Reactor: " + APReact.CustomName);
            Echo("Scuttle Charges: " + APScuttle.Name);
            */            
            //Print coordinates
            Vector3D myPos = APAnt.GetPosition();
            Echo("X: " + myPos.X);
            Echo("Y: " + myPos.Y);
            Echo("Z: " + myPos.Z);
            //Check ALT
            double altitude;
            APRC.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
            Echo("Altitude: " + (int)altitude);
            if (Double.IsInfinity(altitude)) { inSpace = true; }
            //Check velocity
            double velocity = APRC.GetShipSpeed();
            Echo("Velocity: " + (int)velocity + "m/s");
            if (velocity == 0) { isStable = true; }
            if (velocity > 0) { isStable = false; }
            //Set Altitude Thruster override based on altitude
            if (!Double.IsInfinity(altitude))
            {
                APRC.DampenersOverride = false;
                float percentSpace = (float)altitude / maxAlt;
                float percentSpaceInv = 1 - percentSpace;
                float Override = altThrust * (percentSpaceInv);
                Echo("Thrust override: " + Math.Round((percentSpaceInv*100)) + "%");
                APH2Alt.ThrustOverride = (Override);
            }
            if (Double.IsInfinity(altitude)) { APH2Alt.ThrustOverride = 0; }
            //Keep velocity lower than 100m/s while in atmo
            if ((int)velocity > 100 && !inSpace) //only moderate speed in atmo
            { APH2Alt.Enabled = false; }
            if ((int)velocity <= 100 && !inSpace) { APH2Alt.Enabled = true; }
            //Check fuel
            double fuelCur = APH2.FilledRatio;
            float fuelPer = (float)fuelCur * 100;
            Echo("Remaining Fuel: " + (int)fuelPer + "%");
            if (fuelCur <= 0.25) { bingoFuel = true; }
            if (fuelCur > 0.25) { bingoFuel = false; }
            //Check uranium            
            long reactUranium = 0;
            foreach (IMyInventoryItem item in APReact.GetInventory(0).GetItems()) { reactUranium += item.Amount.RawValue; }
            Echo("Remaining uranium: " + Math.Round(((double)reactUranium / 1000000),2));
            //Check battery
            float maxBatt = APBatt.MaxStoredPower;
            float curBatt = APBatt.CurrentStoredPower;
            float battLevel = curBatt / maxBatt;
            float battPercent = battLevel * 100;
            Echo("Remaining Battery: " + (int)battPercent + "%");
            if (battLevel <= 0.25) { bingoCharge = true; }
            if (battLevel > 0.25) { bingoCharge = false; }
            //In case of imminent power loss, activate self destruct
            if (battLevel <= 0.05 && reactUranium <=0) { selfDestruct = true; List<IMyWarhead> APScuttleGrp = new List<IMyWarhead>(); APScuttle.GetBlocksOfType<IMyWarhead>(APScuttleGrp); foreach (var scuttleCharge in APScuttleGrp) { scuttleCharge.StartCountdown(); } }
            //If we're in space but still moving, disengage launch systems
            if (inSpace == true && isStable == false && leapfrog == false)
            {
                if (APRC.DampenersOverride == false) { APRC.DampenersOverride = true; }
                if (APH2Alt.ThrustOverride > 0) { APH2Alt.ThrustOverride = 0; }
                List<IMyThrust> APH2CtrlGrp = new List<IMyThrust>();
                APH2Ctrl.GetBlocksOfType<IMyThrust>(APH2CtrlGrp);
                foreach (var ctrlThrst in APH2CtrlGrp) { if (!ctrlThrst.Enabled) { ctrlThrst.Enabled = true; } }
            }
            //Once manual positioning to near relay to extend complete and orientation complete
            //Manual setting of bool leapfrog required
            if (inSpace == true && leapfrog == true)
            {
                //Enable control thrusters just in case
                List<IMyThrust> APH2CtrlGrp = new List<IMyThrust>();
                APH2Ctrl.GetBlocksOfType<IMyThrust>(APH2CtrlGrp);
                foreach (var ctrlThrst in APH2CtrlGrp) { if (!ctrlThrst.Enabled) { ctrlThrst.Enabled = true; } }
                //Make sure we're not too far from the relay
                Vector3D myPosChk = APAnt.GetPosition();
                double distanceFromRelay = Math.Round(Vector3D.Distance(relayPos, myPosChk), 2);
                if (distanceFromRelay >= maxDistToRelay) { APRC.DampenersOverride = true; APH2Alt.ThrustOverride = 0; APH2Alt.Enabled = false; } //If we are, stop stop stop
                else { APRC.DampenersOverride = false; } //Otherwise go
                Echo("Distance from relay (AP" + (buildNumber - 1) + "): " + distanceFromRelay + "m");
                if (distanceFromRelay < maxDistToRelay)
                {
                    if (velocity <= 100) { APH2Alt.ThrustOverride = 100000; APH2Alt.Enabled = true; APRC.DampenersOverride = false; }
                    if (velocity > 100) { APH2Alt.Enabled = false; APH2Alt.ThrustOverride = 0; APRC.DampenersOverride = true; }
                }
            }
            updateAntenna(APAntID, altitude, velocity, reactUranium, fuelPer, battPercent);
            sendTelemetry(APAntID, altitude, velocity, reactUranium, fuelPer, battPercent, APScuttle);
        }
        public void updateAntenna(long antID, double altitude, double velocity, long reacturanium, float fuelPer, float battPercent)
        {
            IMyRadioAntenna APAnt = GridTerminalSystem.GetBlockWithId(antID) as IMyRadioAntenna;
            APAnt.CustomName = APAntGrName;  //Reset name so we can update it
            if (selfDestruct == true) { APAnt.CustomName += " !!!SELF DESTRUCT IN PROGRESS!!!  Goodbye."; return; }
            if (!inSpace) { APAnt.CustomName += " ALT: " + (int)altitude; }
            if (velocity != 0) { APAnt.CustomName += " VEL: " + (int)velocity; }
            APAnt.CustomName += " Fuel: " + (int)fuelPer + "%";
            APAnt.CustomName += " Batt: " + (int)battPercent + "%";
            if (isStable && inSpace) { APAnt.CustomName += " Stable"; }
            if (bingoFuel == true) { APAnt.CustomName += " WARN: Bingo Fuel!"; }
            if (bingoCharge == true) { APAnt.CustomName += " WARN: Low battery!"; }
        }
        public void sendTelemetry(long antID, double altitude, double velocity, long reactUranium, float fuelPer, float battPercent, IMyBlockGroup Scuttle)
        {
            IMyRadioAntenna APAnt = GridTerminalSystem.GetBlockWithId(antID) as IMyRadioAntenna;
            string myTelemetry = "AP" + buildNumber;
            if (selfDestruct)
            {
                List<IMyWarhead> APScuttleGrp = new List<IMyWarhead>();
                Scuttle.GetBlocksOfType<IMyWarhead>(APScuttleGrp);
                myTelemetry += "|SD";
                myTelemetry += "|" + APScuttleGrp[0].DetonationTime.ToString();
                APAnt.TransmitMessage(myTelemetry);
                return;
            } //No sense in sending further data. F
            if (!inSpace) { myTelemetry += "|" + (int)altitude; }
            if (inSpace && !isStable) { myTelemetry += "|Space"; }
            if (!isStable) {  myTelemetry += "|" + (int)velocity; }
            if (isStable && !inSpace) { myTelemetry += "|Stable"; }
            if (isStable && inSpace) { myTelemetry += "|Space|Stable"; }
            myTelemetry += "|" + Math.Round(((double)reactUranium / 1000000), 2);
            if (bingoFuel) { myTelemetry += "|WRN"; }
            if (bingoCharge) { myTelemetry += "|WRN"; }
            if (!bingoFuel) { myTelemetry += "|" + (int)fuelPer; }
            if (!bingoCharge) { myTelemetry += "|" + (int)battPercent; }
            Vector3D myPosChkTel = APAnt.GetPosition();
            double distanceFromRelayTel = Math.Round(Vector3D.Distance(relayPos, myPosChkTel), 2);
            myTelemetry += "|" + distanceFromRelayTel;
            APAnt.TransmitMessage(myTelemetry);
            return;
        }
    }   
}