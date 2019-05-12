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
        public List<string> careabout = new List<string>() {"h2", "U", "battery"};
        public Dictionary<string, bool> alerts = new Dictionary<string, bool>();
        public Dictionary<string, float> values = new Dictionary<string, float>();
        public Dictionary<string, float> valuesold = new Dictionary<string, float>();
        public List<IMyTerminalBlock> selfgridgiveashit = new List<IMyTerminalBlock>();
        public TimeSpan LastElapsed;
        public int lastAltAct = 0; //Last action taken by correctAlt() 0: no action, 1: lift thrust, 2: descent thrust
        #region mdk preserve
        public const string namebase = "FD001-"; //naming prefix for blocks we care about (e.g.: FD001-Reactor 1)
        public int minAlt = 20; //Minimum altitude to maintain (meters)
        public int minUr = 1;  //Minimum number of uranium ingots in system
        public const int damageCheckInterval = 5; //How many cycles do we wait between each run of damageCheck()?
        public int tdCI = 0; //Counter for damageCheck interval
        //public string teltag = "FollowDrone"; //Tag to identify valid broadcast targets, apparently.  Thanks, Keen
        public int divergenceThreshold = 20; //how much valuesold and values can diverge negatively
        public const double CTRL_COEFF = 0.3; //defines the strength of Pitch/Roll/Yaw correction
        public const bool damageCare = false; //do we give a shit about damage?  if true, drone will attempt landing if damaged
        public float thrustoverride = 0.40f; //percentage of thruster override
        public int altMargin = 5; //minAlt +/- altMargin gives drone target altitude range
        public int thrustduration = 3000; //time in milliseconds per THRUST action
        #endregion
        public Program() { Runtime.UpdateFrequency = UpdateFrequency.Update10;}

        public void Main()
        {
            //LIST handling:
            initList();
            try
            {
                checkCore();
                if (damageCare)
                {
                    //DEBUG:
                    Echo("Running damage checks...");
                    Echo("Damage check counter is " + tdCI);
                    if (tdCI == damageCheckInterval) { damageCheck(); tdCI = 0; }
                    else { tdCI++; }
                }
                //DEBUG:
                Echo("Running positional checks...");
                checkPos();
                //DEBUG:
                Echo("Running owner proximity check...");
                checkProx();
            }
            catch (LandingException ex)
            {
                Echo(ex.Message);
                land();
            }
            catch (Exception)
            {
                Echo("Unknown error, landing!");
                land();
            }
           
        }
        public void initList()
        {
            //Init alerts, values, valuesold dictionaries based on data in careabout
            if (alerts.Count != careabout.Count && values.Count != careabout.Count && valuesold.Count != careabout.Count)
            {
                for (int index = 0; index < careabout.Count; index++)
                {
                    alerts.Add(careabout[index], false);
                    values.Add(careabout[index], 1.0f);
                    valuesold.Add(careabout[index], 1.0f);
                }
            }
            //Init block lists
            if (selfgridgiveashit.Count == 0) { GridTerminalSystem.GetBlocks(selfgridgiveashit);}
        }
        public class LandingException : Exception
        {
            public LandingException() {}
            public LandingException(string message)
                :base(string.Format("Landing immediately.  Cause: {0}",message))
            {}
            public LandingException(string message, Exception inner) {}
        }
        
        public void checkCore()
        {
            //Store previous values
            foreach (KeyValuePair<string, float> kvp in values) { valuesold[kvp.Key] = kvp.Value;}
            //Get velocity
            IMyRemoteControl RC;
            var tRC = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(tRC);
            RC = tRC[0];
            var velocity = RC.GetShipSpeed();
            Echo("Vel: " + (int)velocity + "m/s");
            //Get altitude
            double altitude;
            RC.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
            Echo("Alt: " + (int)altitude + "m");
            //Get H2 tank fill average
            float tgas = 0.0f;
            int tgascnt = 0;
            var ttanks = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType(ttanks);
            foreach (IMyGasTank tank in ttanks) { if(tank.DisplayNameText.Contains("H2") || tank.DisplayNameText.Contains("Hydrogen")) { if (!tank.DisplayNameText.Contains("Reserve")) { tgas += (float)tank.FilledRatio * 100; tgascnt++; } } }
            tgas = tgas / tgascnt; //Average out total fill %
            values[careabout[0]] = tgas;
            Echo("H2: " + tgas + "%");
            if (tgas <= 25) { engageReserves(); throw new LandingException("On fuel reserves"); }
            //Get battery charge average
            float tbchg = 0.0f;
            int tbcnt = 0;
            var tbatts = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(tbatts);
            foreach (IMyBatteryBlock batt in tbatts)
            { tbchg += batthelper(batt); tbcnt++; }
            tbchg = tbchg / tbcnt; //Average out total battery charge %
            values[careabout[2]] = tbchg;
            Echo("Battery: " + tbchg + "%");
            if (tbchg <= 25) { throw new LandingException("Low battery"); }
            //Get Uranium ingots left in reactor(s)
            long tUr = 0;
            double tUrI = 0;
            var treact = new List<IMyReactor>();
            GridTerminalSystem.GetBlocksOfType<IMyReactor>(treact);
            foreach (IMyReactor react in treact) {
                var items = new List<MyInventoryItem>();
                react.GetInventory().GetItems(items);
                foreach (MyInventoryItem itm in items) { tUr += itm.Amount.RawValue; } }
            tUrI = Math.Round(((double)tUr / 1000000), 2);
            values[careabout[1]] = (float)tUrI;
            //DEBUG:  show calculated uranium amount
            Echo("Total Uranium: " + (float)tUrI + "kg");
            if (tUrI < minUr) { throw new LandingException("Uranium low");}
            //Compare current values to previous
            foreach (string check in careabout)
            {
                float tcheck = valuesold[check] - values[check];
                if (tcheck >= 0) { if (tcheck >= divergenceThreshold) { throw new LandingException(string.Format("Value of {0} exceeded negative change threshold", check));}}
            }
            //Send telemetry home
            sendTelemetry(values);
            return;
        }
        public void checkPos()
        {
            correctOrient();
            correctAlt();
        }
        public void correctAlt()
        {
            //Get remote control
            IMyRemoteControl RC;
            var tRC = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(tRC);
            RC = tRC[0]; //fuck me, that's retarded
            double altitude;
            RC.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
            var lifts = new List<IMyThrust>();
            var drops = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(lifts, lift => lift.Orientation.Forward.ToString() == "Down");
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(drops, drop => drop.Orientation.Forward.ToString() == "Up");
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
                    resetThrust();
                    throw new LandingException("lastAltAct in unexpected state");
            }
        }
        public void correctOrient()
        {
            //Get remote control
            IMyRemoteControl RC;
            var tRC = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(tRC);
            RC = tRC[0]; //still retarded
            //Get gyros
            var tmGy = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(tmGy);            
            //Check pitch/roll/yaw compared to gravity
            Matrix orient;
            RC.Orientation.GetMatrix(out orient);
            Vector3D down = orient.Down;
            Vector3D grav = RC.GetNaturalGravity();
            grav.Normalize();
            foreach (IMyGyro gyro in tmGy)
            {
                gyro.Orientation.GetMatrix(out orient);
                var lDown = Vector3D.Transform(down, MatrixD.Transpose(orient));
                var lGrav = Vector3D.Transform(grav, MatrixD.Transpose(gyro.WorldMatrix.GetOrientation()));
                var rot = Vector3D.Cross(lDown, lGrav);
                double ang = rot.Length();
                ang = Math.Atan2(ang, Math.Sqrt(Math.Max(00, 1.0 - ang * ang)));
                if (ang > 0.01)
                {
                    double ctrl_vel = gyro.GetMaximum<float>("Yaw") * (ang / Math.PI) * CTRL_COEFF;
                    ctrl_vel = Math.Min(gyro.GetMaximum<float>("Yaw"), ctrl_vel);
                    ctrl_vel = Math.Max(0.01, ctrl_vel);
                    rot.Normalize();
                    rot *= ctrl_vel;
                    gyro.SetValueFloat("Pitch", (float)rot.GetDim(0));
                    gyro.SetValueFloat("Yaw", -(float)rot.GetDim(1));
                    gyro.SetValueFloat("Roll", -(float)rot.GetDim(2));
                    gyro.SetValueFloat("Power", 1.0f);
                    gyro.GyroOverride = true;
                }
                if (ang < 0.01) { gyro.GyroOverride = false; gyro.SetValueFloat("Pitch", 0); gyro.SetValueFloat("Yaw", 0); gyro.SetValueFloat("Roll", 0); }
            }
            return;
        }
        public void damageCheck()
        {
            foreach (IMyTerminalBlock block in selfgridgiveashit) { if (getHealth(block) <= 0.75f) { throw new LandingException(string.Format("Damage to {0}, {1}", block.BlockDefinition.TypeIdString.Split('_')[1], block.Name)); } } 
            return;
        }
        public float getHealth(IMyTerminalBlock block)
        {
            IMySlimBlock target = block.CubeGrid.GetCubeBlock(block.Position);
            float maxIntegrity = target.MaxIntegrity;
            float buildIntegrity = target.BuildIntegrity;
            float currentDamage = target.CurrentDamage;
            return (buildIntegrity - currentDamage) / maxIntegrity;
        }
        public void sendTelemetry(Dictionary<string, float> data)
        {
            IMyRadioAntenna ant;
            List<IMyRadioAntenna> ants = new List<IMyRadioAntenna>();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(ants);
            try { ant = ants[0]; }
            catch (Exception) { throw new LandingException("Antenna bind failure"); }
            string message = namebase;
            foreach (string str in careabout)
            { message += " " + str + " " + values[str]; }
            ant.TransmitMessage(message);
            return;
        }
        public void engageReserves()
        {
            //Allow fuel to be pulled from reserve tanks for landing
            foreach (IMyGasTank tank in selfgridgiveashit) { if (tank.Name.Contains("Reserve")) { tank.Stockpile = false; } }
            return;
        }
        public float batthelper(IMyBatteryBlock battery)
        {
            //Because batteries don't have FillRatio
            float battmax = battery.MaxStoredPower;
            float battcur = battery.CurrentStoredPower;
            float battratio = battcur / battmax;
            float battper = battratio * 100;
            return battper;
        }
        public void engageThrust(List<IMyThrust> targets)
        {
            IMyRemoteControl RC;
            var tRC = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(tRC);
            RC = tRC[0];
            RC.DampenersOverride = false;
            foreach (IMyThrust thruster in targets) { thruster.ThrustOverridePercentage = thrustoverride; LastElapsed = Runtime.TimeSinceLastRun; lastAltAct = 0; }
            return;
        }
        public void resetThrust()
        {
            IMyRemoteControl RC;
            var tRC = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(tRC);
            RC = tRC[0];
            RC.DampenersOverride = true;
            var targets = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(targets);
            foreach (IMyThrust lthruster in targets) { lthruster.ThrustOverridePercentage = 0; }
            lastAltAct = 0;
            LastElapsed = Runtime.TimeSinceLastRun;
            return;
        }
        public void land()
        {
            resetThrust();
            correctOrient();
        }
        public void checkProx()
        {
            //TODO:  where's owner?
        }
    }
}