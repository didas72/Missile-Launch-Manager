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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.CompilerServices;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
    	#region settings
        //===== Settings =====//

        //You may change any of the values below.
        //Values that contain words must be enclosed in ", for example "Mike"
        //Values that represent numbers must end with f, for example 2.5f
        //Values that represent true or false must NOT be written with capitals.
        //Variables that take more than one value must be enclosed in brackets.

        //In order to not mess up, just replace the values and don't touch anything else.
        //If you do break something simply re-import the script into the programmable block.


        //Name of the LCD to be used:
        string LCD_Name = "Missile Launch Manager LCD";
        //Time between each missile launch:
        float Time_Between_Launches = 1.0f;
        //Wether or not to arm warheads before launch
        bool Arm_Warheads = true;
		

        //Missile Naming Tags:
        string[] Missile_Name_Tags = new string[] { "Missile Script Guidance [Set]", "Missile 1", "Missile 2", "Missile 3", "Missile 4" };

        //Missile Detach Port Type: 0 = Merge Block; 1 = Rotor; 2 = Connector; 3 = Merge Block And Any Locked Connectors, 4 = Rotor And Any Locked Connectors, 99 = No detach required
        int Missile_Detach_Port_Type = 0;

        //Max Grid Speed (if you use speed mods):
        float Max_Grid_Speed = 104.37f;

        //Flight Cruise Altitude:
        float Flight_Cruise_Altitude = 1000.0f;
        
        //===== Code =====//
        //DON'T CHANGE ANYTHING BELOW
        #endregion

    	#region variables
    	List<Target> targets = new List<Target>();
        List<Missile> missiles = new List<Missile>();
        bool firing = false;
        Queue<MissileLaunch> queuedMissiles = new Queue<MissileLaunch>();

        List<Target> selectedTargets = new List<Target>();
        List<Missile> selectedMissiles = new List<Missile>();

        Vector2Int cursor = new Vector2Int(0, 0);
        
        Mode _mode = Mode.None;

        float timeSinceLastLaunch = 0f;

        IMyTextSurface LCD = null;

        string error = string.Empty;
        string log = string.Empty;
        #endregion

        #region default methods
        public Program()
        {
            if (!string.IsNullOrEmpty(Storage))
            {
                LoadFromStorage();
            }

            LCD = GridTerminalSystem.GetBlockWithName(LCD_Name) as IMyTextSurface;
            _mode = Mode.Multiple;

            CheckForMissiles();
            UI();
        }

        public void Main(string argument, UpdateType updateSource)
        {
        	CheckForMissiles();

            ProcessLaunches();
        	
            if (!string.IsNullOrEmpty(argument))
			    ProcessArguments(argument);

            UI();
        }

        public void Save()
        {
            string str = string.Empty;

            foreach (Target tgt in targets)
            {
                str += $"{tgt.ToString()}\n";
            }

            Storage = str;
        }//add save settings
        #endregion

        #region UI methods
        public void UI()
        {
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
                Error("Warning - No LCD panel.");
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
            };//mode

            frame.Add(sprite);

            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = $"Firing: {firing}",
                Position = new Vector2(viewPort.Width, 0),
                RotationOrScale = 1f,
                Color = Color.Green,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            };//firing

            frame.Add(sprite);

            sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "SquareSimple",
                Position = new Vector2(viewPort.Width / 2f, 30),
                Size = new Vector2(viewPort.Width, 2),
                Color = Color.Green,
                Alignment = TextAlignment.CENTER,
            };//separator

            frame.Add(sprite);

            for (int i = 0; i < targets.Count; i++)
            {
                sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = targets[i].name,
                    Position = new Vector2(5, i * 20 + 30),
                    RotationOrScale = 1f,
                    Color = Color.Green,
                    Alignment = TextAlignment.LEFT,
                    FontId = "White"
                };

                frame.Add(sprite);

                if (selectedTargets.Contains(targets[i]))
                {
                    sprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "Triangle",
                        Position = new Vector2(10, i * 20 + 35),
                        Size = new Vector2(10, 10),
                        Color = Color.Red,
                        Alignment = TextAlignment.RIGHT,
                    };

                    frame.Add(sprite);
                }

                if (cursor.x == 0 && cursor.y == i)
                {
                    sprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "Triangle",
                        Position = new Vector2(10, i * 20 + 35),
                        Size = new Vector2(10, 10),
                        Color = Color.LightBlue,
                        Alignment = TextAlignment.RIGHT,
                    };

                    frame.Add(sprite);
                }
            }//targets

            for (int i = 0; i < missiles.Count; i++)
            {
                sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = missiles[i].name,
                    Position = new Vector2(viewPort.Width - 5, i * 20 + 30),
                    Color = Color.Green,
                    RotationOrScale = 0.8f,
                    Alignment = TextAlignment.RIGHT
                };

                frame.Add(sprite);

                if (selectedMissiles.Contains(missiles[i]))
                {
                    sprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "Triangle",
                        Position = new Vector2(viewPort.Width - 10, i * 20 + 35),
                        Size = new Vector2(10, 10),
                        Color = Color.Red,
                        Alignment = TextAlignment.LEFT,
                    };

                    frame.Add(sprite);
                }

                if (cursor.x == 1 && cursor.y == i)
                {
                    sprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "Triangle",
                        Position = new Vector2(viewPort.Width - 10, i * 20 + 35),
                        Size = new Vector2(10, 10),
                        Color = Color.LightBlue,
                        Alignment = TextAlignment.LEFT,
                    };

                    frame.Add(sprite);
                }
            }//missiles

            if (!string.IsNullOrEmpty(error))
            {
                sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = error,
                    Position = viewPort.Center,
                    Color = Color.Red,
                    RotationOrScale = 0.8f,
                    Alignment = TextAlignment.CENTER
                };

                frame.Add(sprite);

                error = string.Empty;
            }

            if (!string.IsNullOrEmpty(log))
            {
                sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = log,
                    Position = viewPort.Center,
                    Color = Color.Yellow,
                    RotationOrScale = 0.8f,
                    Alignment = TextAlignment.CENTER
                };

                frame.Add(sprite);

                log = string.Empty;
            }

            frame.Dispose();
        }
        #endregion

        #region logic methods
        public void ProcessArguments(string arg)
        {
            arg += " END";
        	string instruction = arg.Split(' ')[0].ToLowerInvariant();
        	string[] arguments = arg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
            if (arguments != null && arguments.Length == 1)
            {
                if (arguments[0] == "END")
                {
                    arguments = null;
                }
                else
                {
                    arguments = arguments.Except(new List<string> { "END" }).ToArray();
                }
            }
            else
            {
                arguments = arguments.Except(new List<string> { "END" }).ToArray();
            }

        	switch(instruction)
        	{
        		case "fire":
        		
                    StartLaunches();
        		    
        			break;
        		
        		case "add":
        			
        			Target tgt;
        			if (GetTarget(arguments[0], out tgt))
        			{
                        if (!targets.Contains(tgt))
                            targets.Add(tgt);
                        else
                        {
                            Error("Target already exists.");
                        }
        			}
        			else
        			{
        				Error("Unable to parse GPS point!");
        			}
        			
        			break;

                case "select":

                    if (arguments == null || arguments.Length < 1)
                    {
                        if (cursor.x == 0)
                        {
                            selectedTargets.Add(targets[cursor.y]);
                        }
                        else if (cursor.x == 1)
                        {
                            selectedMissiles.Add(missiles[cursor.y]);
                        }

                        return;
                    }
                    else
                    {
                        if (arguments[1] == "missile")
                        {
                            int i;

                            if (!int.TryParse(arguments[2], out i))
                            {
                                Error($"Couldn't convert {arguments[2]} to an integer.");
                                return;
                            }

                            if (i >= missiles.Count || i < 0)
                            {
                                Error($"{i} is outside the valied values for missile index.");
                                return;
                            }

                            selectedMissiles.Add(missiles[i]);

                            return;
                        }

                        if (arguments[1] == "target")
                        {
                            int i;

                            if (!int.TryParse(arguments[2], out i))
                            {
                                Error($"Couldn't convert {arguments[2]} to an integer.");
                                return;
                            }

                            if (i >= targets.Count || i < 0)
                            {
                                Error($"{i} is outside the valied values for target index.");
                                return;
                            }

                            selectedTargets.Add(targets[i]);
                        }
                    }

                    break;

                case "unselect":

                    if (arguments == null || arguments.Length < 1)
                    {
                        if (cursor.x == 0)
                        {
                            selectedTargets.Remove(targets[cursor.y]);
                        }
                        else if (cursor.x == 1)
                        {
                            selectedMissiles.Remove(missiles[cursor.y]);
                        }

                        return;
                    }
                    else
                    {
                        if (arguments[1] == "missile")
                        {
                            int i;

                            if (!int.TryParse(arguments[2], out i))
                            {
                                Error($"Couldn't convert {arguments[2]} to an integer.");
                                return;
                            }

                            if (i >= missiles.Count || i < 0)
                            {
                                Error($"{i} is outside the valied values for missile index.");
                                return;
                            }

                            selectedMissiles.Remove(missiles[i]);

                            return;
                        }

                        if (arguments[1] == "target")
                        {
                            int i;

                            if (!int.TryParse(arguments[2], out i))
                            {
                                Error($"Couldn't convert {arguments[2]} to an integer.");
                                return;
                            }

                            if (i >= targets.Count || i < 0)
                            {
                                Error($"{i} is outside the valied values for target index.");
                                return;
                            }

                            selectedTargets.Remove(targets[i]);
                        }
                    }

                    break;

                case "abort":

                    Error("LAUNCHES ABORTED.");
                    firing = false;

                    break;

                case "update":

                    CheckForMissiles();

                    break;

                case "save":

                    Save();

                    break;

                #region cursor movement
                case "up":

                    cursor.y--;

                    CheckCursorOutOfBounds();

                    break;

                case "down":

                    cursor.y++;

                    CheckCursorOutOfBounds();

                    break;

                case "left":

                    cursor.x--;

                    if (cursor.x < 0)
                    {
                        cursor.x = 1;
                        if (cursor.y >= missiles.Count - 1)
                        {
                            cursor.y = Math.Max(missiles.Count - 1, 0);
                        }
                    }
                    else
                    {
                        if (cursor.y >= targets.Count - 1)
                        {
                            cursor.y = Math.Max(targets.Count - 1, 0);
                        }
                    }

                    CheckCursorOutOfBounds();

                    break;

                case "right":

                    cursor.x++;

                    if (cursor.x > 1)
                    {
                        cursor.x = 0;
                        if (cursor.y >= targets.Count - 1)
                        {
                            cursor.y = Math.Max(targets.Count - 1, 0);
                        }
                    }
                    else
                    {
                        if (cursor.y >= missiles.Count - 1)
                        {
                            cursor.y = Math.Max(missiles.Count - 1, 0);
                        }
                    }

                    CheckCursorOutOfBounds();

                    break;
                #endregion
            }
        }

        public void ProcessLaunches()
        {
            if (!firing)
                return;

            if (queuedMissiles.Count == 0)
            {
                firing = false;
                Runtime.UpdateFrequency = UpdateFrequency.None;
                Log("Launches completed!");
                return;
            }

            timeSinceLastLaunch += (float)Runtime.TimeSinceLastRun.TotalSeconds;

            if (timeSinceLastLaunch < Time_Between_Launches)
                return;

            MissileLaunch launch = queuedMissiles.Dequeue();

            FireMissile(launch.target, launch.missile);

            missiles.Remove(launch.missile);

            timeSinceLastLaunch = 0f;
        }

        public void StartLaunches()
        {
            if (selectedMissiles.Count == 0 || selectedTargets.Count == 0)
            {
                Error("No missiles/targets selected!");
                return;
            }

            if (selectedTargets.Count > selectedMissiles.Count)
            {
                Error("Not enough missiles selected. Must be at least 1 per target.");
            }

            firing = true;
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            for (int m = 0, t = 0; m < selectedMissiles.Count; m++)
            {
                if (t >= selectedTargets.Count)
                    t = 0;

                QueueMissileLaunch(selectedTargets[t], selectedMissiles[m]);

                t++;
            }

            selectedMissiles.Clear();
            selectedTargets.Clear();
        }
        
        public bool GetTarget(string gps, out Target tgt)
        {
            if (gps.Split(':').Length < 5)
            {
                tgt = new Target();
                return false;
            }

            string name = gps.Split(':')[1];//0 = GPS
            
        	double x, y, z;
        	if (double.TryParse(gps.Split(':')[2], out x) &&
        		double.TryParse(gps.Split(':')[3], out y) &&
        		double.TryParse(gps.Split(':')[4], out z))
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

        public void CheckForMissiles()
        {
            missiles.Clear();

            List<IMyProgrammableBlock> allPrograms = new List<IMyProgrammableBlock>();

            GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(allPrograms);

            foreach (IMyProgrammableBlock control in allPrograms)
            {
                if (Missile_Name_Tags.Any(t => control.DisplayNameText.Contains(t)))
                {
                    Missile msl = new Missile(control.DisplayNameText, control);
                    missiles.Add(msl);
                }
            }
        }

        public void QueueMissileLaunch(Target tgt, Missile msl)
        {
            queuedMissiles.Enqueue(new MissileLaunch(tgt, msl));
        }

        public void FireMissile(Target tgt, Missile msl)
        {
            string dt = string.Empty;

            dt += $"missileDetachPortType={Missile_Detach_Port_Type}";

            msl.control.CustomData = dt;

            msl.control.TryRun(tgt.ToString());
        }

        public void CheckCursorOutOfBounds()
        {
            if (cursor.x == 0)
            {
                if (cursor.y < 0)
                    cursor.y = targets.Count - 1;
                else if (cursor.y >= targets.Count)
                    cursor.y = 0;
            }
            else if (cursor.x == 1)
            {
                if (cursor.y < 0)
                    cursor.y = missiles.Count - 1;
                else if (cursor.y >= missiles.Count)
                    cursor.y = 0;
            }
        }

        public void LoadFromStorage()
        {
            string[] tgts = Storage.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach(string str in tgts)
            {
                Target tgt;

                GetTarget(str, out tgt);

                targets.Add(tgt);
            }
        }//add load settings

        public void Error(string err)
        {
            Echo(err);
            if (!string.IsNullOrEmpty(err))
                error += "\n" + err;
            else
                error += err;
        }

        public void Log(string info)
        {
            Echo(info);
            if (!string.IsNullOrEmpty(log))
                log += "\n" + info;
            else
                log += info;
        }
        #endregion

        #region structs
        public struct MissileLaunch
        {
            public Target target;
            public Missile missile;

            public MissileLaunch(Target tgt, Missile msl)
            {
                target = tgt; missile = msl;
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

            public override string ToString()
            {
                return $"GPS:{name}:{x}:{y}:{z}";
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

        public struct Vector2Int
        {
            public int x;
            public int y;

            public Vector2Int(int X, int Y) { x = X; y = Y; }
        }

        #endregion

        #region enums
        public enum Mode
        {
        	Error = -1,
        	None = 0,
        	Multiple, //Select multiple targets and missiles to fire
        }
        #endregion
    }
}
