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
        //Missile Launch Manager v2 (Version 2.1)
        //Done:
        // -Remove all (targets)
        // -Update now checks for blocks too
        // -Mode
        // -Single mode
        // -Auto mode
        // -Launch spread
        // -Launch with delay
        //TODO:
        // Opt to keep batteries (with tag) on recharge right until before launch
        // Launch with timer

        #region Instructions
        //===== Instructions =====//

        //=====Setup=====//

        //NOTE: Before anything, remember that GPS signals CAN NOT have SPACES in their names for this script to work.

        //1) Ensure the grid has: a programable block (for this script) and power. (An LCD panel and some buttons is STRONGLY recommended but not needed to work)
        //2) If using an LCD, ensure it's name contains the tag 'mlm!' or a tag you may set below in the settings.
        //3) Check that your missiles guidance programmable block has the tag 'mg!' or a tag you may set below in the settings.

        //And that's it!

        //Here are the commands available to control the script:
        /* fire - Starts launches, depending on the selected mode, targets and missiles
         * add <GPS> - Adds a GPS target to the target list
         * remove [<GPS>/<name>/all] - Removes a GPS target from the target list, if using and LCD accepts no argument, otherwise accepts either the entire GPS point, the name or 'all' to clear the target list.
         * select [all/missile/target] [<number>] - Selects the highlighted missile/target if using an LCD, selects everything if used with the argument 'all', selects all missiles or all targets if using with arguments 'all targets' or 'all missiles', selects specific target/missile if given a number to select.
         * deselect [all/missile/target] [<number>] - See command 'select'
         * abort - Cancells all queued launches.
         * update - Checks for new LCDs and missiles.
         * save - Stores all targets to programmable block memory (not needed).
         * mode auto/multiple/single - Sets launch mode. Auto will launch one per selected target or an equal number per selected target, multiple will fire all selected and distribute by the selected targets, single will launch one per selected target.
         */
        #endregion

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
        private const string Tag = "mlm!";

        //Time between each missile launch:
        private const float Time_Between_Launches = 1.0f;

        //Tags missile control programmable block must have to be counted as a missile:
        private readonly string[] Missile_Name_Tags = new string[] { "mg!" };



        //=====Launch Settings=====//

        //The time between the fire command is issued and the first missile is actually launched (in seconds).
        private const float Launch_Delay = 1.0f;

        //Wether or not to use deviation on repeated launches to the same target.
        private bool Use_Deviation = true;

        //How much are missiles allowed to deviate in each axis (in meters).
        private const float Max_Deviaton = 10.0f;



        //=====Guidance Script Settings=====//

        //Whether or not to use these settings for the missile guidance. If set to true, values set in the missile will be ignored.
        private bool Change_Guidance_Settings = false;

        //Missile Detach Port Type: 0 = Merge Block; 1 = Rotor; 2 = Connector; 3 = Merge Block And Any Locked Connectors, 4 = Rotor And Any Locked Connectors, 99 = No detach required
        private const int Missile_Detach_Port_Type = 0;

        //Missile Trajectory Type: 0 = Freefall To Target With Aiming (For Cluster Bomb Deployments), 1 = Freefall To Target Without Aiming (For Cluster Bomb Deployments), 2 = Thrust And Home In To Target
        private const int Missile_Trajectory_Type = 2;

        //Max Grid Speed (if you use speed mods):
        private const float Max_Grid_Speed = 104.37f;

        //Flight Cruise Altitude:
        private const float Flight_Cruise_Altitude = 1500.0f;



        //===== Code =====//
        //DON'T CHANGE ANYTHING BELOW
        #endregion

        #region Variables
        private readonly List<Target> targets = new List<Target>();
        private readonly List<Missile> missiles = new List<Missile>();
        private readonly Queue<MissileLaunch> queuedMissiles = new Queue<MissileLaunch>();

        private readonly List<Target> selectedTargets = new List<Target>();
        private readonly List<Missile> selectedMissiles = new List<Missile>();

        private readonly Random rdm = new Random();

        private Vector2Int cursor = new Vector2Int(0, 0);
        
        private Mode _mode = Mode.None;
        private FireState fireState = FireState.Idle;
        private float fireTiming = 0f;

        private IMyTextSurface LCD = null;

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
                ProcessLaunches();
                UI();
                return;
            }
        	
            if (!string.IsNullOrEmpty(argument))
			    ProcessArguments(argument);

            if (LCD == null) CheckBlocks();
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
                Data = $"FireState: {fireState}",
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

        #region Logic methods
        private void Start()
        {
            if (!string.IsNullOrEmpty(Storage))
            {
                LoadFromStorage();
            }
            _mode = Mode.Auto;
            fireState = FireState.Idle;
            fireTiming = 0f;

            CheckBlocks();
            CheckForMissiles();
            UI();
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

                    if (arguments.Length == 0) StartLaunches();
                    else Error("Invalid number of arguments for command 'fire'.");
        		    
        			break;
        		
        		case "add":

                    if (arguments.Length == 1)
                    {
                        Target tgt;

                        if (GetTarget(arguments[0], out tgt))
                        {
                            if (!targets.Contains(tgt))
                                targets.Add(tgt);
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
                        if (cursor.x == 0 && targets.Count > 0) targets.RemoveAt(cursor.y);
                    }
                    else if (arguments.Length == 1)
                    {
                        Target tgt;
                        if (arguments[0].ToLowerInvariant() == "all")
                        {
                            targets.Clear();
                        }
                        else if (GetTarget(arguments[0], out tgt))
                        {
                            if (targets.Contains(tgt))
                                targets.Remove(tgt);
                            else
                                Error("Target not found.");
                        }
                        else
                        {
                            foreach (Target t in targets)
                                if (t.name == arguments[0])
                                {
                                    targets.Remove(t);
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
                            if (targets.Count > 0)
                                selectedTargets.Add(targets[cursor.y]);
                        }
                        else if (cursor.x == 1)
                        {
                            if (missiles.Count > 0)
                                selectedMissiles.Add(missiles[cursor.y]);
                        }

                        return;
                    }
                    else if (arguments.Length == 1)
                    {
                        if (arguments[0] == "all")
                        {
                            selectedMissiles.Clear();
                            selectedMissiles.AddRange(missiles);

                            selectedTargets.Clear(); 
                            selectedTargets.AddRange(targets);
                        }
                        else
                            Error("Invalid first argument for instruction 'select'");
                    }
                    else if (arguments.Length == 2)
                    {
                        if (arguments[0].ToLowerInvariant() == "all")
                        {
                            if (arguments[1].ToLowerInvariant() == "missiles") { selectedMissiles.Clear(); selectedMissiles.AddRange(missiles.ToArray()); }
                            else if (arguments[1].ToLowerInvariant() == "targets") { selectedTargets.Clear(); selectedTargets.AddRange(targets.ToArray()); }
                        }
                        if (arguments[0] == "missile")
                        {
                            int i;

                            if (!int.TryParse(arguments[1], out i))
                            {
                                Error($"Couldn't convert {arguments[1]} to an integer.");
                                return;
                            }

                            if (i >= missiles.Count || i < 0)
                            {
                                Error($"{i} is outside the valid values for missile index.");
                                return;
                            }

                            selectedMissiles.Add(missiles[i]);

                            return;
                        }
                        else if (arguments[0] == "target")
                        {
                            if (arguments[1] == "all")
                            {
                                selectedTargets.Clear();
                                selectedTargets.AddRange(targets);
                                break;
                            }

                            int i;

                            if (!int.TryParse(arguments[1], out i))
                            {
                                Error($"Couldn't convert {arguments[1]} to an integer.");
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
                    else Error("Invalid number of arguments for command 'select'.");

                    break;

                case "deselect":

                    if (arguments.Length  == 0)
                    {
                        if (cursor.x == 0)
                        {
                            if (targets.Count > 1)
                                selectedTargets.Remove(targets[cursor.y]);
                        }
                        else if (cursor.x == 1)
                        {
                            if (missiles.Count > 1)
                                selectedMissiles.Remove(missiles[cursor.y]);
                        }

                        return;
                    }
                    else if (arguments.Length == 1)
                    {
                        if (arguments[0].ToLowerInvariant() == "all")
                        {
                            selectedTargets.Clear();
                            selectedMissiles.Clear();
                        }
                        else Error("Invalid first argument for instruction 'deselect'.");
                    }
                    else if (arguments.Length == 2)
                    {
                        if (arguments[0].ToLowerInvariant() == "all")
                        {
                            if (arguments[1].ToLowerInvariant() == "missiles") selectedMissiles.Clear();
                            else if (arguments[1].ToLowerInvariant() == "targets") selectedTargets.Clear();
                        }
                        else if (arguments[0].ToLowerInvariant() == "missile")
                        {

                            int i;

                            if (!int.TryParse(arguments[1], out i))
                            {
                                Error($"Couldn't convert {arguments[1]} to an integer.");
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
                        else if (arguments[0].ToLowerInvariant() == "target")
                        {
                            int i;

                            if (!int.TryParse(arguments[1], out i))
                            {
                                Error($"Couldn't convert {arguments[1]} to an integer.");
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
                    else Error("Invalid number of arguments for command 'deselect'.");

                    break;

                case "abort":

                    if (arguments.Length == 0)
                    {
                        Error("LAUNCHES ABORTED.");
                        fireState = FireState.Idle;
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
                        if (fireState != FireState.Idle)
                            Error("Cannot change mode while fireState.");
                        switch (arguments[0].ToLowerInvariant())
                        {
                            case "sgl":
                            case "single":
                                _mode = Mode.Single;
                                break;

                            case "multi":
                            case "multiple":
                                _mode = Mode.Multiple;
                                break;

                            case "auto":
                                _mode = Mode.Auto;
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

                            if (cursor.y >= missiles.Count - 1)
                                cursor.y = Math.Max(missiles.Count - 1, 0);
                        }
                        else
                        {
                            if (cursor.y >= targets.Count - 1)
                                cursor.y = Math.Max(targets.Count - 1, 0);
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

                            if (cursor.y >= targets.Count - 1) cursor.y = Math.Max(targets.Count - 1, 0);
                        }
                        else
                        {
                            if (cursor.y >= missiles.Count - 1) cursor.y = Math.Max(missiles.Count - 1, 0);
                        }
                    }
                    else Error("Invalid number of arguments for command 'right'.");

                    break;
                #endregion
            }
        }
        private void ProcessLaunches()
        {
            if (fireState == FireState.Idle)
                return;

            fireTiming += (float)Runtime.TimeSinceLastRun.TotalSeconds;

            switch (fireState)
            {
                case FireState.Firing:
                    if (queuedMissiles.Count == 0)
                    {
                        fireState = FireState.Idle;
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        Log("Launches completed!");

                        fireTiming = 0f;
                        return;
                    }

                    if (fireTiming < Time_Between_Launches)
                        return;

                    MissileLaunch launch = queuedMissiles.Dequeue();
                    FireMissile(launch);
                    missiles.Remove(launch.missile);
                    fireState = FireState.Wait;
                    break;

                case FireState.Wait:
                    if (fireTiming >= Time_Between_Launches)
                    {
                        fireTiming = 0f;
                        fireState = FireState.Firing;
                    }
                    break;

                case FireState.Delay:
                    if (fireTiming >= Launch_Delay)
                        fireState = FireState.Firing;
                    break;
            }
        }

        private void StartLaunches()
        {
            if (selectedTargets.Count == 0)
            {
                Error("No targets selected!");
                return;
            }

            switch (_mode)
            {
                case Mode.Multiple:
                    StartMultipleLaunches();
                    break;

                case Mode.Single:
                    StartSingleLaunches();
                    break;

                case Mode.Auto:
                    StartAutoLaunches();
                    break;

                default:
                    Error("This mode is not currently implemented.");
                    break;
            }
        }
        private void StartMultipleLaunches()
        {
            if (selectedTargets.Count > selectedMissiles.Count)
            {
                Error("Not enough missiles selected. Must be at least 1 per target.");
                return;
            }

            fireState = FireState.Delay;
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            for (int m = 0, t = 0; m < selectedMissiles.Count; m++)
            {
                if (t >= selectedTargets.Count)
                    t = 0;

                QueueMissileLaunch(selectedTargets[t++], selectedMissiles[m]);
            }
        }
        private void StartAutoLaunches()
        {
            fireState = FireState.Delay;
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            int m = 0, t = 0;

            if (selectedTargets.Count > missiles.Count)
            {
                while (m < missiles.Count)
                {
                    QueueMissileLaunch(selectedTargets[t++], missiles[m++]);
                }
            }
            else
            {
                while ((missiles.Count - m) >= selectedTargets.Count)
                {
                    foreach (Target tgt in selectedTargets)
                    {
                        QueueMissileLaunch(tgt, missiles[m++]);
                    }
                }
            }
        }
        private void StartSingleLaunches()
        {
            if (selectedTargets.Count > missiles.Count)
            {
                Error("There are not enough missiles for the selected targets.");
                return;
            }

            fireState = FireState.Delay;
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            for (int t = 0, m = 0; t < selectedTargets.Count; t++)
            {
                QueueMissileLaunch(selectedTargets[t], missiles[m++]);
            }
        }

        private void CheckForMissiles()
        {
            missiles.Clear();

            List<IMyProgrammableBlock> allPrograms = new List<IMyProgrammableBlock>();
            GridTerminalSystem.GetBlocksOfType(allPrograms);

            foreach (IMyProgrammableBlock control in allPrograms)
            {
                if (Missile_Name_Tags.Any(t => control.DisplayNameText.Contains(t)))
                {
                    Missile msl = new Missile(control.DisplayNameText, control);
                    missiles.Add(msl);
                }
            }
        }
        private void CheckBlocks()
        {
            List<IMyTextSurface> surfaces = new List<IMyTextSurface>();
            GridTerminalSystem.GetBlocksOfType(surfaces);

            foreach (IMyTextSurface sur in surfaces)
            {
                IMyFunctionalBlock block = sur as IMyFunctionalBlock;

                if (block.DisplayNameText.Contains(Tag))
                {
                    LCD = sur;
                    break;
                }
            }
        }

        private void QueueMissileLaunch(Target tgt, Missile msl)
        { 
            if (Use_Deviation)
            {
                foreach(MissileLaunch l in queuedMissiles)
                    if (l.target.name == tgt.name)
                    {
                        queuedMissiles.Enqueue(new MissileLaunch(GPSDeviate(tgt), msl));
                        return;
                    }

                queuedMissiles.Enqueue(new MissileLaunch(tgt, msl));
            }
            else
                queuedMissiles.Enqueue(new MissileLaunch(tgt, msl));
        }
        private void FireMissile(MissileLaunch launch)
        {
            selectedMissiles.Remove(launch.missile);
            missiles.Remove(launch.missile);

            if (Change_Guidance_Settings)
            {
                string dt = string.Empty;

                dt += $"missileDetachPortType={Missile_Detach_Port_Type}\n";
                dt += $"missileTrajectoryType={Missile_Trajectory_Type}\n";
                dt += $"MAX_FALL_SPEED={Max_Grid_Speed}\n";
                dt += $"missileTravelHeight={Flight_Cruise_Altitude}\n";

                launch.missile.control.CustomData = dt;
            }

            launch.missile.control.TryRun(launch.target.ToString());
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
                        targets.Add(tgt);
                }
                else Error("Value in storage was not recognised.");
            }
        }
        private void SaveToStorage()
        {
            string str = string.Empty;

            foreach (Target tgt in targets)
                str += $"{tgt}\n";

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

        private Target GPSDeviate(Target source)
        {
            Target outp = new Target
            {
                name = source.name + "d",
            };

            outp.x = source.x + rdm.Next((int)(-Max_Deviaton * 100.0), (int)(Max_Deviaton * 100f)) / 100.0;
            outp.y = source.y + rdm.Next((int)(-Max_Deviaton * 100.0), (int)(Max_Deviaton * 100f)) / 100.0;
            outp.z = source.z + rdm.Next((int)(-Max_Deviaton * 100.0), (int)(Max_Deviaton * 100f)) / 100.0;

            return outp;
        }

        private void Error(string err)
        {
            Echo(err);
            if (!string.IsNullOrEmpty(err))
                error += "\n" + err;
            else
                error += err;
        }

        private void Log(string info)
        {
            Echo(info);
            if (!string.IsNullOrEmpty(log))
                log += "\n" + info;
            else
                log += info;
        }
        #endregion

        #region Structs
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
