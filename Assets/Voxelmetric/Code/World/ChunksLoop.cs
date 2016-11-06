using System.Collections;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

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

    private YieldInstruction terrainLoopCoroutine;
    private YieldInstruction buildMeshLoopCoroutine;

    private ICoroutineThrottler throttle;

    private Material chunkMaterial;

    public int ChunksInProgress {
        get {
            int i = chunkWorkLists[Stage.buildMesh].Count;
            i += chunkWorkLists[Stage.terrain].Count;
            return i;
        }
    }

    public int NumMarkedForDeletion { get { return markedForDeletion.Count; } }

    public ICoroutineThrottler Throttle {
        get { return throttle; }
        set { throttle = value; }
    }

    public ChunksLoop(World world)
    {
        throttle = new CoroutineThrottle(world);
        this.world = world;
        var renderer = world.gameObject.GetComponent<Renderer>();
        if (renderer != null)
            chunkMaterial = renderer.material;
        
        chunkWorkLists.Add(Stage.terrain, new List<BlockPos>());
        chunkWorkLists.Add(Stage.buildMesh, new List<BlockPos>());
        chunkWorkLists.Add(Stage.priorityBuildMesh, new List<BlockPos>());
        chunkWorkLists.Add(Stage.render, new List<BlockPos>());

        if (world.UseMultiThreading) {
            renderThread = new Thread(() => {
                try {
                    while(isPlaying)
                        CoroutineUtils.DoCoroutine(BuildMeshCoroutine());
                } catch(Exception ex) {
                    Debug.Log(ex);
                }
            });
            renderThread.Start();

            loopThread = new Thread(() => {
                try {
                    while(isPlaying)
                        CoroutineUtils.DoCoroutine(TerrainCoroutine());
                } catch (Exception ex) {
                    Debug.Log(ex);
                }
            });
            loopThread.Start();
        }
    }

    public int NumWorkChunks(Stage stage) {
        List<BlockPos> list;
        if ( chunkWorkLists.TryGetValue(stage, out list) )
            return list.Count;
        return 0;
    }

    public void Stop() {
        isPlaying = false;
    }

    public void MainThreadLoop() {
        if(!world.UseMultiThreading) {
            if(world.UseCoroutines) {
                if(buildMeshLoopCoroutine == null) {
                    buildMeshLoopCoroutine = new YieldInstruction();
                    throttle.StartCoroutineRepeater(BuildMeshCoroutineSupplier, CoroutineThrottle.TopPriority);
                }
                if(terrainLoopCoroutine == null) {
                    terrainLoopCoroutine = new YieldInstruction();
                    throttle.StartCoroutineRepeater(TerrainCoroutineSupplier, CoroutineThrottle.NormalPriority);
                }
            } else {
                Profiler.BeginSample("BuildMesh");
                BuildMesh();
                Profiler.EndSample();

                Profiler.BeginSample("Terrain");
                Terrain();
                Profiler.EndSample();
            }
        }

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

    public void Terrain() {
        CoroutineUtils.DoCoroutine(TerrainCoroutine());
    }

    public void BuildMesh() {
        CoroutineUtils.DoCoroutine(BuildMeshCoroutine());
    }

    public void AddToDeletionList(Chunk chunk) {
        if(!markedForDeletion.Contains(chunk.pos)) {
            markedForDeletion.Add(chunk.pos);
        }
    }

    public void ChunkStageChanged(Chunk chunk, Stage oldStage, Stage newStage) {
        if(chunkWorkLists.ContainsKey(oldStage) &&
            chunkWorkLists[oldStage].Contains(chunk.pos))
            chunkWorkLists[oldStage].Remove(chunk.pos);

        if(chunkWorkLists.ContainsKey(newStage) &&
            !chunkWorkLists[newStage].Contains(chunk.pos))
            chunkWorkLists[newStage].Add(chunk.pos);
    }

    private IEnumerator TerrainCoroutineSupplier() {
        while(isPlaying) {
            return TerrainCoroutine();
        }
        return null;
    }

    private IEnumerator BuildMeshCoroutineSupplier() {
        while (isPlaying) {
            return BuildMeshCoroutine();
        }
        return null;
    }

    private IEnumerator TerrainCoroutine()
    {
        int index = 0;
        var toBuild = chunkWorkLists[Stage.terrain];
        while (toBuild.Count > index) {
            Chunk chunk = world.chunks.Get(toBuild[index]);

            if (!IsCorrectStage(Stage.terrain, chunk)) {
                toBuild.RemoveAt(index);
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
                            var e = Serialization.LoadChunk(chunkToGen); while(e.MoveNext()) yield return e.Current;
                            chunkToGen.blocks.contentsGenerated = true;
                            world.FireContentsGenerated(chunkToGen);
                        } else {
                            if (!chunkToGen.blocks.generationStarted) {
                                chunkToGen.blocks.generationStarted = true;
                                chunkToGen.world.networking.client.RequestChunk(chunkToGen.pos);
                            }
                        }
                    }
                    if(!chunkToGen.blocks.contentsGenerated) {
                        Debug.LogWarning("Chunk not generated: " + chunkToGen.pos + " as neighbor of " + chunk.pos);
                        //yield break;
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
            if(!world.UseMultiThreading && !world.UseCoroutines)
                yield break;
            else
                yield return null;
        }
    }

    private IEnumerator BuildMeshCoroutine()
    {
        var toBuild = chunkWorkLists[Stage.priorityBuildMesh];
        while(toBuild.Count > 0) {
            var e = BuildMeshCoroutine(Stage.priorityBuildMesh); while(e.MoveNext()) yield return e.Current;

            if(!world.UseMultiThreading && !world.UseCoroutines)
                yield break;
        }

        toBuild = chunkWorkLists[Stage.buildMesh];
        while (toBuild.Count > 0) {
            var e = BuildMeshCoroutine(Stage.buildMesh); while (e.MoveNext()) yield return e.Current;

            if(!world.UseMultiThreading && !world.UseCoroutines)
                yield break;
        }
    }

    private IEnumerator BuildMeshCoroutine(Stage stage)
    {
        List<BlockPos> toBuild = chunkWorkLists[stage];
        while(toBuild.Any()) {
            Chunk chunk = world.chunks.Get(toBuild[0]);
            toBuild.RemoveAt(0);
            if(!IsCorrectStage(stage, chunk))
                continue;

            var e = chunk.render.BuildMeshDataCoroutine(); while(e.MoveNext()) yield return e.Current;
            chunk.stage = Stage.render;

            if(!world.UseMultiThreading && !world.UseCoroutines)
                yield break;
        }
    }

    private void UpdateMeshFilters()
    {
        int index = 0;
        List<BlockPos> toRender = chunkWorkLists[Stage.render];
        while (toRender.Count > index) {
            Chunk chunk = world.chunks.Get(toRender[index]);
            if (chunk == null){
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

    private void DrawChunkMeshes()
    {
        foreach (var chunk in world.chunks.chunkCollection) {
            if (chunk.render.mesh != null && chunk.render.mesh.vertexCount != 0)
                Graphics.DrawMesh(chunk.render.mesh,
                    (world.transform.rotation * chunk.pos) + world.transform.position,
                    world.transform.rotation, chunkMaterial, 0);
        }
    }

    private bool IsCorrectStage(Stage stage, Chunk chunk)
    {
        return (chunk != null && chunk.stage == stage);
    }
}
