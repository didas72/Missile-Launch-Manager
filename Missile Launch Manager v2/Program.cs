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
        bool firing = false;

        float timeSinceLastLaunch = 0f;

        IMyTextSurface LCD = null;


        public Program()
        {
            LCD = GridTerminalSystem.GetBlockWithName(LCD_Name) as IMyTextSurface;
        }

        public void Main(string argument, UpdateType updateSource)
        {


            UI();
        }

        public void UI()
        {
            if (LCD != null)
            {
                LCD.WriteText("");

                foreach (Target t in targets.ToArray())
                {
                    LCD.WriteText($"{t.name}: {Math.Round(t.x)}; {Math.Round(t.y)}; {Math.Round(t.z)}", true);
                }
            }
            else
            {
                Echo("Warning - No LCD panel.");
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
    }
}
