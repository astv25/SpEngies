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
		public List<string> careabout = new List<string>("h2","U","battery");
		public Dictionary<string, bool> alerts = new Dictionary<string, bool>();
		public Dictionary<string, float> values = new Dictionary<string, float>();
		public Dictionary<string, float> valuesold = new Dictionary<string, float>();
		public const string namebase = "FD001-" //naming prefix for blocks we care about (e.g.: FD001-Reactor 1)
		public List<IMyTerminalBlock> selfgrid = new List<IMyTerminalBlock>();
		public List<IMyTerminalBlock> selfgridgiveashit = new List<IMyTerminalBlock>();
		public List<string> wantedtypes = new List<string>("IMyGasTank", "IMyGyro", "IMyBatteryBlock", "IMyReactor", "IMyBlockGroup", "IMyRadioAntenna", "IMyRemoteControl", "IMyThrust", "IMyLandingGear", "IMySensorBlock");
		public List<string> coretypes = new List<string>("IMyBatteryBlock", "IMyReactor", "IMyGasTank")
		public List<string> secondaryTypes = new List<string>("IMyRadioAntenna", "IMyGyro", "IMySensorBlock", "IMyThrust", "IMyLandingGear", "IMyRemoteControl");
		public int minAlt = 20;
		public int minUr = 1;
		public int divergenceThreshold = 20; //how much valuesold and values can diverge negatively
		public const double CTRL_COEFF = 0.3; //defines the strength of Pitch/Roll/Yaw correction
		
		public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }		
		
		public void Main(string argument, UpdateType, updateSource) 
		{
			//LIST handling:
			initList();
			try 
			{
				checkCore();
				damageCheck(coreTypes);
				damageCheck(secondaryTypes);
				checkPos();
				checkProx();
			}
			catch (LandingException ex) 
			{
				Echo(ex);
				land();
			}
			catch (Exception ex) 
			{
				Echo("Unknown error, landing!")
				land();
			}
				
		}
		public void initList() 
		{
			//Init alerts, values, valuesold dictionaries based on data in careabout
			if(alerts.Count != careabout.Length && values.Count != careabout.Length && valuesold.Count != careabout.Length)
			{
				for(int index=0; index<careabout.Length;index++)
				{
					alerts.Add(careabout[index], false);
					values.Add(careabout[index], 1.0);
					valuesold.Add(careabout[index], 1.0);
				}
			}
			//Init block lists
			if(selfgrid.Length == 0)
			{
				selfgrid = GridTerminalSystem.GetBlocks;
			}
			if(selfgridgiveashit.Length == 0)
			{
				foreach (IMyTerminalBlock block in selfgrid) 
				{
					if (block.type in wantedtypes && block.Name.contains(namebase))
					{
						selfgridgiveashit.add(block);
					}
				}
			}

		}
		public class LandingException : Exception {public LandingException(string cause):base(string.Format("Landing immediately.  Cause: {0}", cause){}}
		public void checkCore(){
			//Store previous values
			foreach(KeyValuePair<string,float> kvp in values)
			{
				valuesold[kvp] = values[kvp].Value;
			}
			//Get H2 tank fill average
			float tgas = 0.0;
			int tgascnt = 0;
			foreach (IMyGasTank tank in selfgridgiveashit) 
			{
				if(!tank.Name.contains("Reserve")
				{
					tgas += (float)tank.FilledRatio*100;
					tgascnt++;
				}
			}
			tgas=tgas/tgascnt; //Average out total fill %
			values[careabout[0]]=tgas;
			if(tgas<=25)
			{
				engageReserves();
				throw new LandingException("Bingo fuel");
			}
			//Get battery charge average
			float tbchg = 0.0;
			int tbcnt = 0;
			foreach (IMyBatteryBlock batt in selfgridgiveashit) 
			{
				tbchg += batthelper(batt);
				tbcnt++;
			}
			tbchg = tbchg/tbcnt; //Average out total battery charge %
			values[caresabout[2]]=tbchg;
			if(tbchg<=25)
			{
				throw new LandingException("Low battery");
			}
			//Get Uranium ingots left in reactor(s)
			long tUr = 0;
			int tUrI = 0;
			foreach(IMyReactor react in selfgridgiveashit)
			{
				foreach(IMyInventoryItem itm in react.GetInventory(0).GetItems() 
				{
					tUr+=itm.Amount.RawValue;
				}
			}
			tUrI = Math.Round(((double)tUr / 1000000),2));
			values[caresabout[1]]=tUrI;
			if(tURI<minUr)
			{
				throw new LandingException("Uranium low");
			}
			//Compare current values to previous
			foreach(string check in careabout)
			{
				float check = valuesold[check].Value - values[check].Value;
				if(check >= 0)
				{
					if(check >= divergenceThreshold)
					{
						throw new LandingException(string.Format("Value of {0} exceeded negative change threshold",check));
					}
				}
			}
			//Send telemetry home
			sendTelemetry(values);
			return;
		}
		public void checkPos()
		{
			//Get remote control
			IMyRemoteControl RC;
			foreach(IMyRemoteControl tRC in GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(selfgridgiveashit)) 
			{
				RC = tRC as IMyRemoteControl;
			}
			//Get gyro
			IMyGyro mGy;
			foreach(IMyGyro tmGy in GridTerminalSystem.GetBlocks<IMyGyro>(selfgridgiveashit))
			{
				mGy = tmGy as IMyGyro;
			}
			//Check pitch/roll/yaw compared to gravity
			Matrix orient;
            RC.Orientation.GetMatrix(out orient);
            Vector3D down = orient.Down;
            Vector3D grav = RC.GetNaturalGravity();
            grav.Normalize();
            mGy.Orientation.GetMatrix(out orient);
            var lDown = Vector3D.Transform(down, MatrixD.Transpose(orient));
            var lGrav = Vector3D.Transform(grav, MatrixD.Transpose(mainGyro.WorldMatrix.GetOrientation()));
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
            if (ang < 0.01)
            {
                mGy.GyroOverride = false;
            }
            return;
		}
		public void damageCheck(List <string> types)
		{
			foreach(string type in types)
			{
				foreach(IMyTerminalBlock block in GridTerminalSystem.GetBlocksOfType<type>(selfgridgiveashit))
				{
					if(block.health != 100)
					{
						throw new LandingException(string.Format("Damage to {0}, {1}",type, block.Name));
					}
				}
			}
			return;
		}
		public void sendTelemetry(List <string> data)
		{
			//First, horrific antenna fuckery
			IMyRadioAntenna ant;
			long antID = 0;
			List <IMyRadioAntenna> ants = new List <IMyRadioAntenna>;
			foreach(IMyTerminalBlock block in selfgridgiveashit)
			{
				if(block.Type == IMyRadioAntenna)
				{
					ants += block;
				}
			}
			if(ants.Length >= 0)
			{
				foreach(IMyRadioAntenna tant in ants)
				{
					antID = tant.EntityId;
				}
			}
			try
			{
				ant = GridTerminalSystem.GetBlockwithId(antID) as IMyRadioAntenna;
			}
			catch (Exception ex)
			{
				throw new LandingException("Antenna bind failure");
			}
			//holy fuck, I hate myself for that
			string message = basename;
			foreach(string  str in careabout) 
			{
				message += " " + str + " " + values[str]; 
			}
			ant.TransmitMessage(message);
			return;
		}
		public void engageReserves()
		{
			//Allow fuel to be pulled from reserve tanks for landing
			foreach (IMyGasTank tank in selfgridgiveashit) 
			{
				if(tank.Name.contains("Reserve")
				{
					tank.Stockpile = false;
				}
			}
			return;
		}
		public float batthelper(IMyBatteryBlock battery) 
		{
			//Because batteries don't have FillRatio
			float battmax = battery.MaxStoredPower;
			float battcur = battery.CurrentStoredPower;
			float battratio = battcur / battmax
			float battper = battratio * 100;
			return battper;
		}
	}
}