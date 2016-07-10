using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Common.Threading;
using Voxelmetric.Code.Common.Threading.Managers;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Utilities
{
    public class PathFinder {

        World world;
        Dictionary<Vector3Int, Heuristics> open = new Dictionary<Vector3Int, Heuristics>();
        Dictionary<Vector3Int, Heuristics> closed = new Dictionary<Vector3Int, Heuristics>();

        public List<Vector3Int> path = new List<Vector3Int>();

        Vector3Int targetLocation;
        Vector3Int startLocation;

        int entityHeight;

        public float range = 0.5f;
        float distanceFromStartToTarget = 0;
        float maxDistToTravelAfterDirect = 800;
        float maxDistToTravelMultiplier = 10;

        public enum Status { stopped, working, failed, succeeded };

        public Status status;

        struct Heuristics
        {
            /// Real distance from start
            public float g;
            /// Estimated distance to target
            public float h;

            public Vector3Int parent;

            public Heuristics(float g, float h, Vector3Int parent)
            {
                this.g = g;
                this.h = h;
                this.parent = parent;
            }
        };

        public PathFinder(Vector3Int start, Vector3Int target, World world, float range = 0.5f, int entityHeight=1)
        {
            status = Status.working;
            this.range = range;
            startLocation = start.Add(Direction.down);
            targetLocation = target.Add(Direction.down);
            distanceFromStartToTarget = Distance(startLocation, targetLocation);
            this.world = world;
            this.entityHeight = entityHeight;

            open.Add(startLocation, new Heuristics(0, distanceFromStartToTarget, startLocation));

            WorkPoolManager.Add(
                new ThreadPoolItem(
                    Globals.WorkPool,
                    arg =>
                    {
                        PathFinder pf = arg as PathFinder;
                        pf.ComputePath();
                    },
                    this
                ));
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
            path.Add(lastTile.Add(Direction.up));

            open.TryGetValue(lastTile, out pos);

            while (!pos.parent.Equals(startLocation))
            {
                path.Insert(0, pos.parent.Add(Direction.up));
                if (!closed.TryGetValue(pos.parent, out pos))
                    break;
            }

        }

        private static Vector3Int FailedPos = new Vector3Int(0, int.MaxValue, 0);

        private void ProcessBest()
        {
            float shortestDist = (distanceFromStartToTarget*maxDistToTravelMultiplier) + maxDistToTravelAfterDirect;
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

            if (Distance(((Vector3)bestPos), targetLocation) <= range)
            {
                PathComplete(bestPos);
                status = Status.succeeded;
                return;
            }

            if (bestPos.Equals(FailedPos))
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

        private void CheckAdjacent(Vector3Int pos, Heuristics dist)
        {
            List<Vector3Int> adjacentPositions = new List<Vector3Int>();
            List<float> distanceFromStart= new List<float>();

            //Cardinal directions
            adjacentPositions.Add(new Vector3Int(pos.x, pos.y, pos.z + 1));
            distanceFromStart.Add(dist.g +1);
            adjacentPositions.Add(new Vector3Int(pos.x + 1, pos.y, pos.z));
            distanceFromStart.Add(dist.g +1);
            adjacentPositions.Add(new Vector3Int(pos.x, pos.y, pos.z - 1));
            distanceFromStart.Add(dist.g +1);
            adjacentPositions.Add(new Vector3Int(pos.x - 1, pos.y, pos.z));
            distanceFromStart.Add(dist.g +1);

            ////diagonal directions
            //adjacentPositions.Add(new BlockPos(pos.x + 1, pos.y, pos.z + 1));
            //distanceFromStart.Add(dist.g +1.414f);
            //adjacentPositions.Add(new BlockPos(pos.x + 1, pos.y, pos.z - 1));
            //distanceFromStart.Add(dist.g +1.414f);
            //adjacentPositions.Add(new BlockPos(pos.x - 1, pos.y, pos.z - 1));
            //distanceFromStart.Add(dist.g +1.414f);
            //adjacentPositions.Add(new BlockPos(pos.x - 1, pos.y, pos.z + 1));
            //distanceFromStart.Add(dist.g +1.414f);

            //climb up directions
            adjacentPositions.Add(new Vector3Int(pos.x, pos.y+1, pos.z+1));
            distanceFromStart.Add(dist.g + 1.414f);
            adjacentPositions.Add(new Vector3Int(pos.x+1, pos.y+1, pos.z));
            distanceFromStart.Add(dist.g + 1.414f);
            adjacentPositions.Add(new Vector3Int(pos.x, pos.y+1, pos.z-1));
            distanceFromStart.Add(dist.g + 1.414f);
            adjacentPositions.Add(new Vector3Int(pos.x-1, pos.y+1, pos.z));
            distanceFromStart.Add(dist.g + 1.414f);

            //climb down directions
            adjacentPositions.Add(new Vector3Int(pos.x, pos.y-1, pos.z+1));
            distanceFromStart.Add(dist.g + 1.414f);
            adjacentPositions.Add(new Vector3Int(pos.x+1, pos.y-1, pos.z));
            distanceFromStart.Add(dist.g + 1.414f);
            adjacentPositions.Add(new Vector3Int(pos.x, pos.y-1, pos.z-1));
            distanceFromStart.Add(dist.g + 1.414f);
            adjacentPositions.Add(new Vector3Int(pos.x-1, pos.y-1, pos.z));
            distanceFromStart.Add(dist.g + 1.414f);

            for (int i = 0; i<adjacentPositions.Count; i++)
            {
                if(!closed.ContainsKey(adjacentPositions[i])){

                    var h = new Heuristics(
                        distanceFromStart[i],
                        Distance(targetLocation,
                                 adjacentPositions[i]),
                        pos);

                    if (IsWalkable(world, adjacentPositions[i]))
                    {

                        Heuristics existingTile;
                        if (open.TryGetValue(adjacentPositions[i], out existingTile))
                        {
                            if(existingTile.g > distanceFromStart[i]){
                                open.Remove(adjacentPositions[i]);
                                open.Add(adjacentPositions[i], h);
                            }

                        } else {
                            open.Add(adjacentPositions[i],h);
                        }

                    }
                }

            }

        }

        public bool IsWalkable(World world, Vector3Int pos)
        {
            Block block = world.blocks.GetBlock(pos);
            if (!block.canBeWalkedOn)
                return false;

            for (int y = 1; y < entityHeight + 1; y++)
            {
                block = world.blocks.GetBlock(pos.Add(0, y, 0));
                if (!block.canBeWalkedThrough)
                    return false;
            }

            return true;

        }

        public static float Distance(Vector3Int a, Vector3Int b)
        {
            var x = a.x - b.x;
            var y = a.y - b.y;
            var z = a.z - b.z;

            if (x < 0)
                x *= -1;

            if (y < 0)
                y *= -1;

            if (z < 0)
                z *= -1;

            return x + y + z;
        }
    }
}
