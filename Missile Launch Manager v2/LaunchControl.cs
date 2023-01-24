using System;
using System.Collections.Generic;
using System.Linq;

using SpaceEngineers.Game.ModAPI.Ingame;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using static IngameScript.Program;

namespace IngameScript
{
    partial class Program
    {
        class LaunchControl
        {
            public Mode LaunchMode = Mode.None;
            public FireState FireState = FireState.Idle;

            private IMyTimerBlock preLaunch, postLaunch;

            private readonly Program host;

            private readonly List<Target> targets = new List<Target>();
            private readonly List<Missile> missiles = new List<Missile>();
            private readonly Queue<MissileLaunch> queuedMissiles = new Queue<MissileLaunch>();

            private readonly List<int> selectedTargets = new List<int>();
            private readonly List<int> selectedMissiles = new List<int>();

            private readonly Random rdm = new Random();

            private float fireTiming = 0f;
            private Missile lastLaunched;



            public LaunchControl(Program host)
            {
                this.host = host;
            }



            public void StartLaunches()
            {
                if (selectedTargets.Count == 0)
                {
                    host.Error("No targets selected!");
                    return;
                }

                switch (LaunchMode)
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
                        host.Error("This mode is not currently implemented.");
                        break;
                }
            }
            public void StartMultipleLaunches()
            {
                if (selectedTargets.Count > selectedMissiles.Count)
                {
                    host.Error($"Not enough missiles selected.\nMust be at least 1 per target.\n{selectedTargets.Count}:{selectedMissiles.Count}");
                    return;
                }

                FireState = FireState.Delay;
                host.Runtime.UpdateFrequency = UpdateFrequency.Update1;

                for (int m = 0, t = 0; m < selectedMissiles.Count; m++)
                {
                    if (t >= selectedTargets.Count)
                        t = 0;

                    QueueMissileLaunch(targets[selectedTargets[t++]], missiles[selectedMissiles[m]]);
                }
            }
            public void StartAutoLaunches()
            {
                FireState = FireState.Delay;
                host.Runtime.UpdateFrequency = UpdateFrequency.Update1;

                int m = 0, t = 0;

                if (selectedTargets.Count > missiles.Count)
                {
                    while (m < missiles.Count)
                    {
                        QueueMissileLaunch(targets[selectedTargets[t++]], missiles[m++]);
                    }
                }
                else
                {
                    while ((missiles.Count - m) >= selectedTargets.Count)
                    {
                        foreach (int tgt in selectedTargets)
                        {
                            QueueMissileLaunch(targets[tgt], missiles[m++]);
                        }
                    }
                }
            }
            public void StartSingleLaunches()
            {
                if (selectedTargets.Count > missiles.Count)
                {
                    host.Error("There are not enough missiles for the selected targets.");
                    return;
                }

                FireState = FireState.Delay;
                host.Runtime.UpdateFrequency = UpdateFrequency.Update1;

                for (int t = 0, m = 0; t < selectedTargets.Count; t++)
                {
                    QueueMissileLaunch(targets[selectedTargets[t]], missiles[m++]);
                }
            }



