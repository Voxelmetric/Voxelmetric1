using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PathFinder {

    static int characterHeight = 2;

    World world;
    Dictionary<BlockPos, Heuristics> open = new Dictionary<BlockPos, Heuristics>();
    Dictionary<BlockPos, Heuristics> closed = new Dictionary<BlockPos, Heuristics>();

    public List<BlockPos> path = new List<BlockPos>();

    public BlockPos targetLocation;
    BlockPos startLocation;

    public bool complete = false;
    public bool noRoute = false;

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

    public PathFinder() { }

    public void Start(BlockPos start, BlockPos target, World world)
    {
        open.Clear();
        closed.Clear();

        startLocation = start;
        targetLocation = target;
        this.world = world;

        path.Clear();

        open.Add(start, new Heuristics(0,Distance(start, target), start));
        complete = false;
        noRoute = false;

        distanceFromStartToTarget = Distance(start, target);
    }

    public void update()
    {
        if (!complete)
        {
            ProcessBest();
        }
    }

    void PathComplete(BlockPos lastTile)
    {
        complete = true;

        Heuristics pos;
        closed.TryGetValue(lastTile, out pos);
        path.Clear();
        path.Add(lastTile);

        open.TryGetValue(lastTile, out pos);

        while (!pos.parent.Equals(startLocation))
        {
            path.Insert(0, pos.parent);
            var startPos = pos.parent;
            if (!closed.TryGetValue(pos.parent, out pos))
                break;

            Debug.DrawLine(new Vector3(pos.parent.x, pos.parent.y + 1, pos.parent.z),
            new Vector3(startPos.x, startPos.y + 1, startPos.z), Color.blue, 40);

        }

        open.Clear();
        closed.Clear();
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
        Debug.DrawLine(new Vector3(parent.parent.x, parent.parent.y + 1, parent.parent.z),
            new Vector3(bestPos.x, bestPos.y + 1, bestPos.z), Color.blue, 40);

        if (Distance(((Vector3)bestPos) + (Vector3.up*2), targetLocation) <= range)
        {
            PathComplete(bestPos);
            return;
        }

        if (bestPos.Equals(new BlockPos(0, 10000, 0)))
        {
            noRoute = true;
            Debug.Log("Failed to pf " + targetLocation.x + ", " + targetLocation.y + ", " + targetLocation.z);
            complete = true;
            path.Clear();
            path.Add(startLocation);

            open.Clear();
            closed.Clear();
            return;
        }

        ProcessTile(bestPos);
    }

    void ProcessTile(BlockPos pos)
    {
        Heuristics h = new Heuristics();
        bool exists = open.TryGetValue(pos, out h);

        if (!exists)
            return;

        Debug.DrawLine(new Vector3(h.parent.x, h.parent.y + 1, h.parent.z),
                new Vector3(pos.x, pos.y + 1, pos.z), Color.red, 40);

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

    public static bool IsWalkable(World world, BlockPos pos)
    {
        if(!world.GetBlock(pos.x, pos.y, pos.z).Block().IsSolid(Direction.up))
            return false;

        for(int y = 1; y< characterHeight+1; y++){
            if (world.GetBlock(pos.x, pos.y + y, pos.z).GetType() != typeof(BlockAir))
                return false;
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
