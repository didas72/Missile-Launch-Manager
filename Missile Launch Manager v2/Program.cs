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
        readonly string[] Missile_Name_Tags = new string[] { "Missile Script Guidance [Set]", "Missile 1", "Missile 2", "Missile 3", "Missile 4" };

        //Flight
        const float Flight_Cruise_Altitude = 1000.0f;
        
        //Code ===== DON'T CHANGE ANYTHING BELOW

        List<Target> targets = new List<Target>();
        List<Missile> missiles = new List<Missile>();
        //bool firing = false;
        
        Mode _mode = Mode.None;

        //float timeSinceLastLaunch = 0f;

        IMyTextSurface LCD = null;


        public Program()
        {
            LCD = GridTerminalSystem.GetBlockWithName(LCD_Name) as IMyTextSurface;
            _mode = Mode.Multiple;
            Echo(_mode.ToString());

            CheckForMissiles();
            UI();
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
                LCD.ContentType = ContentType.SCRIPT;
                LCD.ScriptBackgroundColor = Color.Black;

                RectangleF viewport = new RectangleF((LCD.TextureSize - LCD.SurfaceSize) / 2f, LCD.SurfaceSize);

                var frame = LCD.DrawFrame();

                DrawSriptes(ref frame, ref viewport);
            }
            else
            {
                Echo("Warning - No LCD panel.");
            }
        }

        public void DrawSriptes(ref MySpriteDrawFrame frame, ref RectangleF viewPort)
        {
            //sprite data: SquareHollow, Triangle, CircleHollow

            MySprite sprite;

            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = $"Mode: {_mode}",
                Position = new Vector2(0, 0),
                RotationOrScale = 1f,
                Color = Color.Green,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            };

            frame.Add(sprite);

            for (int i = 0; i < targets.Count; i++)
            {
                sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = targets[i].name,
                    Position = new Vector2(0,i * 20 + 30),
                    RotationOrScale = 1f,
                    Color = Color.Green,
                    Alignment = TextAlignment.LEFT,
                    FontId = "White"
                };

                frame.Add(sprite);
            }

            Echo($"Missiles: {missiles.Count}");

            for (int i = 0; i < missiles.Count; i++)
            {
                sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareHollow",
                    Position = new Vector2(viewPort.Width / 1.5f, i * 32 + 45),
                    Size = new Vector2(200, 30),
                    Color = Color.Green,
                    Alignment = TextAlignment.CENTER
                };

                frame.Add(sprite);

                sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = missiles[i].name,
                    Position = new Vector2(viewPort.Width / 1.5f, i * 32 + 40),
                    Color = Color.Green,
                    RotationOrScale = 0.6f,
                    Alignment = TextAlignment.CENTER
                };

                frame.Add(sprite);
            }

            frame.Dispose();
        }
        
        public void CheckForMissiles()
        {
            missiles.Clear();

            List<IMyProgrammableBlock> allPrograms = new List<IMyProgrammableBlock>();

            GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(allPrograms);

            foreach(IMyProgrammableBlock control in allPrograms)
            {
                Echo("Loop");
                if (Missile_Name_Tags.Any(t => control.DisplayNameText.Contains(t)))
                {
                    Echo("Found");
                    Missile msl = new Missile(control.DisplayNameText, control);
                    missiles.Add(msl);
                }
            }
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
        	if (double.TryParse(gps.Split(':')[1], out x) &&
        		double.TryParse(gps.Split(':')[2], out y) &&
        		double.TryParse(gps.Split(':')[3], out z))
        	{
        		tgt = new Target(name, x, y, z);
        		return true;
        	}
        	else
        	{
        		tgt = new Target();
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
            public string name;
        	public IMyProgrammableBlock control;
        	public bool hasWarheads;
        	public List<IMyWarhead> warheads;
        	public bool hasSensors;
        	public List<IMySensorBlock> sensors;
        	
        	public Missile(string name, IMyProgrammableBlock control)
        	{
                this.name = name;
        		this.control = control;
        		hasWarheads = false; hasSensors = false;
                warheads = null; sensors = null;
        	}
        	
        	public Missile(string name, IMyProgrammableBlock control, List<IMyWarhead> warheads)
            {
                this.name = name;
                this.control = control;
        		this.warheads = warheads;
        		hasWarheads = true; hasSensors = false;
                sensors = null;
            }
        	
        	public Missile(string name, IMyProgrammableBlock control, List<IMySensorBlock> sensors)
            {
                this.name = name;
                this.control = control;
        		this.sensors = sensors;
        		hasWarheads = false; hasSensors = true;
                warheads = null;
        	}
        	
        	public Missile(string name, IMyProgrammableBlock control, List<IMyWarhead> warheads, List<IMySensorBlock> sensors)
            {
                this.name = name;
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
