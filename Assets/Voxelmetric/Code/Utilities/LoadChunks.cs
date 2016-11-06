using UnityEngine;
using System.Threading;
using System.Collections.Generic;

public class LoadChunks : MonoBehaviour {

    public World world;

    [SerializeField, Range(1, 64)]
    private int chunkLoadRadius = 8;
    private BlockPos[] chunkPositions;

    //The distance is measured in chunks
    [SerializeField, Range(1, 64)]
    private int distanceToDeleteChunks = (int)(8 * 1.25f);
    private int distanceToDeleteInUnitsSquared;

    public int ChunkLoadRadius {
        get { return chunkLoadRadius; }
        set {
            bool changed = chunkLoadRadius != value;
            chunkLoadRadius = value;
            if ( changed )
                OnChangedLoadRadius();
        }
    }

    public int DistanceToDeleteChunks {
        get { return distanceToDeleteChunks; }
        set {
            bool changed = distanceToDeleteChunks != value;
            distanceToDeleteChunks = value;
            if (changed)
                OnChangedDeleteDistance();
        }
    }

    void Start() {
        OnChangedLoadRadius();
        OnChangedDeleteDistance();
    }

    void Update() {
        if(world.chunksLoop.ChunksInProgress > 32) {
            return;
        }

        DeleteChunks();
        FindChunksAndLoad();
    }

    // This function is called when the script is loaded or a value is changed in the inspector (Called in the editor only)
    public void OnValidate() {
        OnChangedLoadRadius();
        OnChangedDeleteDistance();
    }

    private void OnChangedLoadRadius() {
        chunkPositions = ChunkLoadOrder.ChunkPositions(chunkLoadRadius);
    }

    private void OnChangedDeleteDistance() {
        distanceToDeleteInUnitsSquared = (int)((DistanceToDeleteChunks + 1) * Config.Env.ChunkSize * Config.Env.BlockSize);
        distanceToDeleteInUnitsSquared *= distanceToDeleteInUnitsSquared;
    }

    private void DeleteChunks() {
        int posX = Mathf.FloorToInt(transform.position.x);
        int posZ = Mathf.FloorToInt(transform.position.z);

        foreach(var pos in world.chunks.posCollection) {
            int xd = posX - pos.x;
            int yd = posZ - pos.z;

            if((xd * xd + yd * yd) > distanceToDeleteInUnitsSquared) {
                Chunk chunk = world.chunks.Get(pos);
                chunk.MarkForDeletion();
            }
        }
    }

    private void FindChunksAndLoad() {
        //Cycle through the array of positions
        for(int i = 0; i < chunkPositions.Length; i++) {
            //Get the position of this gameobject to generate around
            BlockPos playerPos = ((BlockPos)transform.position).ContainingChunkCoordinates();

            //translate the player position and array position into chunk position
            BlockPos newChunkPos = new BlockPos(
                chunkPositions[i].x * Config.Env.ChunkSize + playerPos.x,
                0,
                chunkPositions[i].z * Config.Env.ChunkSize + playerPos.z
                );

            //Get the chunk in the defined position
            Chunk newChunk = world.chunks.Get(newChunkPos);

            //If the chunk already exists and it's already
            //rendered or in queue to be rendered continue
            if(newChunk != null && newChunk.stage != Stage.created)
                continue;

            for(int y = world.config.minY; y <= world.config.maxY; y += Config.Env.ChunkSize)
                world.chunks.New(new BlockPos(newChunkPos.x, y, newChunkPos.z));

            return;
        }
    }
}
