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

    Dictionary<Stage, List<Chunk>> chunkWorkLists = new Dictionary<Stage, List<Chunk>>();
    List<Chunk> markedForDeletion = new List<Chunk>();
    World world;

    public bool isPlaying = true;
    public Thread loopThread;
    public Thread renderThread;

    public ChunksLoop(World world)
    {
        this.world = world;
        chunkWorkLists.Add(Stage.terrain, new List<Chunk>());
        chunkWorkLists.Add(Stage.buildMesh, new List<Chunk>());
        chunkWorkLists.Add(Stage.saveAndDelete, new List<Chunk>());
        chunkWorkLists.Add(Stage.delete, new List<Chunk>());

        loopThread = new Thread(() =>
        {
            while (isPlaying)
            {
                try
                {
                    Terrain();
                    CheckChunksMarkedForDeletion();
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
            foreach (var list in chunkWorkLists.Values)
            {
                i += list.Count;
            }
            return i;
        }
    }

    public void MainThreadLoop()
    {
        Delete();
        // eventually we want to handle adding mesh data to the unity mesh
        // and maybe even the chunk's regular update func
    }

    void Terrain()
    {
        int index = 0;

        while (chunkWorkLists[Stage.terrain].Count > index)
        {
            Chunk chunk = chunkWorkLists[Stage.terrain][index];

            if (!IsCorrectStage(Stage.terrain, chunk))
            {
                chunkWorkLists[Stage.terrain].RemoveAt(index);
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
            else
            {
                index++;
            }
        }
    }

    void BuildMesh()
    {
        while (chunkWorkLists[Stage.buildMesh].Count > 0)
        {
            Chunk chunk = chunkWorkLists[Stage.buildMesh][0];
            if (!IsCorrectStage(Stage.buildMesh, chunk))
            {
                chunkWorkLists[Stage.buildMesh].RemoveAt(0);
                continue;
            }

            chunk.render.BuildMeshData();
            chunk.stage = Stage.ready;
        }
    }

    void CheckChunksMarkedForDeletion()
    {
        int index = 0;
        while (markedForDeletion.Count > index)
        {
            if (markedForDeletion[index] == null)
            {
                markedForDeletion.RemoveAt(index);
            }

            if (markedForDeletion[index].blocks.contentsGenerated &&
                (markedForDeletion[index].stage == Stage.created ||
                markedForDeletion[index].stage == Stage.ready))
            {
                markedForDeletion[index].stage = Stage.saveAndDelete;
                markedForDeletion.RemoveAt(index);
            }
            else
            {
                index++;
                continue;
            }
        }
    }

    void SaveAndDelete()
    {
        int index = 0;

        while (chunkWorkLists[Stage.saveAndDelete].Count > index)
        {
            Chunk chunk = chunkWorkLists[Stage.saveAndDelete][index];
            if (!IsCorrectStage(Stage.saveAndDelete, chunk))
            {
                chunkWorkLists[Stage.saveAndDelete].RemoveAt(index);
                continue;
            }

            if (chunk.logic.GetFlag(Flag.chunkModified))
            {
                Serialization.SaveChunk(chunk);
            }

            chunk.stage = Stage.delete;
        }
    }

    void Delete()
    {
        int index = 0;

        while (chunkWorkLists[Stage.delete].Count > index)
        {
            Chunk chunk = chunkWorkLists[Stage.delete][index];

            if (chunk == null)
            {
                chunkWorkLists[Stage.delete].RemoveAt(index);
                continue;
            }

            chunk.ReturnChunkToPool();
            //ReturnChunkToPool sets the chunk's stage to created
            chunk.stage = Stage.created;
        }
    }

    public void AddToDeletionList(Chunk chunk)
    {
        if (!markedForDeletion.Contains(chunk))
        {
            markedForDeletion.Add(chunk);
        }
    }

    public void ChunkStageChanged(Chunk chunk, Stage oldStage, Stage newStage)
    {
        if (chunkWorkLists.ContainsKey(oldStage) &&
            chunkWorkLists[oldStage].Contains(chunk))
            chunkWorkLists[oldStage].Remove(chunk);

        if (chunkWorkLists.ContainsKey(newStage) &&
            !chunkWorkLists[newStage].Contains(chunk))
            chunkWorkLists[newStage].Add(chunk);
    }

    bool IsCorrectStage(Stage stage, Chunk chunk)
    {
        return (chunk != null && chunk.stage == stage);
    }
}
