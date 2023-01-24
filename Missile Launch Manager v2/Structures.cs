using System;

using SpaceEngineers.Game.ModAPI.Ingame;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using static IngameScript.Program;

namespace IngameScript
{
    partial class Program
    {
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

        public class Missile
        {
            public string name;
            public IMyProgrammableBlock control;
            public IMyTimerBlock preLaunch, postLaunch;
            public readonly int UID;

            //For debugging purposes only so far
            private static int nextUID = 0;

            public Missile(string name, IMyProgrammableBlock control)
            {
                this.name = name;
                this.control = control;
                preLaunch = null; postLaunch = null;
                UID = nextUID++;
            }

            public Missile(string name, IMyProgrammableBlock control, IMyTimerBlock preLaunch, IMyTimerBlock postLaunch)
            {
                this.name = name;
                this.control = control;
                this.preLaunch = preLaunch; this.postLaunch = postLaunch;
                UID = nextUID++;
            }
        }

        public struct Vector2Int
        {
            public int x;
            public int y;

            public Vector2Int(int X, int Y) { x = X; y = Y; }
        }
    }
}
