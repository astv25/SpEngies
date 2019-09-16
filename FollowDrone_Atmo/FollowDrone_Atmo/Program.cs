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
        public List<string> careabout = new List<string>() { "h2", "U", "battery", "alt", "vel" };
        public Dictionary<string, bool> alerts = new Dictionary<string, bool>();
        public Dictionary<string, float> values = new Dictionary<string, float>();
        public Dictionary<string, float> valuesold = new Dictionary<string, float>();
        public List<IMyTerminalBlock> selfgridgiveashit = new List<IMyTerminalBlock>();
        public TimeSpan LastElapsed;
        public int lastAltAct = 0; //Last action taken by correctAlt() 0: no action, 1: lift thrust, 2: descent thrust
        public bool initSensor = false; //Have sensors been initialized?
        public IMyProgrammableBlock gyroctrl;
        public IMyProgrammableBlock thrustctrl;
        #region mdk preserve
        public const string namebase = "FD001-"; //naming prefix for blocks we care about (e.g.: FD001-Reactor 1)
        public const string gyrooffload = null; //Name of programmable block to offload gyro control to, null for no offload
        public const string thrustoffload = null; //Name of programmable to offload thruster control to, null for no offload
        public int minAlt = 20; //Minimum altitude to maintain (meters)
        public int minDist = 20; //Distance from owner to maintain
        public int minUr = 1;  //Minimum number of uranium ingots in system
        public const int damageCheckInterval = 5; //How many cycles do we wait between each run of damageCheck()?
        public int tdCI = 0; //Counter for damageCheck interval
        public int divergenceThreshold = 20; //how much valuesold and values can diverge negatively
        public const double CTRL_COEFF = 0.3; //defines the strength of Pitch/Roll/Yaw correction
        public const bool damageCare = false; //do we give a shit about damage?  if true, drone will attempt landing if damaged
        public float thrustoverride = 0.40f; //percentage of thruster override
        public int altMargin = 5; //minAlt +/- altMargin gives drone target altitude range
        public int thrustduration = 3000; //time in milliseconds per THRUST action
        public int altlossthresh = 50; //at how many meters of altitude lost between cycles do we panic?
        #endregion
        public Program() { Runtime.UpdateFrequency = UpdateFrequency.Update10;}

        public void Main()
        {
            //LIST handling:
            initList();
            try
            {
                //Override existing configuration of sensors
                if (!initSensor) { initSensors(); }
                checkCore();
                if (damageCare)
                {
                    if (tdCI == damageCheckInterval) { damageCheck(); tdCI = 0; }
                    else { tdCI++; }
                }
                checkPos();
                checkProx();
            }
            catch (LandingException ex)
            {
                Echo(ex.Message);
                land();
            }
            catch (ChuteException ex)
            {
                Echo(ex.Message);
                chutechutechute();
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
            //Init block lists and statics
            if (selfgridgiveashit.Count == 0) { GridTerminalSystem.GetBlocks(selfgridgiveashit);}
            if (gyrooffload != null)
            {
                gyroctrl = GridTerminalSystem.GetBlockWithName(gyrooffload) as IMyProgrammableBlock;
                gyroctrl.TryRun("INIT");
            }
            if (thrustoffload != null)
            {
                thrustctrl = GridTerminalSystem.GetBlockWithName(thrustoffload) as IMyProgrammableBlock;
                thrustctrl.TryRun("INIT, " + minAlt.ToString() + ", " + altMargin.ToString() + ", " + thrustduration.ToString() + ", " + thrustoverride.ToString());
            }
        }
        public void initSensors()
        {
            var sensors = new List<IMySensorBlock>();
            GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors);
            foreach(IMySensorBlock sensor in sensors)
            {
                sensor.Enabled = true;
                sensor.DetectAsteroids = false;
                sensor.DetectEnemy = false;
                sensor.DetectFriendly = false;
                sensor.DetectFloatingObjects = false;
                sensor.DetectLargeShips = false;
                sensor.DetectNeutral = false;
                sensor.DetectOwner = true;
                sensor.DetectPlayers = true;
                sensor.DetectSmallShips = false;
                sensor.DetectSubgrids = false;
                sensor.FrontExtend = 50;
                sensor.LeftExtend = 2;
                sensor.RightExtend = 2;
                sensor.TopExtend = 10;
                sensor.BottomExtend = 10;
                sensor.BackExtend = 0;
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
        public class ChuteException : Exception
        {
            public ChuteException() {}
            public ChuteException(string message)
                : base(string.Format("Deploying parachute.  Cause: {0}", message))
            {}
            public ChuteException(string message, Exception inner) {}
        }
        
        public void checkCore()
        {
            //Store previous values
            foreach (KeyValuePair<string, float> kvp in values) { valuesold[kvp.Key] = kvp.Value;}
            //Get velocity
            IMyRemoteControl RC = getRC();
            var velocity = RC.GetShipSpeed();
            values["vel"] = (float)velocity;
            Echo("Vel: " + (int)velocity + "m/s");
            //Get altitude
            double altitude;
            RC.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
            values["alt"] = (float)altitude;
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
            Echo("Uranium: " + (float)tUrI + "kg");
            if (tUrI < minUr) { throw new LandingException("Uranium low");}
            //Compare current values to previous
            //Altitude:
            if ((valuesold["alt"]-values["alt"]) >= altlossthresh) { throw new ChuteException("Rapid descent"); }
            foreach (string check in careabout)//TODO: add specific aberration checks (e.g. Altitude) and specific handling for those [using separate exceptions]
            {
                if(check=="alt") { continue; }//already checked above
                if ((valuesold[check]-values[check]) >= 0) { if ((valuesold[check]-values[check]) >= divergenceThreshold) { throw new LandingException(string.Format("Value of {0} exceeded negative change threshold", check));}}
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
            try //Check for configured thrust control offload
            {
                if (thrustoffload != null)
                {
                    if (thrustctrl.CustomData == "READY") { thrustctrl.TryRun("RUN"); return; }
                    if (thrustctrl.CustomData == "PROCESSING") { return; }
                    else { throw new Exception(); }
                }
            }
            catch (Exception)
            {
                Echo("WARN:  Thrust offload failure, attempting local control.");
            }
            
            IMyRemoteControl RC = getRC();
            double altitude;
            RC.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
            var lifts = new List<IMyThrust>();
            var drops = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(lifts, lift => lift.Orientation.Forward.ToString() == "Down");
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(drops, drop => drop.Orientation.Forward.ToString() == "Up");
            if (Runtime.TimeSinceLastRun < LastElapsed.Add(TimeSpan.FromMilliseconds(thrustduration))) { lastAltAct = 0; return; }//Only take action after thrustduration delay
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
                    throw new LandingException("lastAltAct in unexpected state");
            }
        }
        public void correctOrient()
        {
            try
            {
                try //Check for a configured offload block
                {   
                    if (gyrooffload != null)
                    {
                        if (gyroctrl.CustomData == "READY") { gyroctrl.TryRun(CTRL_COEFF.ToString()); return; }
                        if (gyroctrl.CustomData == "PROCESSING") { return; }
                        else { throw new Exception(); }
                    }
                }
                catch (Exception)
                {
                    Echo("WARN:  Gyro offload failure, attempting local control.");
                }
                IMyRemoteControl RC = getRC();
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
            }
            catch (Exception ex)
            {
                throw new LandingException("Gyro processing exception: " + ex);
            }
            
        }
        public void damageCheck()
        {
            foreach (IMyTerminalBlock block in selfgridgiveashit) { if (getHealth(block) <= 0.75f) { throw new LandingException(string.Format("Damage to {0}, {1}", block.BlockDefinition.TypeIdString.Split('_')[1], block.DisplayNameText)); } } 
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
        }
        public void engageReserves()
        {
            //Allow fuel to be pulled from reserve tanks
            var tanks = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(tanks, tank => (tank.DisplayNameText.Contains("H2") || tank.DisplayNameText.Contains("Hydrogen")) && tank.DisplayNameText.Contains("Reserve"));
            foreach (IMyGasTank tank in tanks) { tank.Stockpile = false; }
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
        public void land()
        {
            resetThrust();
            correctOrient();
            //TODO:  the rest
        }
        public void chutechutechute()
        {
            //oh god oh fuck
            try { correctOrient(); }
            catch { Echo("WARN:  automatic orientation correction failure!"); } //parachute and gravity will probably fix this anyway
            try { resetThrust(); }
            catch
            {
                Echo("WARN:  automatic thruster reset failure!"); //uh oh
                Echo("INFO:  attempting to override...");
                try
                {
                    IMyRemoteControl RC = getRC();
                    RC.DampenersOverride = false;
                    var targets = new List<IMyThrust>();
                    GridTerminalSystem.GetBlocksOfType<IMyThrust>(targets);
                    foreach (IMyThrust target in targets) { target.Enabled = false; }
                }
                catch
                {
                    Echo("CRITICAL:  override of thrusters failed!"); //fuck
                    Echo("INFO:  attempting to disengage fuel system...");
                    try
                    {
                        var targets = new List<IMyGasTank>();
                        GridTerminalSystem.GetBlocksOfType<IMyGasTank>(targets);
                        foreach (IMyGasTank target in targets) { target.Stockpile = true; }
                        var targets2 = new List<IMyGasGenerator>();
                        GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(targets2);
                        foreach (IMyGasGenerator target in targets2) { target.Enabled = false; }
                    }
                    catch
                    {
                        Echo("CRITICAL:  unable to disengage fuel system!"); //holy shit
                    }
                    
                }
                
            }
            try
            {
                var chutes = new List<IMyParachute>();
                GridTerminalSystem.GetBlocksOfType<IMyParachute>(chutes);
                foreach (IMyParachute chute in chutes) { chute.OpenDoor(); }
            }
            catch { Echo("CRITICAL:  chute failure!"); } //F
            
        }
        public void checkProx()
        {
            var sensors = new List<IMySensorBlock>();
            int forwardsensorindex = 0;
            GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors);
            for (int i = 0; i <= sensors.Count(); i++) { if (sensors[i].Orientation.ToString() == "Forward"){ forwardsensorindex = i; } }
            //DEBUG:
            Echo("Forward sensor OwnerId: " + sensors[forwardsensorindex].OwnerId.ToString());
            Echo("Forward sensor last detected EntityId: " + sensors[forwardsensorindex].LastDetectedEntity.EntityId.ToString());
            if (sensors[forwardsensorindex].LastDetectedEntity.EntityId == sensors[forwardsensorindex].OwnerId)
            {
                Vector3D opos = sensors[forwardsensorindex].LastDetectedEntity.Position;
                Vector3D spos = sensors[forwardsensorindex].Position;
                if (Vector3D.Distance(opos, spos) <= minDist)
                {
                    return;
                }
                else if (Vector3D.Distance(opos, spos) > minDist)
                {
                    return;
                }
            }
        }
    }
}