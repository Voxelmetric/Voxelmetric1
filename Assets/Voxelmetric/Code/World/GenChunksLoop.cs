using System.Collections;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// This class runs constantly running generation jobs for chunks. When chunks are added to one of the
/// generation stages (chunk.generationStage) it should also be added to a list here and this 
/// class will work through every list running the job for the relevant stage and pushing it to the
/// next stage. Chunks can be added and forgotten because they will work their way to fully functioning
/// chunks by the end.
/// 
/// Use ChunksInProgress to check the number of chunks in queue to be generated before adding a new one.
/// There's no point in piling up the queue, better to wait, then add more.
/// </summary>
public class ChunksLoop {

    public Dictionary<Stage, List<Chunk>> chunksToGen = new Dictionary<Stage, List<Chunk>>();
    World world;

    public bool isPlaying = true;
    public Thread loopThread;
    public Thread renderThread;

    public ChunksLoop(World world)
    {
        this.world = world;
        chunksToGen.Add(Stage.terrain, new List<Chunk>());
        chunksToGen.Add(Stage.buildMesh, new List<Chunk>());
        chunksToGen.Add(Stage.saveAndDelete, new List<Chunk>());

        loopThread = new Thread(() =>
        {
            while (isPlaying)
            {
                try
                {
                    Terrain();
                    SaveAndDelete();
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                }
            }
        });

        renderThread = new Thread(() =>
       {
           while (isPlaying)
           {
               try
               {
                   BuildMesh();
               }
               catch (Exception ex)
               {
                   Debug.Log(ex);
               }
           }
       });

        loopThread.Start();
        renderThread.Start();
    }

    public int ChunksInProgress
    {
        get
        {
            int i = 0;
            foreach (var list in chunksToGen.Values)
            {
                i += list.Count;
            }
            return i;
        }
    }

    public void MainThreadLoop()
    {
        // eventually we want to handle chunk return to pool and 
        // adding mesh data to the unity mesh
        // and maybe even the chunk's regular update func
    }

    void Terrain()
    {
        while (chunksToGen[Stage.terrain].Count > 0)
        {
            Chunk chunk = chunksToGen[Stage.terrain][0];

            if (!IsCorrectStage(Stage.terrain, chunk))
            {
                chunksToGen[Stage.terrain].RemoveAt(0);
                continue;
            }

            bool chunksAllGenerated = true;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Chunk chunkToGen = world.chunks.Get(chunk.pos + (new BlockPos(x, y, z) * Config.Env.ChunkSize));
                        chunkToGen.blocks.GenerateChunkContents();
                        if (!chunkToGen.blocks.contentsGenerated)
                        {
                            chunksAllGenerated = false;
                        }
                    }
                }
            }

            if (chunksAllGenerated)
            {
                chunk.stage = Stage.buildMesh;
            }
        }
    }

    void BuildMesh()
    {
        while (chunksToGen[Stage.buildMesh].Count > 0)
        {
            Chunk chunk = chunksToGen[Stage.buildMesh][0];
            if (!IsCorrectStage(Stage.buildMesh, chunk))
            {
                chunksToGen[Stage.buildMesh].RemoveAt(0);
                continue;
            }

            chunk.render.BuildMeshData();
            chunk.stage = Stage.ready;
        }
    }

    void SaveAndDelete()
    {
        for (int i = 0; i < chunksToGen[Stage.saveAndDelete].Count; i++)
        {
            Chunk chunk = chunksToGen[Stage.saveAndDelete][0];

            if (chunk.logic.GetFlag(Flag.chunkModified))
                Serialization.SaveChunk(chunk);

            chunk.stage = Stage.delete;
        }
    }

    public void ChunkStageChanged(Chunk chunk, Stage oldStage, Stage newStage)
    {
        if (chunksToGen.ContainsKey(oldStage) &&
            chunksToGen[oldStage].Contains(chunk))
            chunksToGen[oldStage].Remove(chunk);

        if (chunksToGen.ContainsKey(newStage) &&
            !chunksToGen[newStage].Contains(chunk))
            chunksToGen[newStage].Add(chunk);
    }

    bool IsCorrectStage(Stage stage, Chunk chunk)
    {
        return (chunk != null && chunk.stage == stage);
    }
}
