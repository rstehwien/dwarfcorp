﻿// CreatureAI.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using DwarfCorp.DwarfCorp;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp

{
    /// <summary>
    ///     Component which manages the AI, scripting, and status of a particular creature (such as a Dwarf or Goblin)
    /// </summary>
    [JsonObject(IsReference = true)]
    public class CreatureAI : GameComponent
    {
        public int MaxMessages = 10;
        public List<string> MessageBuffer = new List<string>();

        public CreatureAI()
        {
            Movement = new CreatureMovement(Creature);
            History = new Dictionary<string, TaskHistory>();
        }

        public CreatureAI(Creature creature,
            string name,
            EnemySensor sensor,
            PlanService planService) :
                base(name, creature.Physics)
        {
            History = new Dictionary<string, TaskHistory>();
            Movement = new CreatureMovement(creature);
            GatherManager = new GatherManager(this);
            Blackboard = new Blackboard();
            Creature = creature;
            CurrentPath = null;
            DrawPath = false;
            PlannerTimer = new Timer(0.1f, false);
            LocalControlTimeout = new Timer(5, false);
            WanderTimer = new Timer(1, false);
            Creature.Faction.Minions.Add(this);
            DrawAIPlan = false;
            WaitingOnResponse = false;
            PlanSubscriber = new PlanSubscriber(planService);
            ServiceTimeout = new Timer(2, false);
            Sensor = sensor;
            Sensor.OnEnemySensed += Sensor_OnEnemySensed;
            Sensor.Creature = this;
            CurrentTask = null;
            Tasks = new List<Task>();
            Thoughts = new List<Thought>();
            IdleTimer = new Timer(2.0f, true);
            SpeakTimer = new Timer(5.0f, true);
            XPEvents = new List<int>();
        }

        public Creature Creature { get; set; }
        public List<Voxel> CurrentPath { get; set; }
        public bool DrawPath { get; set; }
        public GatherManager GatherManager { get; set; }
        public Timer IdleTimer { get; set; }
        public List<Thought> Thoughts { get; set; }
        public Timer SpeakTimer { get; set; }

        [JsonIgnore]
        public Act CurrentAct
        {
            get
            {
                if (CurrentTask != null) return CurrentTask.Script;
                return null;
            }
        }

        [JsonIgnore]
        public Task CurrentTask { get; set; }

        public Timer PlannerTimer { get; set; }
        public Timer LocalControlTimeout { get; set; }
        public Timer WanderTimer { get; set; }
        public Timer ServiceTimeout { get; set; }
        public bool DrawAIPlan { get; set; }

        public PlanSubscriber PlanSubscriber { get; set; }
        public bool WaitingOnResponse { get; set; }
        public EnemySensor Sensor { get; set; }

        public CreatureMovement Movement { get; set; }

        [JsonIgnore]
        public Grabber Hands
        {
            get { return Creature.Hands; }
            set { Creature.Hands = value; }
        }

        [JsonIgnore]
        public Physics Physics
        {
            get { return Creature.Physics; }
            set { Creature.Physics = value; }
        }

        [JsonIgnore]
        public Faction Faction
        {
            get { return Creature.Faction; }
            set { Creature.Faction = value; }
        }

        [JsonIgnore]
        public CreatureStats Stats
        {
            get { return Creature.Stats; }
            set { Creature.Stats = value; }
        }

        [JsonIgnore]
        public CreatureStatus Status
        {
            get { return Creature.Status; }
            set { Creature.Status = value; }
        }

        [JsonIgnore]
        public Vector3 Velocity
        {
            get { return Creature.Physics.Velocity; }
            set { Creature.Physics.Velocity = value; }
        }

        [JsonIgnore]
        public Vector3 Position
        {
            get { return Creature.Physics.GlobalTransform.Translation; }
            set
            {
                Matrix newTransform = Creature.Physics.LocalTransform;
                newTransform.Translation = value;
                Creature.Physics.LocalTransform = newTransform;
            }
        }

        [JsonIgnore]
        public ChunkManager Chunks
        {
            get { return PlayState.ChunkManager; }
        }

        public Blackboard Blackboard { get; set; }

        [JsonIgnore]
        public Dictionary<string, TaskHistory> History { get; set; }


        public List<Task> Tasks { get; set; }
        public bool TriggersMourning { get; set; }
        public List<int> XPEvents { get; set; }

        public void AddXP(int amount)
        {
            XPEvents.Add(amount);
        }

        public void Say(string message)
        {
            MessageBuffer.Add(message);
            if (MessageBuffer.Count > MaxMessages)
            {
                MessageBuffer.RemoveAt(0);
            }
        }

        private void Sensor_OnEnemySensed(List<CreatureAI> enemies)
        {
            if (enemies.Count > 0)
            {
                foreach (CreatureAI threat in enemies.Where(threat => !Faction.Threats.Contains(threat.Creature)))
                {
                    Faction.Threats.Add(threat.Creature);
                }
            }
        }

        public Task GetEasiestTask(List<Task> tasks)
        {
            if (tasks == null)
            {
                return null;
            }

            float bestCost = float.MaxValue;
            Task bestTask = null;
            var bestPriority = Task.PriorityType.Eventually;


            foreach (Task task in tasks)
            {
                if (History.ContainsKey(task.Name) && History[task.Name].IsLocked)
                {
                    continue;
                }

                float cost = task.ComputeCost(Creature);

                if (task.IsFeasible(Creature) && task.Priority >= bestPriority && cost < bestCost)
                {
                    bestCost = cost;
                    bestTask = task;
                    bestPriority = task.Priority;
                }
            }

            return bestTask;
        }

        public void PreEmptTasks()
        {
            if (CurrentTask == null) return;

            Task newTask = null;
            foreach (Task task in Tasks)
            {
                if (task.Priority > CurrentTask.Priority && task.IsFeasible(Creature))
                {
                    newTask = task;
                    break;
                }
            }

            if (newTask != null)
            {
                CurrentTask.Cancel();
                if (CurrentTask.ShouldRetry(Creature))
                {
                    Tasks.Add(CurrentTask);
                    CurrentTask.SetupScript(Creature);
                }
                CurrentTask = newTask;
                newTask.SetupScript(Creature);
                Tasks.Remove(newTask);
            }
        }

        public void DeleteBadTasks()
        {
            Tasks.RemoveAll(task => task.ShouldDelete(Creature));
        }

        public void ZoomToMe()
        {
            PlayState.Camera.ZoomTo(Position + Vector3.Up*8.0f);
            PlayState.ChunkManager.ChunkData.SetMaxViewingLevel((int) Position.Y, ChunkManager.SliceMode.Y);
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (!IsActive) return;

            IdleTimer.Update(gameTime);
            SpeakTimer.Update(gameTime);

            OrderEnemyAttack();
            DeleteBadTasks();
            PreEmptTasks();

            if (Status.Energy.IsUnhappy() && PlayState.Time.IsNight())
            {
                Task toReturn = new SatisfyTirednessTask();
                toReturn.SetupScript(Creature);
                if (!Tasks.Contains(toReturn))
                    Tasks.Add(toReturn);
            }

            if (Status.Hunger.IsUnhappy() && Faction.CountResourcesWithTag(Resource.ResourceTags.Edible) > 0)
            {
                Task toReturn = new SatisfyHungerTask();
                toReturn.SetupScript(Creature);
                if (!Tasks.Contains(toReturn))
                    Tasks.Add(toReturn);
            }


            if (CurrentTask != null && CurrentAct != null)
            {
                Act.Status status = CurrentAct.Tick();


                bool retried = false;
                if (status == Act.Status.Fail)
                {
                    if (History.ContainsKey(CurrentTask.Name))
                    {
                        History[CurrentTask.Name].NumFailures++;
                    }
                    else
                    {
                        History[CurrentTask.Name] = new TaskHistory();
                    }

                    if (CurrentTask.ShouldRetry(Creature) && !History[CurrentTask.Name].IsLocked)
                    {
                        if (!Tasks.Contains(CurrentTask))
                        {
                            CurrentTask.Priority = Task.PriorityType.Eventually;
                            Tasks.Add(CurrentTask);
                            CurrentTask.SetupScript(Creature);
                            retried = true;
                        }
                    }
                }
                else if (status == Act.Status.Success)
                {
                    if (History.ContainsKey(CurrentTask.Name))
                    {
                        History.Remove(CurrentTask.Name);
                    }
                }

                if (status != Act.Status.Running && !retried)
                {
                    CurrentTask = null;
                }
            }
            else
            {
                bool tantrum = false;
                if (Status.Happiness.IsUnhappy())
                {
                    tantrum = MathFunctions.Rand(0, 1) < 0.25f;
                }

                Task goal = GetEasiestTask(Tasks);
                if (goal != null)
                {
                    if (tantrum)
                    {
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Sad);
                        if (Creature.Allies == "Dwarf")
                        {
                            PlayState.AnnouncementManager.Announce(
                                Stats.FullName + " (" + Stats.CurrentLevel.Name + ")" +
                                Drawer2D.WrapColor(" refuses to work!", Color.DarkRed),
                                "Our employee is unhappy, and would rather not work!", ZoomToMe);
                        }
                        CurrentTask = null;
                    }
                    else
                    {
                        IdleTimer.Reset(IdleTimer.TargetTimeSeconds);
                        goal.SetupScript(Creature);
                        CurrentTask = goal;
                        Tasks.Remove(goal);
                    }
                }
                else
                {
                    CurrentTask = ActOnIdle();
                }
            }


            PlannerTimer.Update(gameTime);
            UpdateThoughts();
            UpdateXP();

            if (MathFunctions.RandEvent(0.01f))
            {
                Voxel above = Physics.CurrentVoxel.GetVoxelAbove();
                bool shouldDrown = above != null && (!above.IsEmpty || above.WaterLevel > 0);
                if (Physics.IsInLiquid && (!Movement.CanSwim || shouldDrown))
                {
                    Creature.Damage(1.0f, Health.DamageType.Normal);
                }
            }

            foreach (var history in History)
            {
                history.Value.Update();
            }

            base.Update(gameTime, chunks, camera);
        }

        public void UpdateXP()
        {
            foreach (int xp in XPEvents)
            {
                Stats.XP += xp;
                string sign = xp > 0 ? "+" : "";

                IndicatorManager.DrawIndicator(sign + xp + " XP",
                    Position + Vector3.Up + MathFunctions.RandVector3Cube()*0.5f, 0.5f, xp > 0 ? Color.Green : Color.Red);
            }
            XPEvents.Clear();
        }

        public virtual Act ActOnWander()
        {
            return new WanderAct(this, 2, 0.5f + MathFunctions.Rand(-0.25f, 0.25f), 1.0f);
        }

        public virtual Task ActOnIdle()
        {
            if (GatherManager.VoxelOrders.Count == 0 &&
                (GatherManager.StockOrders.Count == 0 || !Faction.HasFreeStockpile()))
            {
                // Find a room to train in
                if (Stats.CurrentClass.HasAction(GameMaster.ToolMode.Attack) && MathFunctions.RandEvent(0.01f))
                {
                    Body closestTraining = Faction.FindNearestItemWithTags("Train", Position, true);

                    if (closestTraining != null)
                    {
                        return new ActWrapperTask(new GoTrainAct(this));
                    }
                }

                // Otherwise, try to find a chair to sit in
                if (IdleTimer.HasTriggered && MathFunctions.RandEvent(0.25f))
                {
                    return new ActWrapperTask(new GoToChairAndSitAct(this))
                    {
                        Priority = Task.PriorityType.Eventually,
                        AutoRetry = false
                    };
                }
                if (IdleTimer.HasTriggered)
                {
                    IdleTimer.Reset(IdleTimer.TargetTimeSeconds);
                    return new ActWrapperTask(ActOnWander())
                    {
                        Priority = Task.PriorityType.Eventually
                    };
                }
                Physics.Velocity *= 0.0f;
                return null;
            }
                // If we have no more build orders, look for gather orders
            if (GatherManager.VoxelOrders.Count == 0)
            {
                GatherManager.StockOrder order = GatherManager.StockOrders[0];
                GatherManager.StockOrders.RemoveAt(0);
                return new ActWrapperTask(new StockResourceAct(this, order.Resource))
                {
                    Priority = Task.PriorityType.Low
                };
            }
                // Otherwise handle build orders.
            var voxels = new List<Voxel>();
            var types = new List<VoxelType>();
            foreach (GatherManager.BuildVoxelOrder order in GatherManager.VoxelOrders)
            {
                voxels.Add(order.Voxel);
                types.Add(order.Type);
            }

            GatherManager.VoxelOrders.Clear();
            return new ActWrapperTask(new BuildVoxelsAct(this, voxels, types))
            {
                Priority = Task.PriorityType.Low,
                AutoRetry = true
            };
        }


        public void Jump(DwarfTime dt)
        {
            if (!Creature.JumpTimer.HasTriggered)
            {
                return;
            }

            Creature.Physics.ApplyForce(Vector3.Up*Creature.Stats.JumpForce, (float) dt.ElapsedGameTime.TotalSeconds);
            Creature.JumpTimer.Reset(Creature.JumpTimer.TargetTimeSeconds);
            SoundManager.PlaySound(ContentPaths.Audio.jump, Creature.Physics.GlobalTransform.Translation);
        }

        public bool HasThought(Thought.ThoughtType type)
        {
            return Thoughts.Any(existingThought => existingThought.Type == type);
        }

        public void AddThought(Thought.ThoughtType type)
        {
            if (!HasThought(type))
            {
                AddThought(Thought.CreateStandardThought(type, PlayState.Time.CurrentDate), true);
            }
        }

        public void RemoveThought(Thought.ThoughtType thoughtType)
        {
            Thoughts.RemoveAll(thought => thought.Type == thoughtType);
        }

        public void AddThought(Thought thought, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                Thoughts.Add(thought);
            }
            else
            {
                if (HasThought(thought.Type))
                {
                    return;
                }

                Thoughts.Add(thought);
            }
            bool good = thought.HappinessModifier > 0;
            Color textColor = good ? Color.Yellow : Color.Red;
            string prefix = good ? "+" : "";
            string postfix = good ? ":)" : ":(";
            IndicatorManager.DrawIndicator(prefix + thought.HappinessModifier + " " + postfix,
                Position + Vector3.Up + MathFunctions.RandVector3Cube()*0.5f, 1.0f, textColor);
        }

        public void Kill(Body entity)
        {
            var killTask = new KillEntityTask(entity, KillEntityTask.KillType.Auto);
            if (!Tasks.Contains(killTask))
                Tasks.Add(killTask);
        }

        public void LeaveWorld()
        {
            Task leaveTask = new LeaveWorldTask
            {
                Priority = Task.PriorityType.Urgent,
                AutoRetry = true,
                Name = "Leave the world."
            };
            Tasks.Add(leaveTask);
        }

        public void UpdateThoughts()
        {
            Thoughts.RemoveAll(thought => thought.IsOver(PlayState.Time.CurrentDate));
            Status.Happiness.CurrentValue = 50.0f;

            foreach (Thought thought in Thoughts)
            {
                Status.Happiness.CurrentValue += thought.HappinessModifier;
            }

            if (Status.IsAsleep)
            {
                AddThought(Thought.ThoughtType.Slept);
            }
            else if (Status.Energy.IsUnhappy())
            {
                AddThought(Thought.ThoughtType.FeltSleepy);
            }

            if (Status.Hunger.IsUnhappy())
            {
                AddThought(Thought.ThoughtType.FeltHungry);
            }
        }

        public void Converse(CreatureAI other)
        {
            if (SpeakTimer.HasTriggered)
            {
                AddThought(Thought.ThoughtType.Talked);
                Creature.DrawIndicator(IndicatorManager.StandardIndicators.Dots);
                Creature.Physics.Face(other.Position);
                SpeakTimer.Reset(SpeakTimer.TargetTimeSeconds);
            }
        }

        public bool HasTaskWithName(Task other)
        {
            return Tasks.Any(task => task.Name == other.Name);
        }

        public void OrderEnemyAttack()
        {
            foreach (CreatureAI enemy in Sensor.Enemies)
            {
                Task task = new KillEntityTask(enemy.Physics, KillEntityTask.KillType.Auto);
                if (!HasTaskWithName(task))
                {
                    Creature.AI.Tasks.Add(task);

                    if (Faction == PlayState.PlayerFaction)
                    {
                        PlayState.AnnouncementManager.Announce(
                            Stats.FullName + Drawer2D.WrapColor(" is fighting ", Color.DarkRed) +
                            TextGenerator.IndefiniteArticle(enemy.Creature.Name),
                            Stats.FullName + " the " + Stats.CurrentLevel.Name + " is fighting " +
                            TextGenerator.IndefiniteArticle(enemy.Stats.CurrentLevel.Name) + " " +
                            enemy.Faction.Race.Name,
                            ZoomToMe);
                    }
                }
            }
        }

        public void AddMoney(float pay)
        {
            Status.Money += pay;
            bool good = pay > 0;
            Color textColor = good ? Color.Green : Color.Red;
            string prefix = good ? "+" : "";
            IndicatorManager.DrawIndicator(prefix + "$" + pay,
                Position + Vector3.Up + MathFunctions.RandVector3Cube()*0.5f, 1.0f, textColor);
        }

        public override string GetDescription()
        {
            string desc = Stats.FullName + ", level " + Stats.CurrentLevel.Index +
                          " " +
                          Stats.CurrentClass.Name + "\n    " +
                          "Happiness: " + Status.Happiness.GetDescription() + ". Health: " + Status.Health.Percentage +
                          ". Hunger: " + (100 - Status.Hunger.Percentage) + ". Energy: " + Status.Energy.Percentage +
                          "\n";
            if (CurrentTask != null)
            {
                desc += "    Task: " + CurrentTask.Name;
            }

            return desc;
        }

        public class LeaveWorldTask : Task
        {
            public IEnumerable<Act.Status> GreedyFallbackBehavior(Creature agent)
            {
                var edgeGoal = new EdgeGoalRegion();

                while (true)
                {
                    Voxel creatureVoxel = agent.Physics.CurrentVoxel;

                    if (edgeGoal.IsInGoalRegion(creatureVoxel))
                    {
                        yield return Act.Status.Success;
                        yield break;
                    }

                    List<Creature.MoveAction> actions = agent.AI.Movement.GetMoveActions(creatureVoxel);

                    float minCost = float.MaxValue;
                    var minAction = new Creature.MoveAction();
                    bool hasMinAction = false;
                    foreach (Creature.MoveAction action in actions)
                    {
                        Voxel vox = action.Voxel;

                        float cost = edgeGoal.Heuristic(vox) + MathFunctions.Rand(0.0f, 5.0f);

                        if (cost < minCost)
                        {
                            minAction = action;
                            minCost = cost;
                            hasMinAction = true;
                        }
                    }

                    if (hasMinAction)
                    {
                        var nullAction = new Creature.MoveAction
                        {
                            Diff = minAction.Diff,
                            MoveType = Creature.MoveType.Walk,
                            Voxel = creatureVoxel
                        };

                        agent.AI.Blackboard.SetData("GreedyPath", new List<Creature.MoveAction> {nullAction, minAction});
                        var pathAct = new FollowPathAnimationAct(agent.AI, "GreedyPath");
                        pathAct.Initialize();

                        foreach (Act.Status status in pathAct.Run())
                        {
                            yield return Act.Status.Running;
                        }
                    }

                    yield return Act.Status.Running;
                }
            }

            public override Act CreateScript(Creature agent)
            {
                return new Select(
                    new GoToVoxelAct("", PlanAct.PlanType.Edge, agent.AI),
                    new Wrap(() => GreedyFallbackBehavior(agent))
                    );
            }

            public override Task Clone()
            {
                return new LeaveWorldTask();
            }
        }

        public class TaskHistory
        {
            public static float LockoutTime;
            public static int MaxFailures;
            public Timer LockoutTimer;
            public int NumFailures;

            static TaskHistory()
            {
                LockoutTime = 30.0f;
                MaxFailures = 3;
            }

            public TaskHistory()
            {
                NumFailures = 0;
                LockoutTimer = new Timer(LockoutTime, true);
            }

            public bool IsLocked
            {
                get { return NumFailures >= MaxFailures && !LockoutTimer.HasTriggered; }
            }

            public void Update()
            {
                LockoutTimer.Update(DwarfTime.LastTime);
                if (LockoutTimer.HasTriggered)
                {
                    LockoutTimer.Reset(LockoutTime*1.5f);
                    NumFailures = 0;
                }
            }
        }
    }

    public class CreatureMovement
    {
        public CreatureMovement(Creature creature)
        {
            Actions = new Dictionary<Creature.MoveType, ActionStats>
            {
                {
                    Creature.MoveType.Climb,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 2.0f,
                        Speed = 0.5f
                    }
                },
                {
                    Creature.MoveType.Walk,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 1.0f,
                        Speed = 1.0f
                    }
                },
                {
                    Creature.MoveType.Swim,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 2.0f,
                        Speed = 0.5f
                    }
                },
                {
                    Creature.MoveType.Jump,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 1.0f,
                        Speed = 1.0f
                    }
                },
                {
                    Creature.MoveType.Fly,
                    new ActionStats
                    {
                        CanMove = false,
                        Cost = 1.0f,
                        Speed = 1.0f
                    }
                },
                {
                    Creature.MoveType.Fall,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 5.0f,
                        Speed = 1.0f
                    }
                },
                {
                    Creature.MoveType.DestroyObject,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 30.0f,
                        Speed = 1.0f
                    }
                },
                {
                    Creature.MoveType.ClimbWalls,
                    new ActionStats
                    {
                        CanMove = false,
                        Cost = 10.0f,
                        Speed = 1.0f
                    }
                },
            };
        }

        public Creature Creature { get; set; }

        [JsonIgnore]
        public bool CanFly
        {
            get { return Can(Creature.MoveType.Fly); }
            set { SetCan(Creature.MoveType.Fly, value); }
        }

        [JsonIgnore]
        public bool CanSwim
        {
            get { return Can(Creature.MoveType.Swim); }
            set { SetCan(Creature.MoveType.Swim, value); }
        }

        [JsonIgnore]
        public bool CanClimb
        {
            get { return Can(Creature.MoveType.Climb); }
            set { SetCan(Creature.MoveType.Climb, value); }
        }

        [JsonIgnore]
        public bool CanClimbWalls
        {
            get { return Can(Creature.MoveType.ClimbWalls); }
            set { SetCan(Creature.MoveType.ClimbWalls, value); }
        }

        [JsonIgnore]
        public bool CanWalk
        {
            get { return Can(Creature.MoveType.Walk); }
            set { SetCan(Creature.MoveType.Walk, value); }
        }

        public Dictionary<Creature.MoveType, ActionStats> Actions { get; set; }

        public bool Can(Creature.MoveType type)
        {
            return Actions[type].CanMove;
        }

        public float Cost(Creature.MoveType type)
        {
            return Actions[type].Cost;
        }

        public float Speed(Creature.MoveType type)
        {
            return Actions[type].Speed;
        }

        public void SetCan(Creature.MoveType type, bool value)
        {
            Actions[type].CanMove = value;
        }

        public void SetCost(Creature.MoveType type, float value)
        {
            Actions[type].Cost = value;
        }

        public void SetSpeed(Creature.MoveType type, float value)
        {
            Actions[type].Speed = value;
        }

        private Voxel[,,] GetNeighborhood(Voxel voxel)
        {
            var neighborHood = new Voxel[3, 3, 3];
            CollisionManager objectHash = PlayState.ComponentManager.CollisionManager;

            VoxelChunk startChunk = voxel.Chunk;
            var x = (int) voxel.GridPosition.X;
            var y = (int) voxel.GridPosition.Y;
            var z = (int) voxel.GridPosition.Z;
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    for (int dz = -1; dz < 2; dz++)
                    {
                        neighborHood[dx + 1, dy + 1, dz + 1] = new Voxel();
                        int nx = dx + x;
                        int ny = dy + y;
                        int nz = dz + z;
                        if (
                            !PlayState.ChunkManager.ChunkData.GetVoxel(startChunk,
                                new Vector3(nx, ny, nz) + startChunk.Origin,
                                ref neighborHood[dx + 1, dy + 1, dz + 1]))
                        {
                            neighborHood[dx + 1, dy + 1, dz + 1] = null;
                        }
                    }
                }
            }
            return neighborHood;
        }

        private bool HasNeighbors(Voxel[,,] neighborHood)
        {
            bool hasNeighbors = false;
            for (int dx = 0; dx < 3; dx++)
            {
                for (int dz = 0; dz < 3; dz++)
                {
                    if (dx == 1 && dz == 1)
                    {
                        continue;
                    }

                    hasNeighbors = hasNeighbors ||
                                   (neighborHood[dx, 1, dz] != null && (!neighborHood[dx, 1, dz].IsEmpty));
                }
            }


            return hasNeighbors;
        }

        private bool IsEmpty(Voxel v)
        {
            return v == null || v.IsEmpty;
        }

        public List<Creature.MoveAction> GetMoveActions(Vector3 pos)
        {
            var vox = new Voxel();
            PlayState.ChunkManager.ChunkData.GetVoxel(pos, ref vox);
            return GetMoveActions(vox);
        }

        public List<Creature.MoveAction> GetMoveActions(Voxel voxel)
        {
            var toReturn = new List<Creature.MoveAction>();

            CollisionManager objectHash = PlayState.ComponentManager.CollisionManager;

            Voxel[,,] neighborHood = GetNeighborhood(voxel);
            var x = (int) voxel.GridPosition.X;
            var y = (int) voxel.GridPosition.Y;
            var z = (int) voxel.GridPosition.Z;
            bool inWater = (neighborHood[1, 1, 1] != null && neighborHood[1, 1, 1].WaterLevel > 5);
            bool standingOnGround = (neighborHood[1, 0, 1] != null && !neighborHood[1, 0, 1].IsEmpty);
            bool topCovered = (neighborHood[1, 2, 1] != null && !neighborHood[1, 2, 1].IsEmpty);
            bool hasNeighbors = HasNeighbors(neighborHood);
            bool isClimbing = false;

            var successors = new List<Creature.MoveAction>();

            //Climbing ladders
            IEnumerable<IBoundedObject> objectsInside = objectHash.GetObjectsAt(voxel,
                CollisionManager.CollisionType.Static);
            if (objectsInside != null)
            {
                IEnumerable<GameComponent> bodies = objectsInside.OfType<GameComponent>();
                IList<GameComponent> enumerable = bodies as IList<GameComponent> ?? bodies.ToList();
                if (CanClimb)
                {
                    bool hasLadder = enumerable.Any(component => component.Tags.Contains("Climbable"));
                    if (hasLadder)
                    {
                        successors.Add(new Creature.MoveAction
                        {
                            Diff = new Vector3(1, 2, 1),
                            MoveType = Creature.MoveType.Climb
                        });

                        isClimbing = true;

                        if (!standingOnGround)
                        {
                            successors.Add(new Creature.MoveAction
                            {
                                Diff = new Vector3(1, 0, 1),
                                MoveType = Creature.MoveType.Climb
                            });
                        }

                        standingOnGround = true;
                    }
                }
            }

            if (CanClimbWalls && !topCovered)
            {
                bool nearWall = (neighborHood[2, 1, 1] != null && !neighborHood[2, 1, 1].IsEmpty) ||
                                (neighborHood[0, 1, 1] != null && !neighborHood[0, 1, 1].IsEmpty) ||
                                (neighborHood[1, 1, 2] != null && !neighborHood[1, 1, 2].IsEmpty) ||
                                (neighborHood[1, 1, 0] != null && !neighborHood[1, 1, 0].IsEmpty);

                if (nearWall)
                {
                    isClimbing = true;
                    successors.Add(new Creature.MoveAction
                    {
                        Diff = new Vector3(1, 2, 1),
                        MoveType = Creature.MoveType.ClimbWalls
                    });
                }

                if (nearWall && !standingOnGround)
                {
                    successors.Add(new Creature.MoveAction
                    {
                        Diff = new Vector3(1, 0, 1),
                        MoveType = Creature.MoveType.ClimbWalls
                    });
                }
            }

            if ((CanWalk && standingOnGround) || (CanSwim && inWater))
            {
                Creature.MoveType moveType = inWater ? Creature.MoveType.Swim : Creature.MoveType.Walk;
                if (IsEmpty(neighborHood[0, 1, 1]))
                    // +- x
                    successors.Add(new Creature.MoveAction
                    {
                        Diff = new Vector3(0, 1, 1),
                        MoveType = moveType
                    });

                if (IsEmpty(neighborHood[2, 1, 1]))
                    successors.Add(new Creature.MoveAction
                    {
                        Diff = new Vector3(2, 1, 1),
                        MoveType = moveType
                    });

                if (IsEmpty(neighborHood[1, 1, 0]))
                    // +- z
                    successors.Add(new Creature.MoveAction
                    {
                        Diff = new Vector3(1, 1, 0),
                        MoveType = moveType
                    });

                if (IsEmpty(neighborHood[1, 1, 2]))
                    successors.Add(new Creature.MoveAction
                    {
                        Diff = new Vector3(1, 1, 2),
                        MoveType = moveType
                    });

                if (!hasNeighbors)
                {
                    if (IsEmpty(neighborHood[2, 1, 2]))
                        // +x + z
                        successors.Add(new Creature.MoveAction
                        {
                            Diff = new Vector3(2, 1, 2),
                            MoveType = moveType
                        });

                    if (IsEmpty(neighborHood[2, 1, 0]))
                        successors.Add(new Creature.MoveAction
                        {
                            Diff = new Vector3(2, 1, 0),
                            MoveType = moveType
                        });

                    if (IsEmpty(neighborHood[0, 1, 2]))
                        // -x -z
                        successors.Add(new Creature.MoveAction
                        {
                            Diff = new Vector3(0, 1, 2),
                            MoveType = moveType
                        });

                    if (IsEmpty(neighborHood[0, 1, 0]))
                        successors.Add(new Creature.MoveAction
                        {
                            Diff = new Vector3(0, 1, 0),
                            MoveType = moveType
                        });
                }
            }

            if (!topCovered && (standingOnGround || (CanSwim && inWater) || isClimbing))
            {
                for (int dx = 0; dx <= 2; dx++)
                {
                    for (int dz = 0; dz <= 2; dz++)
                    {
                        if (dx == 1 && dz == 1) continue;

                        if (!IsEmpty(neighborHood[dx, 1, dz]))
                        {
                            successors.Add(new Creature.MoveAction
                            {
                                Diff = new Vector3(dx, 2, dz),
                                MoveType = Creature.MoveType.Jump
                            });
                        }
                    }
                }
            }


            // Falling
            if (!inWater && !standingOnGround)
            {
                successors.Add(new Creature.MoveAction
                {
                    Diff = new Vector3(1, 0, 1),
                    MoveType = Creature.MoveType.Fall
                });
            }

            if (CanFly && !inWater)
            {
                for (int dx = 0; dx <= 2; dx++)
                {
                    for (int dz = 0; dz <= 2; dz++)
                    {
                        for (int dy = 0; dy <= 2; dy++)
                        {
                            if (dx == 1 && dz == 1 && dy == 1) continue;

                            if (IsEmpty(neighborHood[dx, 1, dz]))
                            {
                                successors.Add(new Creature.MoveAction
                                {
                                    Diff = new Vector3(dx, dy, dz),
                                    MoveType = Creature.MoveType.Fly
                                });
                            }
                        }
                    }
                }
            }


            foreach (Creature.MoveAction v in successors)
            {
                Voxel n = neighborHood[(int) v.Diff.X, (int) v.Diff.Y, (int) v.Diff.Z];
                if (n != null && (n.IsEmpty || n.WaterLevel > 0))
                {
                    bool blockedByObject = false;
                    List<IBoundedObject> objectsAtNeighbor = PlayState.ComponentManager.CollisionManager.GetObjectsAt(
                        n, CollisionManager.CollisionType.Static);

                    if (objectsAtNeighbor != null)
                    {
                        IEnumerable<GameComponent> bodies = objectsAtNeighbor.OfType<GameComponent>();
                        IList<GameComponent> enumerable = bodies as IList<GameComponent> ?? bodies.ToList();

                        foreach (GameComponent body in enumerable)
                        {
                            Door door = body.GetRootComponent().GetChildrenOfType<Door>(true).FirstOrDefault();

                            if (door != null)
                            {
                                if (
                                    PlayState.ComponentManager.Diplomacy.GetPolitics(door.TeamFaction, Creature.Faction)
                                        .GetCurrentRelationship() !=
                                    Relationship.Loving)
                                {
                                    if (Can(Creature.MoveType.DestroyObject))
                                        toReturn.Add(new Creature.MoveAction
                                        {
                                            Diff = v.Diff,
                                            MoveType = Creature.MoveType.DestroyObject,
                                            InteractObject = door,
                                            Voxel = n
                                        });
                                    blockedByObject = true;
                                }
                            }
                        }
                    }
                    if (!blockedByObject)
                    {
                        Creature.MoveAction newAction = v;
                        newAction.Voxel = n;
                        toReturn.Add(newAction);
                    }
                }
            }


            return toReturn;
        }

        public class ActionStats
        {
            public bool CanMove = false;
            public float Cost = 1.0f;
            public float Speed = 1.0f;
        }
    }
}