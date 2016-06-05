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

    private Dictionary<Stage, List<BlockPos>> chunkWorkLists = new Dictionary<Stage, List<BlockPos>>();
    private List<BlockPos> markedForDeletion = new List<BlockPos>();

    private World world;

    private bool isPlaying = true;
    private Thread loopThread;
    private Thread renderThread;

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

        if (world.UseMultiThreading) {
            loopThread = new Thread(() => {
                try {
                    CoroutineUtils.DoCoroutine(TerrainLoopCoroutine());
                } catch (Exception ex) {
                    Debug.Log(ex);
                }
            });

            renderThread = new Thread(() => {
                try {
                    CoroutineUtils.DoCoroutine(BuildMeshLoopCoroutine());
                } catch (Exception ex) {
                    Debug.Log(ex);
                }
            });

            loopThread.Start();
            renderThread.Start();
        }
    }

    public void Stop() {
        isPlaying = false;
    }

    public void MainThreadLoop()
    {
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

    public void Terrain()
    {
        CoroutineUtils.DoCoroutine(TerrainCoroutine());
    }

    public IEnumerator TerrainLoopCoroutine() {
        while (isPlaying) {
            var enumerator = TerrainCoroutine();
            while (enumerator.MoveNext())
                yield return enumerator.Current;
            yield return null;
        }
    }

    public void BuildMesh() {
        CoroutineUtils.DoCoroutine(BuildMeshCoroutine());
    }

    public IEnumerator BuildMeshLoopCoroutine() {
        while (isPlaying) {
            var enumerator = BuildMeshCoroutine();
            while (enumerator.MoveNext())
                yield return enumerator.Current;
            yield return null;
        }
    }

    private IEnumerator TerrainCoroutine()
    {
        int index = 0;

        while (chunkWorkLists[Stage.terrain].Count > index) {
            Chunk chunk = world.chunks.Get(chunkWorkLists[Stage.terrain][index]);

            if (!IsCorrectStage(Stage.terrain, chunk)) {
                chunkWorkLists[Stage.terrain].RemoveAt(index);
                continue;
            }

            List<Chunk> generatedChunks = new List<Chunk>();
            foreach (BlockPos posChunk in new BlockPosEnumerable(chunk.pos - Config.Env.ChunkSizePos,
                    chunk.pos + Config.Env.ChunkSizePos, Config.Env.ChunkSizePos, true)) {
                Chunk chunkToGen = world.chunks.Get(posChunk);
                if (chunkToGen != null) {
                    if (!chunkToGen.blocks.contentsGenerated) {
                        if (chunkToGen.world.networking.isServer) {
                            var enumerator = chunkToGen.world.terrainGen.GenerateTerrainForChunkCoroutine(chunkToGen);
                            while (enumerator.MoveNext())
                                yield return enumerator.Current;
                            Serialization.LoadChunk(chunkToGen);
                            chunkToGen.blocks.contentsGenerated = true;
                        } else {
                            if (!chunkToGen.blocks.generationStarted) {
                                chunkToGen.blocks.generationStarted = true;
                                chunkToGen.world.networking.client.RequestChunk(chunkToGen.pos);
                            }
                        }
                    }
                    generatedChunks.Add(chunkToGen);
                }
            }
            
            bool chunksAllGenerated = true;
            foreach (Chunk chunkToGen in generatedChunks) {
                if (!chunkToGen.blocks.contentsGenerated) {
                    //chunksAllGenerated = false;
                }
            }

            if (chunksAllGenerated) {
                chunk.stage = Stage.buildMesh;
            } else {
                index++;
            }
        }
        yield return null;
    }

    private IEnumerator BuildMeshCoroutine()
    {
        while (chunkWorkLists[Stage.buildMesh].Count > 0)
        {
            if (chunkWorkLists[Stage.priorityBuildMesh].Count > 0)
            {
                break;
            }

            var enumerator = BuildMeshCoroutine(Stage.buildMesh);
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        while (chunkWorkLists[Stage.priorityBuildMesh].Count > 0)
        {
            var enumerator = BuildMeshCoroutine(Stage.priorityBuildMesh);
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }
    }

    private IEnumerator BuildMeshCoroutine(Stage stage)
    {
        Chunk chunk = world.chunks.Get(chunkWorkLists[stage][0]);
        if (!IsCorrectStage(stage, chunk))
        {
            chunkWorkLists[stage].RemoveAt(0);
            yield break;
        }

        var enumerator = chunk.render.BuildMeshDataCoroutine();
        while (enumerator.MoveNext())
            yield return enumerator.Current;
        chunk.stage = Stage.render;
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
                continue;
            }
        }
    }

    public void DrawChunkMeshes()
    {
        foreach (var pos in world.chunks.posCollection)
        {
            if (world.chunks[pos].render.mesh != null && world.chunks[pos].render.mesh.vertexCount != 0)
            {
                Graphics.DrawMesh(world.chunks[pos].render.mesh, (world.transform.rotation * pos) + world.transform.position, world.transform.rotation, chunkMaterial, 0);
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
        if (chunkWorkLists.ContainsKey(oldStage) &&
            chunkWorkLists[oldStage].Contains(chunk.pos))
            chunkWorkLists[oldStage].Remove(chunk.pos);

        if (chunkWorkLists.ContainsKey(newStage) &&
            !chunkWorkLists[newStage].Contains(chunk.pos))
            chunkWorkLists[newStage].Add(chunk.pos);
    }

    private static bool IsCorrectStage(Stage stage, Chunk chunk)
    {
        return (chunk != null && chunk.stage == stage);
    }
}
