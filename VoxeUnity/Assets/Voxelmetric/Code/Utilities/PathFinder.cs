using System;
using System.Collections.Generic;
using Voxelmetric.Code.Common.Threading;
using Voxelmetric.Code.Common.Threading.Managers;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Utilities
{
    public class PathFinder
    {
        private const float maxDistToTravelAfterDirect = 800;
        private const float maxDistToTravelMultiplier = 10;

        public readonly List<Vector3Int> path;

        private readonly World world;
        private readonly Dictionary<Vector3Int, Heuristics> open;
        private readonly Dictionary<Vector3Int, Heuristics> closed;
        
        private readonly Vector3Int target;
        private readonly Vector3Int start;

        private readonly int entityHeight;
        private readonly float range = 0.5f;
        private readonly float distanceFromStartToTarget = 0;

        public enum Status { stopped, working, failed, succeeded };
        public Status status;

        private struct Heuristics
        {
            //! Parent block position
            public readonly Vector3Int parent;
            //! Real distance from start
            public readonly float g;
            //! Estimated distance to target
            public readonly float h;

            public Heuristics(float g, float h, Vector3Int parent)
            {
                this.g = g;
                this.h = h;
                this.parent = parent;
            }
        }

        public PathFinder(Vector3Int start, Vector3Int target, World world, float range = 0.5f, int entityHeight=1)
        {
            // Don't search the path if our target is too close
            if (start.Distance2(ref target)<=range*range)
                return;
            
            path = new List<Vector3Int>();
            open = new Dictionary<Vector3Int, Heuristics>();
            closed = new Dictionary<Vector3Int, Heuristics>();

            this.world = world;
            this.range = range;
            this.entityHeight = entityHeight;
            this.start = start;
            this.target = target;

            distanceFromStartToTarget = start.Distance2(ref target);
            open.Add(start, new Heuristics(0, distanceFromStartToTarget, start));
            status = Status.working;
            WorkPoolManager.Add(
                new ThreadPoolItem<PathFinder>(
                    Globals.WorkPool,
                    arg =>
                    {
                        arg.ComputePath();
                    },
                    this
                ),
                false);
        }

        private void ComputePath()
        {
            while (status == Status.working)
            {
                ProcessBest();
            }
        }

        private void PathComplete(Vector3Int lastTile)
        {
            Heuristics pos;
            closed.TryGetValue(lastTile, out pos);
            path.Clear();
            path.Add(lastTile + Direction.up);

            open.TryGetValue(lastTile, out pos);

            while (pos.parent!=start)
            {
                path.Insert(0, pos.parent + Direction.up);
                if (!closed.TryGetValue(pos.parent, out pos))
                    break;
            }

        }

        private static readonly Vector3Int FailedPos = new Vector3Int(0, int.MaxValue, 0);

        private void ProcessBest()
        {
            float shortestDist = distanceFromStartToTarget*maxDistToTravelMultiplier + maxDistToTravelAfterDirect;
            Vector3Int bestPos = FailedPos;

            foreach (var tile in open)
            {
                if (tile.Value.g + tile.Value.h < shortestDist)
                {
                    bestPos = tile.Key;
                    shortestDist = tile.Value.g + tile.Value.h;
                }
            }

            Heuristics parent;
            open.TryGetValue(bestPos, out parent);

            if (target.Distance2(ref bestPos) <= range*range)
            {
                PathComplete(bestPos);
                status = Status.succeeded;
                return;
            }

            if (bestPos==FailedPos)
            {
                status = Status.failed;
            }

            ProcessTile(bestPos);
        }

        private void ProcessTile(Vector3Int pos)
        {
            Heuristics h;
            bool exists = open.TryGetValue(pos, out h);

            if (!exists)
                return;

            open.Remove(pos);
            closed.Add(pos, h);

            CheckAdjacent(pos, h);
        }

        [ThreadStatic] private static Vector3Int[] adjacentPositions;
        [ThreadStatic] private static float[] distanceFromStart;

        private void CheckAdjacent(Vector3Int pos, Heuristics dist)
        {
            {
                if (adjacentPositions==null)
                    adjacentPositions = new Vector3Int[12]; // 16 for diagonal directions
                
                // Cardinal directions
                adjacentPositions[0] = new Vector3Int(pos.x, pos.y, pos.z+1);
                adjacentPositions[1] = new Vector3Int(pos.x+1, pos.y, pos.z);
                adjacentPositions[2] = new Vector3Int(pos.x, pos.y, pos.z-1);
                adjacentPositions[3] = new Vector3Int(pos.x-1, pos.y, pos.z);
                // Climb up directions
                adjacentPositions[4] = new Vector3Int(pos.x, pos.y+1, pos.z+1);
                adjacentPositions[5] = new Vector3Int(pos.x+1, pos.y+1, pos.z);
                adjacentPositions[6] = new Vector3Int(pos.x, pos.y+1, pos.z-1);
                adjacentPositions[7] = new Vector3Int(pos.x-1, pos.y+1, pos.z);
                // Climb down directions
                adjacentPositions[8] = new Vector3Int(pos.x, pos.y-1, pos.z+1);
                adjacentPositions[9] = new Vector3Int(pos.x+1, pos.y-1, pos.z);
                adjacentPositions[10] = new Vector3Int(pos.x, pos.y-1, pos.z-1);
                adjacentPositions[11] = new Vector3Int(pos.x-1, pos.y-1, pos.z);
                // Diagonal directions
                //adjacentPositions[12] = new Vector3Int(pos.x+1, pos.y, pos.z+1);
                //adjacentPositions[13] = new Vector3Int(pos.x+1, pos.y, pos.z-1);
                //adjacentPositions[14] = new Vector3Int(pos.x-1, pos.y, pos.z-1);
                //adjacentPositions[15] = new Vector3Int(pos.x-1, pos.y, pos.z+1);
            }

            {
                if (distanceFromStart==null)
                    distanceFromStart = new float[12]; // 16 for diagonal directions
                
                // Cardinal directions
                distanceFromStart[0] = dist.g+1;
                distanceFromStart[1] = dist.g+1;
                distanceFromStart[2] = dist.g+1;
                distanceFromStart[3] = dist.g+1;
                // Climb up directions
                distanceFromStart[4] = dist.g+1.414f;
                distanceFromStart[5] = dist.g+1.414f;
                distanceFromStart[6] = dist.g+1.414f;
                distanceFromStart[7] = dist.g+1.414f;
                // Climb down directions
                distanceFromStart[8] = dist.g+1.414f;
                distanceFromStart[9] = dist.g+1.414f;
                distanceFromStart[10] = dist.g+1.414f;
                distanceFromStart[11] = dist.g+1.414f;
                // Diagonal directions
                //distanceFromStart[12] = dist.g+1.414f;
                //distanceFromStart[13] = dist.g+1.414f;
                //distanceFromStart[14] = dist.g+1.414f;
                //distanceFromStart[15] = dist.g+1.414f;
            }

            for (int i = 0; i<12/*16*/; i++)
            {
                if (!closed.ContainsKey(adjacentPositions[i]))
                {
                    Vector3Int adjPos = adjacentPositions[i];

                    var h = new Heuristics(
                        distanceFromStart[i],
                        target.Distance2(ref adjPos),
                        pos);

                    if (IsWalkable(world, ref adjPos))
                    {
                        Heuristics existingTile;
                        if (open.TryGetValue(adjacentPositions[i], out existingTile))
                        {
                            if (existingTile.g>distanceFromStart[i])
                            {
                                open.Remove(adjacentPositions[i]);
                                open.Add(adjacentPositions[i], h);
                            }
                        }
                        else
                        {
                            open.Add(adjacentPositions[i], h);
                        }
                    }
                }
            }
        }

        public bool IsWalkable(World world, ref Vector3Int pos)
        {
            Block block = world.blocks.GetBlock(ref pos);
            if (!block.CanCollide)
                return false;

            // There has to be enough free space above the position
            for (int y = 1; y<=entityHeight; y++)
            {
                Vector3Int blockPos = pos.Add(0, y, 0);
                block = world.blocks.GetBlock(ref blockPos);
                if (block.CanCollide)
                    return false;
            }

            return true;

        }
    }
}
