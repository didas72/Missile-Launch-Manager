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
        
        //Name of the LCD to be used:
        const string LCD_Name = "Missile Launch Manager LCD";
        //Time between each missile launch:
        const float Time_Between_Launches = 1.0f;
        //Wether or not to arm warheads before launch
        const bool Arm_Warheads = true;
		

        //Missile
        const string Missile_Control_Program_Name = "Missile Guidance";

        //Flight
        const float Flight_Cruise_Altitude = 1000.0f;
        
        //Code ===== DON'T CHANGE ANYTHING BELOW

        List<Target> targets = new List<Target>();
        List<Missile> missiles = new List<Missile>();
        bool firing = false;
        
        Mode _mode = Mode.None;

        float timeSinceLastLaunch = 0f;

        IMyTextSurface LCD = null;


        public Program()
        {
            LCD = GridTerminalSystem.GetBlockWithName(LCD_Name) as IMyTextSurface;
            _mode = Mode.Multiple;
        }

        public void Main(string argument, UpdateType updateSource)
        {
        	CheckForMissiles();
        	
			ProcessArguments(argument);

            UI();
        }

        public void UI()
        {
			//show:
			//MLM v2.0 by Didas72
			//Mode: MM   Firing: No   Targets: 3   Missiles: 6
			//Left column, list of targets
			//Right column, Visual representation of missiles with position
			
            if (LCD != null)
            {
				//build image here
            }
            else
            {
                Echo("Warning - No LCD panel.");
            }
        }
        
        public void CheckForMissiles()
        {
        	
        }
        
        public void ProcessArguments(string arg)
        {
        	string instruction = arg.Split(' ')[0].ToLowerInvariant();
        	string[] arguments = arg.Split(' ').Skip(0).ToArray();
        	
        	switch(instruction)
        	{
        		case "fire":
        		
        			if (targets.Count != 0)
        			{
        				
        				throw new Exception("Not Implemented");
        				
        				//send missile
        			}
        			
        			break;
        			
        		//add queuing
        		
        		case "add":
        			
        			Target tgt;
        			if (GetTarget(arguments[0], out tgt))
        			{
        				targets.Add(tgt);
        			}
        			else
        			{
        				Echo("Unable to parse GPS point!");
        			}
        			
        			break;
        	}
        }
        
        public bool GetTarget(string gps, out Target tgt)
        {
        	string name = gps.Split(':')[0];
        	double x, y, z;
        	if (double.TryParse(gps.Split(':')[1], x) &&
        		double.TryParse(gps.Split(':')[2], y) &&
        		double.TryParse(gps.Split(':')[3], z))
        	{
        		tgt = new Target(name, x, y, z);
        		return true;
        	}
        	else
        	{
        		tgt = null;
        		return false;
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
        
        public enum Mode
        {
        	Error = -1,
        	None = 0,
        	Multiple, //Select multiple targets and missiles to fire
        }
    }
}
