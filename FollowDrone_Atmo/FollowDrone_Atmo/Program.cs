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
        public const string namebase = "FD001-"; //naming prefix for blocks we care about (e.g.: FD001-Reactor 1)
        public List<IMyTerminalBlock> selfgrid = new List<IMyTerminalBlock>();
        public List<IMyTerminalBlock> selfgridgiveashit = new List<IMyTerminalBlock>();
        public List<string> wantedtypes = new List<string>() { "IMyGasTank", "IMyGyro", "IMyBatteryBlock", "IMyReactor", "IMyBlockGroup", "IMyRadioAntenna", "IMyRemoteControl", "IMyThrust", "IMyLandingGear", "IMySensorBlock" };
        public List<string> coreTypes = new List<string>() {"IMyBatteryBlock", "IMyReactor", "IMyGasTank"};
        public List<string> secondaryTypes = new List<string>() { "IMyRadioAntenna", "IMyGyro", "IMySensorBlock", "IMyThrust", "IMyLandingGear", "IMyRemoteControl" };
        public int minAlt = 20;
        public int minUr = 1;
        public int divergenceThreshold = 20; //how much valuesold and values can diverge negatively
        public const double CTRL_COEFF = 0.3; //defines the strength of Pitch/Roll/Yaw correction

        public Program() { Runtime.UpdateFrequency = UpdateFrequency.Update10; }

        public void Main()
        {
            //LIST handling:
            initList();
            try
            {
                checkCore();
                damageCheck();
                checkPos();
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
            if (selfgridgiveashit.Count == 0)
            {
                foreach (string kind in wantedtypes)
                {
                    List<IMyTerminalBlock> tmpall = new List<IMyTerminalBlock>();
                    GridTerminalSystem.GetBlocks(tmpall);
                    foreach(IMyTerminalBlock block in tmpall) { if (block.BlockDefinition.TypeIdString == kind) { selfgridgiveashit.Add(block);}}
                }

            }

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
            foreach (KeyValuePair<string, float> kvp in values)
            {
                valuesold[kvp.Key] = kvp.Value;
            }
            /*{ valuesold[kvp] = values[kvp]; }*/
            //Get H2 tank fill average
            float tgas = 0.0f;
            int tgascnt = 0;
            foreach (IMyGasTank tank in selfgridgiveashit)
            {
                if (!tank.Name.Contains("Reserve"))
                {
                    tgas += (float)tank.FilledRatio * 100;
                    tgascnt++;
                }
            }
            tgas = tgas / tgascnt; //Average out total fill %
            values[careabout[0]] = tgas;
            if (tgas <= 25)
            { engageReserves(); throw new LandingException("Bingo fuel"); }
            //Get battery charge average
            float tbchg = 0.0f;
            int tbcnt = 0;
            foreach (IMyBatteryBlock batt in selfgridgiveashit)
            { tbchg += batthelper(batt); tbcnt++; }
            tbchg = tbchg / tbcnt; //Average out total battery charge %
            values[careabout[2]] = tbchg;
            if (tbchg <= 25)
            {
                throw new LandingException("Low battery");
            }
            //Get Uranium ingots left in reactor(s)
            long tUr = 0;
            double tUrI = 0;
            foreach (IMyReactor react in selfgridgiveashit) {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                react.GetInventory().GetItems(items);
                foreach (MyInventoryItem itm in items) { tUr += itm.Amount.RawValue; } }
            tUrI = Math.Round(((double)tUr / 1000000), 2);
            values[careabout[1]] = (float)tUrI;
            if (tUrI < minUr)
            {
                throw new LandingException("Uranium low");
            }
            //Compare current values to previous
            foreach (string check in careabout)
            {
                float tcheck = valuesold[check] - values[check];
                if (tcheck >= 0)
                {
                    if (tcheck >= divergenceThreshold)
                    {
                        throw new LandingException(string.Format("Value of {0} exceeded negative change threshold", check));
                    }
                }
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
            List<IMyRemoteControl> tRC = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(tRC);
            RC = tRC[0]; //fuck me, that's retarded
            //Get applicable thrusters
            List<IMyThrust> lift = new List<IMyThrust>();
            List<IMyThrust> tlift = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(tlift);
            foreach (IMyThrust thruster in tlift) { if (thruster.Orientation.Forward.ToString() == "Up") { lift.Add(thruster); } }
            //Try to keep drone at minAlt
            //TODO:  set thrust override based on velocity AND total mass
            float thrustoverride = 1.0f;
            double altitude;
            RC.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
            while (altitude <= minAlt)
            {
                //slowly reduce thrust override until we're balanced
                //hopefully, anyway
                while (RC.GetShipSpeed() >= 0) { foreach (IMyThrust thruster in lift) { thruster.Enabled = true; thruster.ThrustOverride = thrustoverride; } thrustoverride -= 0.05f; }
            }
            while (altitude > minAlt)
            {
                while (RC.GetShipSpeed() <= 5) { foreach (IMyThrust thruster in lift) { thruster.Enabled = false; } } //cut lift thrust
                foreach (IMyThrust thruster in lift) { thruster.Enabled = true; } //moderate speed
            }
        }
        public void correctOrient()
        {
            //Get remote control
            IMyRemoteControl RC;
            List<IMyRemoteControl> tRC = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(tRC);
            RC = tRC[0]; //still retarded
            //Get gyro
            IMyGyro mGy;
            List<IMyGyro> tmGy = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(tmGy);
            mGy = tmGy[0];
            //Check pitch/roll/yaw compared to gravity
            Matrix orient;
            RC.Orientation.GetMatrix(out orient);
            Vector3D down = orient.Down;
            Vector3D grav = RC.GetNaturalGravity();
            grav.Normalize();
            mGy.Orientation.GetMatrix(out orient);
            var lDown = Vector3D.Transform(down, MatrixD.Transpose(orient));
            var lGrav = Vector3D.Transform(grav, MatrixD.Transpose(mGy.WorldMatrix.GetOrientation()));
            var rot = Vector3D.Cross(lDown, lGrav);
            double ang = rot.Length();
            ang = Math.Atan2(ang, Math.Sqrt(Math.Max(00, 1.0 - ang * ang)));
            if (ang > 0.01)
            {
                double ctrl_vel = mGy.GetMaximum<float>("Yaw") * (ang / Math.PI) * CTRL_COEFF;
                ctrl_vel = Math.Min(mGy.GetMaximum<float>("Yaw"), ctrl_vel);
                ctrl_vel = Math.Max(0.01, ctrl_vel);
                rot.Normalize();
                rot *= ctrl_vel;
                mGy.SetValueFloat("Pitch", (float)rot.GetDim(0));
                mGy.SetValueFloat("Yaw", -(float)rot.GetDim(1));
                mGy.SetValueFloat("Roll", -(float)rot.GetDim(2));
                mGy.SetValueFloat("Power", 1.0f);
                mGy.GyroOverride = true;
            }
            if (ang < 0.01) { mGy.GyroOverride = false; }
            return;
        }
        public void damageCheck()
        {
            foreach (IMyTerminalBlock block in selfgridgiveashit) { if (getHealth(block) <= 0.75f) { throw new LandingException(string.Format("Damage to {0}, {1}", block.BlockDefinition.TypeIdString, block.Name)); } } 
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
            //First, horrific antenna fuckery
            IMyRadioAntenna ant;
            long antID = 0;
            List<IMyRadioAntenna> ants = new List<IMyRadioAntenna>();
            foreach (IMyTerminalBlock block in selfgridgiveashit) { if (block.BlockDefinition.TypeIdString == "IMyRadioAntenna") { ants.Add(block as IMyRadioAntenna); }}
            if (ants.Count >= 0) { foreach (IMyRadioAntenna tant in ants) { antID = tant.EntityId; }}
            try { ant = GridTerminalSystem.GetBlockWithId(antID) as IMyRadioAntenna; }
            catch (Exception ex) { throw new LandingException("Antenna bind failure"); }
            //holy fuck, I hate myself for that
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
        public void land()
        {
            //TODO:  any landing you walk away from
        }
        public void checkProx()
        {
            //TODO:  where's owner?
        }
    }
}