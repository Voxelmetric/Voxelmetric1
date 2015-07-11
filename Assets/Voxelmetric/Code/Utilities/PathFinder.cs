using UnityEngine;
using System.Collections.Generic;
using System.Threading;

public class PathFinder {

    World world;
    Dictionary<BlockPos, Heuristics> open = new Dictionary<BlockPos, Heuristics>();
    Dictionary<BlockPos, Heuristics> closed = new Dictionary<BlockPos, Heuristics>();

    public List<BlockPos> path = new List<BlockPos>();

    BlockPos targetLocation;
    BlockPos startLocation;

    int entityHeight;

    public float range = 2;
    float distanceFromStartToTarget = 0;
    float maxDistToTravelAfterDirect = 80;
    float maxDistToTravelMultiplier = 2;

    struct Heuristics
    {
        /// Real distance from start
        public float g;
        /// Estimated distance to target
        public float h;

        public BlockPos parent;

        public Heuristics(float g, float h, BlockPos parent)
        {
            this.g = g;
            this.h = h;
            this.parent = parent;
        }
    };

    public PathFinder(BlockPos startLocation, BlockPos targetLocation, World world, int entityHeight=2)
    {
        this.startLocation = startLocation;
        this.targetLocation = targetLocation;
        distanceFromStartToTarget = Distance(startLocation, targetLocation);
        this.world = world;
        this.entityHeight = entityHeight;

        open.Add(startLocation, new Heuristics(0, distanceFromStartToTarget, startLocation));

        if (Config.Toggle.UseMultiThreading)
        {
            Thread thread = new Thread(() =>
           {
               while (path.Count == 0)
               {
                   update();
               }
           });
            thread.Start();
        }
        else
        {
            while (path.Count == 0)
            {
                update();
            }
        }
    }

    public void update()
    {
        if (path.Count == 0)
        {
            ProcessBest();
        }
    }

    void PathComplete(BlockPos lastTile)
    {
        Heuristics pos;
        closed.TryGetValue(lastTile, out pos);
        path.Clear();
        path.Add(lastTile);

        open.TryGetValue(lastTile, out pos);

        while (!pos.parent.Equals(startLocation))
        {
            path.Insert(0, pos.parent);
            if (!closed.TryGetValue(pos.parent, out pos))
                break;
        }
    }

    void ProcessBest()
    {
        float shortestDist = (distanceFromStartToTarget*maxDistToTravelMultiplier) + maxDistToTravelAfterDirect;
        BlockPos bestPos = new BlockPos(0,10000,0);

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

        if (Distance(((Vector3)bestPos) + (Vector3.up*2), targetLocation) <= range)
        {
            PathComplete(bestPos);
            return;
        }

        if (bestPos.Equals(new BlockPos(0, 10000, 0)))
        {
            Debug.Log("Failed to pf " + targetLocation.x + ", " + targetLocation.y + ", " + targetLocation.z);
            bestPos = new BlockPos(0,10000,0);

            foreach (var tile in open)
            {
                if (tile.Value.g + tile.Value.h < shortestDist)
                {
                    bestPos = tile.Key;
                    shortestDist = tile.Value.g + tile.Value.h;
                }
            }
            PathComplete(bestPos);
        }

        ProcessTile(bestPos);
    }

    void ProcessTile(BlockPos pos)
    {
        Heuristics h = new Heuristics();
        bool exists = open.TryGetValue(pos, out h);

        if (!exists)
            return;

        open.Remove(pos);
        closed.Add(pos, h);

        CheckAdjacent(pos, h);
    }

    void CheckAdjacent(BlockPos pos, Heuristics dist)
    {
        List<BlockPos> adjacentPositions = new List<BlockPos>();
        List<float> distanceFromStart= new List<float>();

        //Cardinal directions
        adjacentPositions.Add(new BlockPos(pos.x, pos.y, pos.z + 1));
        distanceFromStart.Add(dist.g +1);
        adjacentPositions.Add(new BlockPos(pos.x + 1, pos.y, pos.z));
        distanceFromStart.Add(dist.g +1);
        adjacentPositions.Add(new BlockPos(pos.x, pos.y, pos.z - 1));
        distanceFromStart.Add(dist.g +1);
        adjacentPositions.Add(new BlockPos(pos.x - 1, pos.y, pos.z));
        distanceFromStart.Add(dist.g +1);

        //diagonal directions
        adjacentPositions.Add(new BlockPos(pos.x + 1, pos.y, pos.z + 1));
        distanceFromStart.Add(dist.g +1.414f);
        adjacentPositions.Add(new BlockPos(pos.x + 1, pos.y, pos.z - 1));
        distanceFromStart.Add(dist.g +1.414f);
        adjacentPositions.Add(new BlockPos(pos.x - 1, pos.y, pos.z - 1));
        distanceFromStart.Add(dist.g +1.414f);
        adjacentPositions.Add(new BlockPos(pos.x - 1, pos.y, pos.z + 1));
        distanceFromStart.Add(dist.g +1.414f);

        //climb up directions
        adjacentPositions.Add(new BlockPos(pos.x, pos.y+1, pos.z+1));
        distanceFromStart.Add(dist.g + 1.414f);
        adjacentPositions.Add(new BlockPos(pos.x+1, pos.y+1, pos.z));
        distanceFromStart.Add(dist.g + 1.414f);
        adjacentPositions.Add(new BlockPos(pos.x, pos.y+1, pos.z-1));
        distanceFromStart.Add(dist.g + 1.414f);
        adjacentPositions.Add(new BlockPos(pos.x-1, pos.y+1, pos.z));
        distanceFromStart.Add(dist.g + 1.414f);

        //climb down directions
        adjacentPositions.Add(new BlockPos(pos.x, pos.y-1, pos.z+1));
        distanceFromStart.Add(dist.g + 1.414f);
        adjacentPositions.Add(new BlockPos(pos.x+1, pos.y-1, pos.z));
        distanceFromStart.Add(dist.g + 1.414f);
        adjacentPositions.Add(new BlockPos(pos.x, pos.y-1, pos.z-1));
        distanceFromStart.Add(dist.g + 1.414f);
        adjacentPositions.Add(new BlockPos(pos.x-1, pos.y-1, pos.z));
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

    public bool IsWalkable(World world, BlockPos pos)
    {
        Block block = world.GetBlock(pos);

        if (!block.controller.CanBeWalkedOn(block))
            return false;

        for (int y = 1; y < entityHeight + 1; y++)
        {
            block = world.GetBlock(pos.Add(0, y, 0));

            if (!block.controller.CanBeWalkedThrough(block))
            {
                return false;
            }
        }

        return true;

    }

    public static float Distance(BlockPos a, BlockPos b)
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
