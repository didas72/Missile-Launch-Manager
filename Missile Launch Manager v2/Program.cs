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
        //Settings
        const string LCD_Name = "Missile Launch Manager LCD";

        //Missile
        const string Missile_Control_Program_Name = "Missile Guidance";

        //Flight
        const float Flight_Cruise_Altitude = 1000.0f;

        //Launch
        const float Time_Between_Launches = 1.0f;

        //Code ===== DON'T CHANGE ANYTHING BELOW

        Queue<Target> targets = new Queue<Target>();
        Queue<Missile> missiles = new Queue<Missile>();
        bool firing = false;

        float timeSinceLastLaunch = 0f;

        IMyTextSurface LCD = null;


        public Program()
        {
            LCD = GridTerminalSystem.GetBlockWithName(LCD_Name) as IMyTextSurface;
        }

        public void Main(string argument, UpdateType updateSource)
        {
        	//check for new missiles
        	
			ProcessArguments(argument);

            UI();
        }

        public void UI()
        {
            if (LCD != null)
            {
            	string display = string.Empty;
            	
            	display += $"Firing: {firing}\n";

                foreach (Target t in targets.ToArray())
                {
                    display += $"{t.name}: {Math.Round(t.x)}; {Math.Round(t.y)}; {Math.Round(t.z)}\n";
                }
                
                LCD.WriteText(display);
            }
            else
            {
                Echo("Warning - No LCD panel.");
            }
        }
        
        public void ProcessArguments(string arg)
        {
        	switch(arg.ToLowerInvariant())
        	{
        		case "Fire":
        		
        			if (targets.Count != 0)
        			{
        				Target tgt = targets.Dequeue();
        				Missile msl = missiles.Dequeue();
        				
        				throw new Exception("Not Implemented");
        				
        				//send missile
        			}
        			
        			break;
        			
        		//add queuing
        		//add add salvo launch
        		//add launch until out of missiles/targets
        	}
        }

        public struct Target
        {
            public string name;
            public double x;
            public double y;
            public double z;

            public Target(string n, double X, double Y, double Z)
            {
                name = n; x = X; y = Y; z = Z;
            }
        }
        
        public struct Missile
        {
        	public IMyProgrammableBlock control;
        	public bool hasWarheads;
        	public List<IMyWarhead> warheads;
        	public bool hasSensors;
        	public List<IMySensor> sensors;
        	
        	public Missile(IMyProgrammableBlock control)
        	{
        		this.control = control;
        		hasWarheads = false; hasSensors = false;
        	}
        	
        	public Missile(IMyProgrammableBlock control, List<IMyWarhead> warheads)
        	{
        		this.control = control;
        		this.warheads = warheads;
        		hasWarheads = true; hasSensors = false;
        	}
        	
        	public Missile(IMyProgrammableBlock control, List<IMySensor> sensors)
        	{
        		this.control = control;
        		this.sensors = sensors;
        		hasWarheads = false; hasSensors = true;
        	}
        	
        	public Missile(IMyProgrammableBlock control, List<IMyWarhead> warheads, List<IMySensor> sensors)
        	{
        		this.control = control;
        		this.warheads = warheads;
        		this.sensors = sensors;
        		hasWarheads = true; hasSensors = true;
        	}
        }
    }
}
