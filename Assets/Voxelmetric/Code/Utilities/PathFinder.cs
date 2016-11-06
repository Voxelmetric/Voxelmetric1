using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Collections;

public class PathFinder {

    Dictionary<BlockPos, Heuristics> open = new Dictionary<BlockPos, Heuristics>();
    Dictionary<BlockPos, Heuristics> closed = new Dictionary<BlockPos, Heuristics>();

    public List<BlockPos> path = new List<BlockPos>();

    BlockPos targetLocation;
    BlockPos startLocation;

    public float range = 0.5f;
    float distanceFromStartToTarget = 0;
    float maxDistToTravelAfterDirect = 800;
    float maxDistToTravelMultiplier = 10;

    public int MaxClosed { get { return 1000; } }

    public enum Status { stopped, working, failed, succeeded };

    public Status status { get; private set; }

    private bool debug = false;

    private Func<BlockPos, BlockPos, float> getMovementCost;
    private AdjacentInfo adjacentInfo;

    private static AdjacentInfo walkingInfo = new AdjacentInfo();

    public static AdjacentInfo WalkingInfo { get { return walkingInfo; } }

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

    public class AdjacentInfo {
        private List<BlockPos> offset = new List<BlockPos>();
        private List<float> distance = new List<float>();

        public int Count { get { return offset.Count; } }

        public void MakeWalking() {
            Clear();
            //Cardinal directions
            AddOffset(new BlockPos(0, 0, 1));
            AddOffset(new BlockPos(1, 0, 0));
            AddOffset(new BlockPos(0, 0, -1));
            AddOffset(new BlockPos(-1, 0, 0));

            //diagonal directions
            AddOffset(new BlockPos(1, 0, 1));
            AddOffset(new BlockPos(1, 0, -1));
            AddOffset(new BlockPos(-1, 0, -1));
            AddOffset(new BlockPos(-1, 0, 1));

            //climb up directions
            AddOffset(new BlockPos(0, 1, 1));
            AddOffset(new BlockPos(1, 1, 0));
            AddOffset(new BlockPos(0, 1, -1));
            AddOffset(new BlockPos(-1, 1, 0));

            //climb down directions
            AddOffset(new BlockPos(0, -1, 1));
            AddOffset(new BlockPos(1, -1, 0));
            AddOffset(new BlockPos(0, -1, -1));
            AddOffset(new BlockPos(-1, -1, 0));
        }

        public void MakeFlying() {
            foreach(BlockPos pos in new BlockPosEnumerable(-BlockPos.one, BlockPos.one, true)) {
                if (pos != BlockPos.zero)
                    AddOffset(pos);
            }
        }


        public BlockPos GetOffset(int i) {
            return offset[i];
        }

        public float GetDistance(int i) {
            return distance[i];
        }

        public void Clear() {
            offset.Clear();
            distance.Clear();
        }

        private void AddOffset(BlockPos blockPos) {
            offset.Add(blockPos);
            distance.Add(blockPos.Length);
        }

    }

    static PathFinder() {
        walkingInfo.MakeWalking();
    }

    public PathFinder(BlockPos start, BlockPos target, Func<BlockPos, BlockPos, float> getMovementCost,
            AdjacentInfo adjacentInfo, float range = 0.5f) {
        this.getMovementCost = getMovementCost;
        this.adjacentInfo = adjacentInfo;
        status = Status.working;
        this.range = range;
        startLocation = start.Add(Direction.down);
        targetLocation = target.Add(Direction.down);
        distanceFromStartToTarget = Distance(startLocation, targetLocation);

        open.Add(startLocation, new Heuristics(0, distanceFromStartToTarget, startLocation));
    }

    public PathFinder(BlockPos start, BlockPos target, World world, float range = 0.5f, int entityHeight=1)
        :this(start, target, (from, to) => world.MovementCost(from, to, entityHeight), walkingInfo, range)
    {
    }

    public void FindPathThreaded() {
        new Thread(FindPath).Start();
    }

    public IEnumerator FindPathCoroutine() {
        while(status == Status.working) {
            Update();
            //if(Throttle.Instance.FrameTimeDone())
            yield return null;
        }
    }

    public void FindPath() {
        while(status == Status.working) {
            Update();
        }
    }

    public void Update()
    {
        Profiler.BeginSample("PathFinder.Update");
        if (status == Status.working)
            ProcessBest();
        Profiler.EndSample();
    }

    public void Stop() {
        status = Status.stopped;
    }

    void PathComplete(BlockPos lastTile)
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

        if (Distance(((Vector3)bestPos), targetLocation) <= range)
        {
            PathComplete(bestPos);
            status = Status.succeeded;
            return;
        }

        if (bestPos.Equals(new BlockPos(0, 10000, 0)))
        {
            status = Status.failed;
        }

        ProcessTile(bestPos);
    }

    void ProcessTile(BlockPos pos)
    {
        Heuristics h;
        if (!open.TryGetValue(pos, out h))
            return;

        open.Remove(pos);
        closed.Add(pos, h);
        if(closed.Count > MaxClosed) {
            if(debug) Debug.LogError("PathFinder.ProcessTile failing due to MaxClosed being exceeded");
            status = Status.failed;
        }

        CheckAdjacent(pos, h);
    }

    void CheckAdjacent(BlockPos pos, Heuristics dist)
    {
        for (int i = 0, n = adjacentInfo.Count; i<n; i++) {
            var adjPos = adjacentInfo.GetOffset(i) + pos;
            if(!closed.ContainsKey(adjPos)){
                float movementCost = getMovementCost(pos.Add(0,1,0), adjPos.Add(0,1,0));
                if (movementCost > 0f){
                    float adjacentDistanceFromStart = dist.g + adjacentInfo.GetDistance(i) * movementCost;
                    var h = new Heuristics(
                            adjacentDistanceFromStart,
                            Distance(targetLocation,
                            adjPos),
                            pos);
                    Heuristics existingTile;
                    if (open.TryGetValue(adjPos, out existingTile)) {
                        if(existingTile.g > adjacentDistanceFromStart) {
                            open.Remove(adjPos);
                            open.Add(adjPos, h);
                        }
                    } else {
                        open.Add(adjPos, h);
                    }
                }
            }
        }
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