            public void QueueMissileLaunch(Target tgt, Missile msl)
            {
                if (host.Use_Deviation)
                {
                    foreach (MissileLaunch l in queuedMissiles)
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
            public void FireMissile(MissileLaunch launch)
            {
                int mslIndex = missiles.IndexOf(launch.missile);
                selectedMissiles.Remove(mslIndex);
                missiles.Remove(launch.missile);

                if (host.Change_Guidance_Settings)
                {
                    string dt = string.Empty;

                    dt += $"missileDetachPortType={Missile_Detach_Port_Type}\n";
                    dt += $"missileTrajectoryType={Missile_Trajectory_Type}\n";
                    dt += $"MAX_FALL_SPEED={Max_Grid_Speed}\n";
                    dt += $"missileTravelHeight={Flight_Cruise_Altitude}\n";

                    launch.missile.control.CustomData = dt;
                }

                launch.missile.control.TryRun(launch.target.ToString());

                lastLaunched = launch.missile;
            }



            public void ProcessPreLaunch()
            { 
                preLaunch?.Trigger();
            }
            public void ProcessLaunches()
            {
                if (FireState == FireState.Idle)
                    return;

                fireTiming += (float)host.Runtime.TimeSinceLastRun.TotalSeconds;

                switch (FireState)
                {
                    case FireState.PreFire:
                        ProcessPreLaunch();
                        FireState = FireState.Firing;
                        break;

                    case FireState.Firing:
                        if (queuedMissiles.Count == 0)
                        {
                            FireState = FireState.Idle;
                            host.Runtime.UpdateFrequency = UpdateFrequency.Once;
                            host.Log("Launches completed!");

                            fireTiming = 0f;
                            return;
                        }

                        if (fireTiming < Time_Between_Launches)
                            return;

                        MissileLaunch launch = queuedMissiles.Dequeue();
                        FireMissile(launch);
                        FireState = FireState.Wait;
                        break;

                    case FireState.Wait:
                        if (fireTiming >= Time_Between_Launches)
                        {
                            fireTiming = 0f;
                            FireState = FireState.PreFire;
                        }
                        break;

                    case FireState.Delay:
                        if (fireTiming >= Launch_Delay)
                            FireState = FireState.PreFire;
                        break;
                }
            }
            public void ProcessPostLaunch()
            {
                if (lastLaunched != null)
                {
                    postLaunch?.Trigger();
                    lastLaunched = null;
                }
            }



            public void SetLaunchTimers(IMyTimerBlock preLaunch, IMyTimerBlock postLaunch)
            {
                this.preLaunch = preLaunch;
                this.postLaunch = postLaunch;
            }



            #region Missiles wrapper
            public void AddMissile(Missile msl) => missiles.Add(msl);
            public void RemoveMissile(Missile msl) => missiles.Remove(msl);
            public void ClearMissiles() => missiles.Clear();
            public int MissileCount() => missiles.Count;
            public Missile MissileAt(int index) => missiles[index];
            public bool MissileExists(IMyProgrammableBlock control) => missiles.Any((Missile msl) => msl.control == control);
            public void ClearMissingMissiles(List<IMyProgrammableBlock> controls)
            {
                List<Missile> missing = missiles.FindAll((Missile msl) => !controls.Contains(msl.control));

                foreach (Missile msl in missing)
                    missiles.Remove(msl);
            }
            #endregion

            #region Targets wrapper
            public void AddTarget(Target tgt) => targets.Add(tgt);
            public void RemoveTarget(Target tgt) => targets.Remove(tgt);
            public void RemoveTarget(int index) => targets.RemoveAt(index);
            public void ClearTargets() => targets.Clear();
            public bool HasTarget(Target tgt) => targets.Contains(tgt);
            public int TargetCount() => targets.Count;
            public Target TargetAt(int index) => targets[index];
            #endregion

            #region Selected missiles wrapper
            public void SelectMissile(int index)
            {
                if (!selectedMissiles.Contains(index))
                    selectedMissiles.Add(index);
            }
            public void SelectAllMissiles()
            {
                selectedMissiles.Clear();
                for (int i = 0; i < missiles.Count; i++)
                    selectedMissiles.Add(i);
            }
            public bool IsMissileSelected(int index) => selectedMissiles.Contains(index);
            public void UnselectMissile(int index) => selectedMissiles.Remove(index);
            public void ClearSelectedMissiles() => selectedMissiles.Clear();
            #endregion

            #region Selected targets wrapper
            public void SelectTarget(int index)
            { 
                if (!selectedTargets.Contains(index))
                    selectedTargets.Add(index);
            }
            public void SelectAllTargets()
            {
                selectedTargets.Clear();
                for (int i = 0; i < targets.Count; i++)
                    selectedTargets.Add(i);
            }
            public bool IsTargetSelected(int index) => selectedTargets.Contains(index);
            public void UnselectTarget(int index) => selectedTargets.Remove(index);
            public void ClearSelectedTargets() => selectedTargets.Clear();
            #endregion



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
        }
    }
}
