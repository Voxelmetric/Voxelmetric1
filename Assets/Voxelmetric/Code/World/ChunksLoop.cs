using System.Collections.Generic;
using UnityEngine;
using Assets.Voxelmetric.Code.Common.Threading;
using Assets.Voxelmetric.Code.Common.Threading.Managers;

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

    private Dictionary<Stage, List<BlockPos>> chunkWorkLists = new Dictionary<Stage, List<BlockPos>>();
    private List<BlockPos> markedForDeletion = new List<BlockPos>();

    private World world;

    private Material chunkMaterial;

    public int ChunksInProgress {
        get {
            int i = chunkWorkLists[Stage.buildMesh].Count;
            i += chunkWorkLists[Stage.terrain].Count;
            return i;
        }
    }

    public ChunksLoop(World world)
    {
        this.world = world;
        var renderer = world.gameObject.GetComponent<Renderer>();
        if (renderer != null)
            chunkMaterial = renderer.material;
        
        chunkWorkLists.Add(Stage.terrain, new List<BlockPos>());
        chunkWorkLists.Add(Stage.buildMesh, new List<BlockPos>());
        chunkWorkLists.Add(Stage.priorityBuildMesh, new List<BlockPos>());
        chunkWorkLists.Add(Stage.render, new List<BlockPos>());
    }

    public void Stop()
    {
    }

    public void MainThreadLoop()
    {
        Profiler.BeginSample("UpdateTerrain");
        UpdateTerrain();
        Profiler.EndSample();

        Profiler.BeginSample("UpdateMeshes");
        UpdateMeshes();
        Profiler.EndSample();

        Profiler.BeginSample("DeleteMarkedChunks");
        DeleteMarkedChunks();
        Profiler.EndSample();

        Profiler.BeginSample("UpdateMeshFilters");
        UpdateMeshFilters();
        Profiler.EndSample();

        Profiler.BeginSample("DrawChunkMeshes");
        DrawChunkMeshes();
        Profiler.EndSample();
    }

    private void UpdateTerrain()
    {
        var workList = chunkWorkLists[Stage.terrain];
        for (int i=0; i<workList.Count;)
        {
            Chunk chunk = world.chunks.Get(workList[i]);

            if (!IsCorrectStage(Stage.terrain, chunk))
            {
                workList.RemoveAt(i);
                continue;
            }
            
            // Proceed with the next stage
            if (BuildTerrain(chunk))
                chunk.stage = Stage.buildMesh;
            else
                i++;
        }
    }

    private bool BuildTerrain(Chunk chunk)
    {
        List<Chunk> generatedChunks = new List<Chunk>();
        foreach (BlockPos posChunk in new BlockPosEnumerable(chunk.pos - Config.Env.ChunkSizePos,
                                                             chunk.pos + Config.Env.ChunkSizePos,
                                                             Config.Env.ChunkSizePos, true))
        {
            Chunk chunkToGen = world.chunks.Get(posChunk);
            if (chunkToGen == null)
                continue;

            if (!chunkToGen.blocks.contentsGenerated)
            {
                if (chunkToGen.world.networking.isServer)
                {
                    if (!chunkToGen.blocks.generationStarted)
                    {
                        chunkToGen.blocks.generationStarted = true;
                        WorkPoolManager.Add(
                            new ThreadItem(
                                chunkToGen.ThreadId,
                                arg =>
                                {
                                    Chunk ch = (Chunk)arg;
                                    ch.world.terrainGen.GenerateTerrainForChunk(ch);
                                    ch.blocks.contentsRequestCompleted = true;
                                },
                                chunkToGen
                                )
                            );
                    }

                    if (chunkToGen.blocks.contentsRequestCompleted && !chunkToGen.blocks.loadRequested)
                    {
                        chunkToGen.blocks.loadRequested = true;
                        IOPoolManager.Add(
                            new ThreadItem(
                                arg =>
                                {
                                    Chunk ch = (Chunk)arg;
                                    Serialization.LoadChunk(ch);
                                    ch.blocks.contentsGenerated = true;

                                },
                                chunkToGen
                                )
                            );
                    }

                    if (chunkToGen.blocks.contentsGenerated)
                        generatedChunks.Add(chunkToGen);
                }
                else
                {
                    if (!chunkToGen.blocks.generationStarted)
                    {
                        chunkToGen.blocks.generationStarted = true;
                        NetworkPoolManager.Add(
                            new ThreadItem(
                                arg =>
                                {
                                    Chunk ch = (Chunk)arg;
                                    ch.world.networking.client.RequestChunk(ch.pos);
                                }, chunkToGen
                                ));

                    }
                }
            }
        }

        // Let's wait until all neighbors are generated
        bool chunksAllGenerated = true;
        foreach (Chunk chunkToGen in generatedChunks)
        {
            if (!chunkToGen.blocks.contentsGenerated)
            {
                chunksAllGenerated = false;
                break;
            }
        }
        return chunksAllGenerated;
    }

    private void UpdateMeshes()
    {
        var workListBuildMesh = chunkWorkLists[Stage.buildMesh];
        var workListBuildPriorityMesh = chunkWorkLists[Stage.priorityBuildMesh];

        for (int i = 0; i<workListBuildMesh.Count;)
        {
            if (workListBuildPriorityMesh.Count>0)
                break;

            Chunk chunk = world.chunks.Get(workListBuildMesh[0]);
            if (!IsCorrectStage(Stage.buildMesh, chunk))
            {
                workListBuildMesh.RemoveAt(0);
                continue;
            }

            BuildMesh(chunk);

            if (chunk.blocks.meshCompleted)
                chunk.stage = Stage.render;
            else
                i++;
        }

        for (int i = 0; i<workListBuildPriorityMesh.Count;)
        {
            Chunk chunk = world.chunks.Get(workListBuildPriorityMesh[0]);
            if (!IsCorrectStage(Stage.priorityBuildMesh, chunk))
            {
                workListBuildPriorityMesh.RemoveAt(0);
                continue;
            }

            BuildMesh(chunk);

            if (chunk.blocks.meshCompleted)
                chunk.stage = Stage.render;
            else
                i++;
        }
    }

    private static void BuildMesh(Chunk chunk)
    {
        if (!chunk.blocks.meshStarted)
        {
            chunk.blocks.meshStarted = true;
            WorkPoolManager.Add(
                new ThreadItem(
                    chunk.ThreadId,
                    arg =>
                    {
                        Chunk ch = (Chunk)arg;
                        ch.render.BuildMeshData();
                        ch.blocks.meshCompleted = true;
                    },
                    chunk)
                );
        }
    }

    private void UpdateMeshFilters()
    {
        int index = 0;
        while (chunkWorkLists[Stage.render].Count > index)
        {
            Chunk chunk = world.chunks.Get(chunkWorkLists[Stage.render][index]);

            if (chunk == null)
            {
                index++;
                continue;
            }

            chunk.render.BuildMesh();
            chunk.stage = Stage.ready;
        }
    }

    private void DeleteMarkedChunks()
    {
        int index = 0;
        while (markedForDeletion.Count > index)
        {
            Chunk chunk = world.chunks.Get(markedForDeletion[index]);

            if (chunk != null && chunk.blocks.contentsGenerated &&
                (chunk.stage == Stage.created || chunk.stage == Stage.ready))
            {
                if (chunk.blocks.contentsModified)
                {
                    Serialization.SaveChunk(chunk);
                }
                world.chunks.Remove(chunk.pos);
                markedForDeletion.RemoveAt(index);
            }
            else
            {
                index++;
            }
        }
    }

    public void DrawChunkMeshes()
    {
        Transform trans = world.transform;
        foreach (var pos in world.chunks.posCollection)
        {
            Mesh mesh = world.chunks[pos].render.mesh;
            if (mesh != null && mesh.vertexCount != 0)
            {
                Graphics.DrawMesh(mesh, (trans.rotation * pos) + trans.position, trans.rotation, chunkMaterial, 0);
            }
        }
    }

    public void AddToDeletionList(Chunk chunk)
    {
        if (!markedForDeletion.Contains(chunk.pos))
        {
            markedForDeletion.Add(chunk.pos);
        }
    }

    public void ChunkStageChanged(Chunk chunk, Stage oldStage, Stage newStage)
    {
        List<BlockPos> list;
        // Remove old state is possible
        if (chunkWorkLists.TryGetValue(oldStage, out list) && list.Contains(chunk.pos))
            list.Remove(chunk.pos);
        // Add new state if possible
        if (chunkWorkLists.TryGetValue(newStage, out list) && !list.Contains(chunk.pos))
        {
            if (newStage==Stage.buildMesh || newStage==Stage.priorityBuildMesh)
            {
                chunk.blocks.meshStarted = false;
                chunk.blocks.meshCompleted = false;
            }

            list.Add(chunk.pos);
        }
    }

    private static bool IsCorrectStage(Stage stage, Chunk chunk)
    {
        return chunk!=null && chunk.stage==stage;
    }
}
