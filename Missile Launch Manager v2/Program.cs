using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;
using System;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region Settings
        //===== Settings =====//

        //You may change any of the values below.
        //Values that contain words must be enclosed in ", for example "Mike"
        //Values that represent numbers must end with f, for example 2.5f
        //Values that represent true or false must NOT be written with capitals.
        //Variables that take more than one value must be enclosed in brackets.

        //In order to not mess up, just replace the values and don't touch anything else.
        //If you do break something simply re-import the script into the programmable block.

        //Note that, for this version, the script only works with Alysius's Trajectory Missile Script.



        //=====Script Settings=====//

        //Script blocks name tag:
        public const string Tag = "mlm!";

        //Tags missile control programmable block must have to be counted as a missile. Different tags will count as different froups:
        public readonly string[] Missile_Name_Tags = new string[] { "mg!" };



        //=====Launch Settings=====//

        //The time between the fire command is issued and the first missile is actually launched (in seconds).
        public const float Launch_Delay = 1.0f;

        //Wether or not to use deviation on repeated launches to the same target.
        public readonly bool Use_Deviation = true;

        //How much are missiles allowed to deviate in each axis (in meters).
        public const float Max_Deviaton = 10.0f;

        //Time between each missile launch:
        public const float Time_Between_Launches = 1.0f;

        //Pre/Post-Launch timer tag, leave empty for no timer. (Timers must have script tag and the tags below)
        public readonly string Pre_Launch_Timer_Tag = "Pre";
        public readonly string Post_Launch_Timer_Tag = "Post";



        //=====Guidance Script Settings=====//

        //Whether or not to use these settings for the missile guidance. If set to true, values set in the missile will be ignored.
        public readonly bool Change_Guidance_Settings = false;

        //Missile Detach Port Type: 0 = Merge Block; 1 = Rotor; 2 = Connector; 3 = Merge Block And Any Locked Connectors, 4 = Rotor And Any Locked Connectors, 99 = No detach required
        public const int Missile_Detach_Port_Type = 0;

        //Missile Trajectory Type: 0 = Freefall To Target With Aiming (For Cluster Bomb Deployments), 1 = Freefall To Target Without Aiming (For Cluster Bomb Deployments), 2 = Thrust And Home In To Target
        public const int Missile_Trajectory_Type = 2;

        //Max Grid Speed (if you use speed mods):
        public const float Max_Grid_Speed = 104.37f;

        //Flight Cruise Altitude:
        public const float Flight_Cruise_Altitude = 1500.0f;



        //===== Code =====//
        //DON'T CHANGE ANYTHING BELOW
        #endregion

        #region Variables
        private LaunchControl launchControl;

        private IMyTextSurface LCD = null;

        private Vector2Int cursor = new Vector2Int(0, 0);

        private string error = string.Empty;
        private string log = string.Empty;
        #endregion



        #region Default methods
        public Program()
        {
            Start();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update1)
            {
                launchControl.ProcessLaunches();
                UI();
                return;
            }

            if (updateSource == UpdateType.Once)
            {
                launchControl.ProcessPostLaunch();
                return;
            }
        	
            if (!string.IsNullOrEmpty(argument))
			    ProcessArguments(argument);

            CheckBlocks();
            CheckForMissiles();
            CheckCursorOutOfBounds();
            UI();
        }

        public void Save()
        {
            SaveToStorage();
        }
        #endregion



        #region UI methods
        private void UI()
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

        private void DrawSriptes(ref MySpriteDrawFrame frame, ref RectangleF viewPort)
        {
            //sprite data: SquareHollow, Triangle, CircleHollow

            MySprite sprite;

            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = $"Mode: {launchControl.LaunchMode}",
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
                Data = $"FireState: {launchControl.FireState}",
                Position = new Vector2(viewPort.Width, 0),
                RotationOrScale = 1f,
                Color = Color.Green,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            };//fireState

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

            for (int i = 0; i < launchControl.TargetCount(); i++)
            {
                sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = launchControl.TargetAt(i).name,
                    Position = new Vector2(5, i * 20 + 30),
                    RotationOrScale = 1f,
                    Color = Color.Green,
                    Alignment = TextAlignment.LEFT,
                    FontId = "White"
                };

                frame.Add(sprite);

                if (launchControl.IsTargetSelected(i))
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

            for (int i = 0; i < launchControl.MissileCount(); i++)
            {
                sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = launchControl.MissileAt(i).name,
                    Position = new Vector2(viewPort.Width - 5, i * 20 + 30),
                    Color = Color.Green,
                    RotationOrScale = 0.8f,
                    Alignment = TextAlignment.RIGHT
                };

                frame.Add(sprite);

                if (launchControl.IsMissileSelected(i))
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



        #region Logic methods
        private void Start()
        {
            launchControl = new LaunchControl(this);

            if (!string.IsNullOrEmpty(Storage))
            {
                LoadFromStorage();
            }
            launchControl.LaunchMode = Mode.Multiple;

            CheckBlocks();
            CheckForMissiles();
            UI();
        }

        private void CheckBlocks()
        {
            List<IMyTextSurface> surfaces = new List<IMyTextSurface>();
            GridTerminalSystem.GetBlocksOfType(surfaces);

            foreach (IMyTextSurface sur in surfaces)
            {
                IMyFunctionalBlock block = sur as IMyFunctionalBlock;

                if (block.DisplayNameText.Contains(Tag)) { LCD = sur; break; }
            }

            List<IMyTimerBlock> tbs = new List<IMyTimerBlock>();
            GridTerminalSystem.GetBlocksOfType(tbs);
            IMyTimerBlock preLaunch = null, postLaunch = null;

            if (!string.IsNullOrEmpty(Pre_Launch_Timer_Tag))
                foreach (IMyTimerBlock tb in tbs)
                    if (tb.DisplayNameText.Contains(Pre_Launch_Timer_Tag) && tb.DisplayNameText.Contains(Tag)) { preLaunch = tb; break; }

            if (!string.IsNullOrEmpty(Post_Launch_Timer_Tag))
                foreach (IMyTimerBlock tb in tbs)
                    if (tb.DisplayNameText.Contains(Post_Launch_Timer_Tag) && tb.DisplayNameText.Contains(Tag)) { postLaunch = tb; break; }

            launchControl.SetLaunchTimers(preLaunch, postLaunch);
        }
        private void CheckForMissiles()
        {
            List<IMyProgrammableBlock> allPrograms = new List<IMyProgrammableBlock>();
            GridTerminalSystem.GetBlocksOfType(allPrograms);

            List<IMyProgrammableBlock> filteredControls = allPrograms.FindAll((IMyProgrammableBlock program) => Missile_Name_Tags.Any(t => program.DisplayNameText.Contains(t)));

            launchControl.ClearMissingMissiles(filteredControls);

            foreach (IMyProgrammableBlock control in filteredControls)
            {
                if (launchControl.MissileExists(control))
                    continue;

                Missile msl = new Missile(control.DisplayNameText, control);
                launchControl.AddMissile(msl);
            }
        }

        private void ProcessArguments(string arg)
        {
        	string instruction = arg.Split(' ')[0].ToLowerInvariant();
        	string[] arguments = arg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();

            if (arguments == null) arguments = Array.Empty<string>();

            Echo($"Instruction: '{arg}'");

        	switch(instruction)
        	{
        		case "fire":

                    if (arguments.Length == 0) launchControl.StartLaunches();
                    else Error("Invalid number of arguments for command 'fire'.");
        		    
        			break;
        		
        		case "add":

                    if (arguments.Length == 1)
                    {
                        Target tgt;

                        if (GetTarget(arguments[0], out tgt))
                        {
                            if (!launchControl.HasTarget(tgt))
                                launchControl.AddTarget(tgt);
                            else
                                Error("Target already exists.");
                        }
                        else Error("Unable to parse GPS point!");
                    }
                    else Error("Invalid number of arguments for command 'add'.");
        			
        			break;

                case "remove":

                    if (arguments.Length == 0)
                    {
                        if (cursor.x == 0 && launchControl.TargetCount() > 0) launchControl.RemoveTarget(cursor.y);
                    }
                    else if (arguments.Length == 1)
                    {
                        Target tgt;
                        if (arguments[0].ToLowerInvariant() == "all")
                        {
                            launchControl.ClearTargets();
                        }
                        else if (GetTarget(arguments[0], out tgt))
                        {
                            if (launchControl.HasTarget(tgt))
                                launchControl.RemoveTarget(tgt);
                            else
                                Error("Target not found.");
                        }
                        else
                        {
                            for (int i = 0; i < launchControl.TargetCount(); i++)
                                if (launchControl.TargetAt(i).name == arguments[0])
                                {
                                    launchControl.RemoveTarget(i);
                                    break;
                                }

                            Error("Unable to parse GPS point!");
                        }
                    }
                    else Error("Invalid number of arguments for command 'remove'.");

                    break;

                case "select":

                    if (arguments.Length  == 0)
                    {
                        if (cursor.x == 0)
                        {
                            if (launchControl.TargetCount() > 0)
                                launchControl.SelectTarget(cursor.y);
                        }
                        else if (cursor.x == 1)
                        {
                            if (launchControl.MissileCount() > 0)
                                launchControl.SelectMissile(cursor.y);
                        }

                        return;
                    }
                    else if (arguments.Length == 1)
                    {
                        if (arguments[0] == "all")
                        {
                            launchControl.SelectAllMissiles();
                            launchControl.SelectAllTargets();
                        }
                        else
                            Error("Invalid first argument for instruction 'select'");
                    }
                    else if (arguments.Length == 2)
                    {
                        if (arguments[0].ToLowerInvariant() == "all")
                        {
                            if (arguments[1].ToLowerInvariant() == "missiles")
                                launchControl.SelectAllMissiles();
                            else if (arguments[1].ToLowerInvariant() == "targets")
                                launchControl.SelectAllTargets();
                        }
                        if (arguments[0] == "missile")
                        {
                            int i;

                            if (!int.TryParse(arguments[1], out i))
                            {
                                Error($"Couldn't convert {arguments[1]} to an integer.");
                                return;
                            }

                            if (i >= launchControl.MissileCount() || i < 0)
                            {
                                Error($"{i} is outside the valid values for missile index.");
                                return;
                            }

                            launchControl.SelectMissile(i);

                            return;
                        }
                        else if (arguments[0] == "target")
                        {
                            if (arguments[1] == "all")
                            {
                                launchControl.SelectAllTargets();
                                break;
                            }

                            int i;

                            if (!int.TryParse(arguments[1], out i))
                            {
                                Error($"Couldn't convert {arguments[1]} to an integer.");
                                return;
                            }

                            if (i >= launchControl.TargetCount() || i < 0)
                            {
                                Error($"{i} is outside the valied values for target index.");
                                return;
                            }

                            launchControl.SelectTarget(i);
                        }
                    }
                    else Error("Invalid number of arguments for command 'select'.");

                    break;

                case "deselect":

                    if (arguments.Length  == 0)
                    {
                        if (cursor.x == 0)
                        {
                            if (launchControl.TargetCount() > 1)
                                launchControl.UnselectTarget(cursor.y);
                        }
                        else if (cursor.x == 1)
                        {
                            if (launchControl.MissileCount() > 1)
                                launchControl.UnselectMissile(cursor.y);
                        }

                        return;
                    }
                    else if (arguments.Length == 1)
                    {
                        if (arguments[0].ToLowerInvariant() == "all")
                        {
                            launchControl.ClearSelectedMissiles();
                            launchControl.ClearSelectedTargets();
                        }
                        else Error("Invalid first argument for instruction 'deselect'.");
                    }
                    else if (arguments.Length == 2)
                    {
                        if (arguments[0].ToLowerInvariant() == "all")
                        {
                            if (arguments[1].ToLowerInvariant() == "missiles")
                                launchControl.ClearSelectedMissiles();
                            else if (arguments[1].ToLowerInvariant() == "targets")
                                launchControl.ClearSelectedTargets();
                        }
                        else if (arguments[0].ToLowerInvariant() == "missile")
                        {
                            int i;
                            if (!int.TryParse(arguments[1], out i))
                            {
                                Error($"Couldn't convert {arguments[1]} to an integer.");
                                return;
                            }

                            if (i >= launchControl.MissileCount() || i < 0)
                            {
                                Error($"{i} is outside the valied values for missile index.");
                                return;
                            }

                            launchControl.UnselectMissile(i);

                            return;
                        }
                        else if (arguments[0].ToLowerInvariant() == "target")
                        {
                            int i;

                            if (!int.TryParse(arguments[1], out i))
                            {
                                Error($"Couldn't convert {arguments[1]} to an integer.");
                                return;
                            }

                            if (i >= launchControl.TargetCount() || i < 0)
                            {
                                Error($"{i} is outside the valied values for target index.");
                                return;
                            }

                            launchControl.UnselectTarget(i);
                        }
                    }
                    else Error("Invalid number of arguments for command 'deselect'.");

                    break;

                case "abort":

                    if (arguments.Length == 0)
                    {
                        Error("LAUNCHES ABORTED.");
                        launchControl.FireState = FireState.Idle;
                    }
                    else Error("Invalid number of arguments for command 'abort'.");

                    break;

                case "update":

                    if (arguments.Length == 0) { CheckForMissiles(); CheckBlocks(); }
                    else Error("Invalid number of arguments for command 'update'.");

                    break;

                case "save":

                    if (arguments.Length == 0) SaveToStorage();
                    else Error("Invalid number of arguments for command 'save'.");

                    break;

                case "mode":

                    if (arguments.Length == 1)
                    {
                        if (launchControl.FireState != FireState.Idle)
                            Error("Cannot change mode while fireState.");
                        switch (arguments[0].ToLowerInvariant())
                        {
                            case "sgl":
                            case "single":
                                launchControl.LaunchMode = Mode.Single;
                                break;

                            case "multi":
                            case "multiple":
                                launchControl.LaunchMode = Mode.Multiple;
                                break;

                            case "auto":
                                launchControl.LaunchMode = Mode.Auto;
                                break;

                            default:
                                Error($"Invalid mode '{arguments[0]}'");
                                break;
                        }
                    }
                    else Error("Invalid number of arguments for command 'mode'.");

                    break;

                #region cursor movement
                case "up":

                    if (arguments.Length == 0) cursor.y--;
                    else Error("Invalid number of arguments for command 'up'.");

                    break;

                case "down":

                    if (arguments.Length == 0) cursor.y++;
                    else Error("Invalid number of arguments for command 'down'.");

                    break;

                case "left":

                    if (arguments.Length == 0)
                    {
                        cursor.x--;

                        if (cursor.x < 0)
                        {
                            cursor.x = 1;

                            if (cursor.y >= launchControl.MissileCount() - 1)
                                cursor.y = Math.Max(launchControl.MissileCount() - 1, 0);
                        }
                        else
                        {
                            if (cursor.y >= launchControl.TargetCount() - 1)
                                cursor.y = Math.Max(launchControl.TargetCount() - 1, 0);
                        }
                    }
                    else Error("Invalid number of arguments for command 'left'.");

                    break;

                case "right":

                    if (arguments.Length == 0)
                    {
                        cursor.x++;

                        if (cursor.x > 1)
                        {
                            cursor.x = 0;

                            if (cursor.y >= launchControl.TargetCount() - 1) cursor.y = Math.Max(launchControl.TargetCount() - 1, 0);
                        }
                        else
                        {
                            if (cursor.y >= launchControl.MissileCount() - 1) cursor.y = Math.Max(launchControl.MissileCount() - 1, 0);
                        }
                    }
                    else Error("Invalid number of arguments for command 'right'.");

                    break;
                #endregion
            }
        }

        private void LoadFromStorage()
        {
            string[] lines = Storage.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach(string line in lines)
            {
                if (line.StartsWith("GPS:"))
                {
                    Target tgt;

                    if (!GetTarget(line, out tgt))
                        Error("Couldn't parse target from Storage.");
                    else
                        launchControl.AddTarget(tgt);
                }
                else Error("Value in storage was not recognised.");
            }
        }
        private void SaveToStorage()
        {
            string str = string.Empty;

            for (int i = 0; i < launchControl.TargetCount(); i++)
                str += $"{launchControl.TargetAt(i)}\n";

            Storage = str;
        }
        #endregion



        #region Util Methods
        private bool GetTarget(string gps, out Target tgt)
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

        private void CheckCursorOutOfBounds()
        {
            if (cursor.x == 0)
            {
                if (cursor.y < 0)
                    cursor.y = launchControl.TargetCount() - 1;
                else if (cursor.y >= launchControl.TargetCount())
                    cursor.y = 0;
            }
            else if (cursor.x == 1)
            {
                if (cursor.y < 0)
                    cursor.y = launchControl.MissileCount() - 1;
                else if (cursor.y >= launchControl.MissileCount())
                    cursor.y = 0;
            }
        }

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



        #region Enums
        public enum Mode
        {
        	None = 0,
            Single,
        	Multiple,
            Auto,
        }

        public enum FireState
        {
            Idle,
            Delay,
            Wait,
            Firing,
        }
        #endregion
    }
}
