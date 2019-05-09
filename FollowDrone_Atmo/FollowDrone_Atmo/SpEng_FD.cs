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
		public int minAlt = 20;
		public int minUr = 1;
		public int divergenceThreshold = 20; //how much valuesold and values can diverge negatively
		
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
				checkStat();
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
			//Check core blocks for damage
			List<string> coretypes = new List<string>("IMyBatteryBlock", "IMyReactor", "IMyGasTank")
			//It's probably not block.health
			foreach(string type in coretypes) 
			{ 
				foreach(type block in selfgridgiveashit)
				{
					if(block.health!=100)
					{
						throw new LandingException(string.Format("Damage to {0}, {1}",type, block.Name));
					}
				}
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