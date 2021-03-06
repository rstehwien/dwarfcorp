﻿// AStarPlanner.cs
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

namespace DwarfCorp
{
    /// <summary>
    ///     The classic A star planner, but singled out for use with voxels. It should
    ///     probably be made generic. It is called in a thread so that it can return new paths at its
    ///     own leisurely pace. Plans are then returned via delegate.
    ///     The A* planner uses a graph where nodes are Voxels, and the edges between nodes are Movements.
    ///     A Movement could be something like "walk", "jump" or "fly". The planner finds the shortest list of
    ///     Movements that go from a start Voxel to a goal Voxel.
    /// </summary>
    internal class AStarPlanner
    {
        /// <summary>
        ///     Gets the Voxel that has minimum expansion score. Expands this voxel.
        /// </summary>
        /// <param name="fScore">Queue of voxels by their expansion scores.</param>
        /// <returns>The Voxel with minimu expansion score.</returns>
        public static Voxel GetVoxelWithMinimumFScore(PriorityQueue<Voxel> fScore)
        {
            return fScore.Dequeue();
        }


        /// <summary>
        ///     Creates a path of Movements starting from some end movement to the start.
        /// </summary>
        /// <param name="cameFrom">A dictionary of Voxels to the movement that was taken to get there.</param>
        /// <param name="currentNode">The very last movement in the path.</param>
        /// <returns>The path of movements. from the start to the current node</returns>
        public static List<Creature.MoveAction> ReconstructPath(Dictionary<Voxel, Creature.MoveAction> cameFrom,
            Creature.MoveAction currentNode)
        {
            var toReturn = new List<Creature.MoveAction>();
            // If there is a dictionary entry for the current voxel, add it to the path recursively.
            if (cameFrom.ContainsKey(currentNode.Voxel))
            {
                toReturn.AddRange(ReconstructPath(cameFrom, cameFrom[currentNode.Voxel]));
                toReturn.Add(currentNode);
                return toReturn;
            }
            // Otherwise, this is the start node. Add it to the path and return.
            toReturn.Add(currentNode);
            return toReturn;
        }


        /// <summary>
        ///     Creates a path from the start voxel to some goal region, returning a list of movements that can
        ///     take a mover from the start voxel to the goal region.
        /// </summary>
        /// <param name="mover">The agent that we want to find a path for.</param>
        /// <param name="start">The voxel that the agent starts in.</param>
        /// <param name="goal">Goal conditions that the agent is trying to satisfy.</param>
        /// <param name="chunks">The voxels that the agent is moving through</param>
        /// <param name="maxExpansions">Maximum number of voxels to consider before giving up.</param>
        /// <param name="toReturn">The path of movements that the mover should take to reach the goal.</param>
        /// <param name="weight">
        ///     A heuristic weight to apply to the planner. If 1.0, the path is garunteed to be optimal. Higher values
        ///     usually result in faster plans that are suboptimal.
        /// </param>
        /// <returns>True if a path could be found, or false otherwise.</returns>
        private static bool Path(CreatureMovement mover, Voxel start, GoalRegion goal, ChunkManager chunks,
            int maxExpansions, ref List<Creature.MoveAction> toReturn, float weight)
        {
            // Sometimes a goal may not even be achievable a.priori. If this is true, we know there can't be a path 
            // which satisifies that goal.
            if (!goal.IsPossible())
            {
                toReturn = null;
                return false;
            }

            // Voxels that have already been explored.
            var closedSet = new HashSet<Voxel>();

            // Voxels which should be explored.
            var openSet = new HashSet<Voxel>
            {
                start
            };

            // Dictionary of voxels to the optimal action that got the mover to that voxel.
            var cameFrom = new Dictionary<Voxel, Creature.MoveAction>();

            // Optimal score of a voxel based on the path it took to get there.
            var gScore = new Dictionary<Voxel, float>();

            // Expansion priority of voxels.
            var fScore = new PriorityQueue<Voxel>();

            // Starting conditions of the search.
            gScore[start] = 0.0f;
            fScore.Enqueue(start, gScore[start] + weight*goal.Heuristic(start));

            // Keep count of the number of expansions we've taken to get to the goal.
            int numExpansions = 0;

            // Check the voxels adjacent to the current voxel as a quick test of adjacency to the goal.
            var manhattanNeighbors = new List<Voxel>(6);
            for (int i = 0; i < 6; i++)
            {
                manhattanNeighbors.Add(new Voxel());
            }

            // Loop until we've either checked every possible voxel, or we've exceeded the maximum number of
            // expansions.
            while (openSet.Count > 0 && numExpansions < maxExpansions)
            {
                // Check the next voxel to explore.
                Voxel current = GetVoxelWithMinimumFScore(fScore);
                if (current == null)
                {
                    // If there wasn't a voxel to explore, just try to expand from
                    // the start again.
                    current = start;
                    numExpansions++;
                }
                numExpansions++;

                // If we've reached the goal already, reconstruct the path from the start to the 
                // goal.
                if (goal.IsInGoalRegion(current))
                {
                    // Assume that the last action in the path involves walking to the goal.
                    var first = new Creature.MoveAction
                    {
                        Voxel = current,
                        MoveType = Creature.MoveType.Walk
                    };
                    toReturn = ReconstructPath(cameFrom, first);
                    return true;
                }

                // We've already considered the voxel, so add it to the closed set.
                openSet.Remove(current);
                closedSet.Add(current);

                VoxelChunk currentChunk = chunks.ChunkData.ChunkMap[current.ChunkID];

                List<Creature.MoveAction> neighbors = null;

                // Get the voxels that can be moved to from the current voxel.
                neighbors = mover.GetMoveActions(current);
                currentChunk.GetNeighborsManhattan(current, manhattanNeighbors);

                // A quick test to see if we're already adjacent to the goal. If we are, assume
                // that we can just walk to it.
                if (manhattanNeighbors.Contains(goal.GetVoxel()))
                {
                    var first = new Creature.MoveAction
                    {
                        Voxel = current,
                        MoveType = Creature.MoveType.Walk
                    };
                    var last = new Creature.MoveAction
                    {
                        Voxel = goal.GetVoxel(),
                        MoveType = Creature.MoveType.Walk
                    };
                    List<Creature.MoveAction> subPath = ReconstructPath(cameFrom, first);
                    subPath.Add(last);
                    toReturn = subPath;
                    return true;
                }

                // Otherwise, consider all of the neighbors of the current voxel that can be moved to,
                // and determine how to add them to the list of expansions.
                foreach (Creature.MoveAction n in neighbors)
                {
                    // If we've already explored that voxel, don't explore it again.
                    if (closedSet.Contains(n.Voxel))
                    {
                        continue;
                    }

                    // Otherwise, consider the case of moving to that neighbor.
                    float tenativeGScore = gScore[current] + GetDistance(current, n.Voxel, n.MoveType, mover);

                    // IF the neighbor can already be reached more efficiently, ignore it.
                    if (openSet.Contains(n.Voxel) && !(tenativeGScore < gScore[n.Voxel]))
                    {
                        continue;
                    }

                    // Otherwise, add it to the list of voxels for consideration.
                    openSet.Add(n.Voxel);

                    // Add an edge to the voxel from the current voxel.
                    Creature.MoveAction cameAction = n;
                    cameAction.Voxel = current;
                    cameFrom[n.Voxel] = cameAction;

                    // Update the expansion scores for the next voxel.
                    gScore[n.Voxel] = tenativeGScore;
                    fScore.Enqueue(n.Voxel, gScore[n.Voxel] + weight*goal.Heuristic(n.Voxel));
                }

                // If we've expanded too many voxels, just give up.
                if (numExpansions >= maxExpansions)
                {
                    return false;
                }
            }

            // Somehow we've reached this code without having found a path. Return false.
            toReturn = null;
            return false;
        }


