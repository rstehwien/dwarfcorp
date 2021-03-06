﻿// GuardVoxelAct.cs
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

using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     A creature goes to a voxel, and then waits there until cancelled.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class GuardVoxelAct : CompoundCreatureAct
    {
        public GuardVoxelAct()
        {
        }

        public GuardVoxelAct(CreatureAI agent, Voxel voxel) :
            base(agent)
        {
            Voxel = voxel;
            Name = "Guard Voxel " + voxel;

            Tree = new Sequence
                (
                new GoToVoxelAct(voxel, PlanAct.PlanType.Adjacent, agent),
                new StopAct(Agent),
                new WhileLoop(new WanderAct(Agent, 1.0f, 0.5f, 0.1f), new Condition(LoopCondition)),
                new Condition(ExitCondition)
                );
        }

        public Voxel Voxel { get; set; }

        public bool LoopCondition()
        {
            return Agent.Faction.IsGuardDesignation(Voxel) && !EnemiesNearby() && !Creature.Status.Energy.IsUnhappy() &&
                   !Creature.Status.Hunger.IsUnhappy();
        }

        public bool GuardDesignationExists()
        {
            return Agent.Faction.IsGuardDesignation(Voxel);
        }

        public bool ExitCondition()
        {
            if (EnemiesNearby())
            {
                Creature.AI.OrderEnemyAttack();
            }

            return !GuardDesignationExists();
        }


        public bool EnemiesNearby()
        {
            return (Agent.Sensor.Enemies.Count > 0);
        }
    }
}