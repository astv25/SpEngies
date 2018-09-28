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
        public const string nameBase = "FD";
        public bool lowBatt = false;
        public bool bingoFuel = false;
        public bool lowFiss = false;
        public bool lowIce = false;
        public bool isLanded = false;
        public bool testLand = false;
        public float liftThrust = 0.25f;
        public float descThrust = 0.15f;
        public int minAlt = 15;
        public const double CTRL_COEFF = 0.3;
        #endregion
        public IMyGyro mainGyro;
        public IMyThrust mainThrustUp;
        public IMyThrust mainThrustDown;
        public IMyThrust mainThrustLeft;
        public IMyThrust mainThrustRight;
        public IMyThrust mainThrustForw;
        public IMyThrust mainThrustBack;
        public IMyRemoteControl RC;
        public IMySensorBlock forwardSensor;
        public IMySensorBlock leftSensor;
        public IMySensorBlock rightSensor;
        public IMySensorBlock rearSensor;
        public IMySensorBlock topSensor;
        public IMySensorBlock bottomSensor;
        public IMyBatteryBlock mainBattery;
        public IMyReactor mainReactor;
        public IMyGasGenerator h2gen;
        public IMyGasTank h2stor;
        public IMyRadioAntenna ant;
        public IMyLandingGear gear1;
        public IMyLandingGear gear2;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save()
        {
            //Probably not used
        }
        
        public void Main(string argument, UpdateType updateSource)
        {
            Echo("Booting...");
            getParts();
            //Get system resource status
            List<int> status = new List<int>(4);
            status = getStatus(mainBattery, mainReactor, h2gen, h2stor);
            //Are we landed and should we remain so?
            if (testLand) { return; }
            if (isLanded)
            {
                if(lowBatt || lowFiss || lowIce || bingoFuel) { Echo("One or more resources is low!"); Me.Enabled = false; return; }
                if(!lowBatt && !lowFiss && !lowIce && !bingoFuel) { goFlight(); }
            }
            //Create container list of sensors
            List<IMySensorBlock> systemSensors = new List<IMySensorBlock>(6) { forwardSensor, leftSensor, rightSensor, rearSensor, topSensor, bottomSensor };
            Echo("Initialzing sensors...");
            initializeSensors(systemSensors);
            //Show status in antenna name
            updateAntenna(ant, status);
            //Should we land?
            if (lowBatt || lowFiss || (lowIce && bingoFuel)) { Echo("Low resource(s), landing!"); tryLanding(); }
            if (testLand) { Echo("Testing landing sequence..."); tryLanding(); }
            //Make sure we're the right way up
            checkOrientation();
            //Check altitude
            maintainAlt();
        }
        public void getParts()
        {
            try
            {
                long antID = 0;
                mainGyro = GridTerminalSystem.GetBlockWithName(nameBase + "-Gyro") as IMyGyro;
                mainThrustUp = GridTerminalSystem.GetBlockWithName(nameBase + "-H2ThrUp") as IMyThrust;
                mainThrustDown = GridTerminalSystem.GetBlockWithName(nameBase + "-H2ThrDwn") as IMyThrust;
                mainThrustForw = GridTerminalSystem.GetBlockWithName(nameBase + "-H2ThrFor") as IMyThrust;
                mainThrustBack = GridTerminalSystem.GetBlockWithName(nameBase + "-H2ThrBck") as IMyThrust;
                mainThrustLeft = GridTerminalSystem.GetBlockWithName(nameBase + "-H2ThrLft") as IMyThrust;
                mainThrustRight = GridTerminalSystem.GetBlockWithName(nameBase + "-H2ThrRgh") as IMyThrust;
                RC = GridTerminalSystem.GetBlockWithName(nameBase + "-RC") as IMyRemoteControl;
                forwardSensor = GridTerminalSystem.GetBlockWithName(nameBase + "-SensForw") as IMySensorBlock;
                leftSensor = GridTerminalSystem.GetBlockWithName(nameBase + "-SensLeft") as IMySensorBlock;
                rightSensor = GridTerminalSystem.GetBlockWithName(nameBase + "-SensRight") as IMySensorBlock;
                rearSensor = GridTerminalSystem.GetBlockWithName(nameBase + "-SensRear") as IMySensorBlock;
                topSensor = GridTerminalSystem.GetBlockWithName(nameBase + "-SensTop") as IMySensorBlock;
                bottomSensor = GridTerminalSystem.GetBlockWithName(nameBase + "-SensBott") as IMySensorBlock;
                mainBattery = GridTerminalSystem.GetBlockWithName(nameBase + "-Batt") as IMyBatteryBlock;
                mainReactor = GridTerminalSystem.GetBlockWithName(nameBase + "-Reactor") as IMyReactor;
                h2gen = GridTerminalSystem.GetBlockWithName(nameBase + "-O2/H2Gen") as IMyGasGenerator;
                h2stor = GridTerminalSystem.GetBlockWithName(nameBase + "-H2Tank") as IMyGasTank;
                gear1 = GridTerminalSystem.GetBlockWithName(nameBase + "-Gear 1") as IMyLandingGear;
                gear2 = GridTerminalSystem.GetBlockWithName(nameBase + "-Gear 2") as IMyLandingGear;
                //Get antenna regardless of name
                List<IMyRadioAntenna> blocks = new List<IMyRadioAntenna>();
                GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(blocks);
                foreach (IMyRadioAntenna item in blocks)
                {
                    antID = item.EntityId;
                }
                ant = GridTerminalSystem.GetBlockWithId(antID) as IMyRadioAntenna;
            }
            catch (Exception e)
            {
                Echo(e.ToString());
                Me.Enabled = false;
            }
        }

        public void initializeSensors(List<IMySensorBlock>sensors)
        {
            foreach (IMySensorBlock block in sensors)
            {
                block.BackExtend = 0;
                block.FrontExtend = 10;
                block.BottomExtend = 5;
                block.TopExtend = 5;
                block.LeftExtend = 2.5f;
                block.RightExtend = 2.5f;
                block.DetectSubgrids = false;
                block.DetectStations = false;
                block.DetectSmallShips = false;
                block.DetectPlayers = true; //Does this override DetectOwner?
                block.DetectOwner = true;
                block.DetectNeutral = false;
                block.DetectLargeShips = false;
                block.DetectFriendly = false;
                block.DetectFloatingObjects = false;
                block.DetectEnemy = false;
                block.DetectAsteroids = false;
                block.Enabled = true;
            }
            Echo("Sensor initialization complete.");
        }
        public List<int> getStatus(IMyBatteryBlock batt,IMyReactor react,IMyGasGenerator gasgen,IMyGasTank tank)
        {
            int battper = (int)((batt.CurrentStoredPower / batt.MaxStoredPower) * 100);
            int uranium = 0;
            foreach (IMyInventoryItem item in react.GetInventory(0).GetItems())
            {
                uranium += (int)(item.Amount.RawValue / 1000000);
            }
            int ice = 0;
            foreach (IMyInventoryItem item in gasgen.GetInventory(0).GetItems())
            {
                ice += (int)(item.Amount.RawValue / 1000000);
            }
            int gas = (int)(tank.FilledRatio * 100);
            if (battper <= 10) { lowBatt = true; }else if (battper > 10) { lowBatt = false; }
            if (uranium <= 1) { lowFiss = true; }else if (uranium > 1) { lowFiss = false; }
            if (ice <= 1000) { lowIce = true; }else if (ice > 1000) { lowIce = false; }
            if (gas <= 25) { bingoFuel = true; }else if (gas > 10) { bingoFuel = false; }
            List<int> output = new List<int>(4) { battper, uranium, ice, gas };
            Echo(String.Format("Battery : {0}%", output[0]));
            Echo(String.Format("Uranium : {0}kg", output[1]));
            Echo(String.Format("Ice     : {0}kg", output[2]));
            Echo(String.Format("Hydrogen: {0}%", output[3]));
            return output;
        }
        public void updateAntenna(IMyRadioAntenna ant,List<int>stat)
        {
            //Reset customname
            ant.CustomName = nameBase;
            //Check for low batt/fuel/uranium/ice status
            if (lowBatt) { ant.CustomName += " Batt:Low"; } else { ant.CustomName += " Batt:" + stat[0] + "%"; }
            if (bingoFuel) { ant.CustomName += " Fuel:Low"; } else { ant.CustomName += " Fuel:" + stat[3] + "%"; }
            if (lowFiss) { ant.CustomName += " Ur:Low"; } else { ant.CustomName += " Ur:" + stat[1] + "kg"; }
            if (lowIce) { ant.CustomName += " Ice:Low"; } else { ant.CustomName += " Ice:" + stat[2] + "kg"; }
        }
        public void tryLanding()
        {
            checkOrientation();
            gear1.AutoLock = true;
            gear2.AutoLock = true;
            if (!gear1.IsLocked && !gear2.IsLocked)
            {
                if (RC.GetShipSpeed() < 5)
                {
                    mainThrustDown.ThrustOverridePercentage = descThrust;
                }
                if (RC.GetShipSpeed() > 5)
                {
                    mainThrustDown.ThrustOverride = 0;
                }
                
            }
            if (gear1.IsLocked || gear2.IsLocked)
            {
                //sanity check
                mainThrustDown.ThrustOverride = 0;
                enableStandby();
                isLanded = true;
                return;
            }
        }
        public void checkOrientation()
        {
            Matrix orient;
            RC.Orientation.GetMatrix(out orient);
            Vector3D down = orient.Down;
            Vector3D grav = RC.GetNaturalGravity();
            grav.Normalize();
            mainGyro.Orientation.GetMatrix(out orient);
            var lDown = Vector3D.Transform(down, MatrixD.Transpose(orient));
            var lGrav = Vector3D.Transform(grav, MatrixD.Transpose(mainGyro.WorldMatrix.GetOrientation()));
            var rot = Vector3D.Cross(lDown, lGrav);
            double ang = rot.Length();
            ang = Math.Atan2(ang, Math.Sqrt(Math.Max(00, 1.0 - ang * ang)));
            if (ang > 0.01)
            {
                double ctrl_vel = mainGyro.GetMaximum<float>("Yaw") * (ang / Math.PI) * CTRL_COEFF;
                ctrl_vel = Math.Min(mainGyro.GetMaximum<float>("Yaw"), ctrl_vel);
                ctrl_vel = Math.Max(0.01, ctrl_vel);
                rot.Normalize();
                rot *= ctrl_vel;
                mainGyro.SetValueFloat("Pitch", (float)rot.GetDim(0));
                mainGyro.SetValueFloat("Yaw", -(float)rot.GetDim(1));
                mainGyro.SetValueFloat("Roll", -(float)rot.GetDim(2));
                mainGyro.SetValueFloat("Power", 1.0f);
                mainGyro.GyroOverride = true;
            }
            if (ang < 0.01)
            {
                mainGyro.GyroOverride = false;
            }
            return;
        }
        public void enableStandby()
        {
            //Disable thrusters
            List<IMyThrust> thrusters = new List<IMyThrust> { mainThrustBack, mainThrustDown, mainThrustForw, mainThrustLeft, mainThrustRight, mainThrustUp };
            foreach (IMyThrust a in thrusters) { a.ThrustOverride = 0;a.Enabled = false; }
            //Disable sensors
            List<IMySensorBlock> systemSensors = new List<IMySensorBlock>(6) { forwardSensor, leftSensor, rightSensor, rearSensor, topSensor, bottomSensor };
            foreach (IMySensorBlock a in systemSensors) { a.Enabled = false; }
            //Set H2 tank to stockpile
            h2stor.Stockpile = true;
            
        }
        public void goFlight()
        {
            //Enable sensors
            List<IMySensorBlock> systemSensors = new List<IMySensorBlock>(6) { forwardSensor, leftSensor, rightSensor, rearSensor, topSensor, bottomSensor };
            initializeSensors(systemSensors);
            //Enable thrusters
            List<IMyThrust> thrusters = new List<IMyThrust> { mainThrustBack, mainThrustDown, mainThrustForw, mainThrustLeft, mainThrustRight, mainThrustUp };
            foreach (IMyThrust a in thrusters) { a.ThrustOverride = 0; a.Enabled = true; }
            //Set H2 tank to distribute
            h2stor.Stockpile = false;
            //Disable gear locks
            gear1.AutoLock = false;
            gear1.Unlock();
            gear2.AutoLock = false;
            gear2.Unlock();
            isLanded = false;
            //Go for minimum operating altitude
            maintainAlt();
        }
        public void maintainAlt()
        {
            double elevation;
            RC.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
            if (elevation <= minAlt)
            {
                if (RC.GetShipSpeed() < 10)
                {
                    mainThrustUp.ThrustOverridePercentage = liftThrust;
                }
                mainThrustUp.ThrustOverride = 0;
            }
            //Sanity check
            mainThrustUp.ThrustOverride = 0;
        }
    }
}