        /// <summary>
        ///     Finds the path from the start to the goal region of move actions that can be performed by the creature.
        /// </summary>
        /// <param name="mover">The creature following the path.</param>
        /// <param name="start">The voxel the path starts with.</param>
        /// <param name="goal">Goal conditions that must be satisfied by the path.</param>
        /// <param name="chunks">The chunks the creature is moving through.</param>
        /// <param name="maxExpansions">Maximum number of voxels to explore before giving up.</param>
        /// <param name="weight">
        ///     The heuristic weight of the planner. If 1.0, the path returned is optimal.
        ///     Higher values result in suboptimal paths, but the search may be faster.
        /// </param>
        /// <returns>The path of movements the creature must take to reach the goal. Returns null if no such path exists.</returns>
        public static List<Creature.MoveAction> FindPath(CreatureMovement mover, Voxel start, GoalRegion goal,
            ChunkManager chunks, int maxExpansions, float weight)
        {
            var p = new List<Creature.MoveAction>();
            bool success = Path(mover, start, goal, chunks, maxExpansions, ref p, weight);

            if (success)
            {
                return p;
            }
            return null;
        }

        /// <summary>
        ///     Given two voxels, and an action taken between the voxels, returns the cost of moving
        ///     between the two voxels given that action.
        /// </summary>
        /// <param name="a">The source voxel of the action.</param>
        /// <param name="b">The destination voxel of the action.</param>
        /// <param name="action">The action taken to get between voxels.</param>
        /// <param name="movement">The creature making the movement.</param>
        /// <returns>The cost of going from a to b using the given action.</returns>
        public static float GetDistance(Voxel a, Voxel b, Creature.MoveType action, CreatureMovement movement)
        {
            // If trying to move through a non-empty voxel, the cost is  just a big number.
            if (!b.IsEmpty)
            {
                return 100000;
            }
            // Otherwise, the cost is the distance between the voxels multiplied by the intrinsic cost
            // of an action.
            float score = (a.Position - b.Position).LengthSquared()*ActionCost(movement, action);

            return score;
        }

        /// <summary>
        ///     Returns the intrinsic cost of an action.
        /// </summary>
        private static float ActionCost(CreatureMovement movement, Creature.MoveType action)
        {
            return movement.Cost(action);
        }
    }
}