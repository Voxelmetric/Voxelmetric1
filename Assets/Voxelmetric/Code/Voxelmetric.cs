using UnityEngine;
using System.Threading;
using System.Collections.Generic;

public static class Voxelmetric
{
    //Used as a manager class with references to classes treated like singletons
    public static VoxelmetricResources resources = new VoxelmetricResources();

    public static GameObject CreateGameObjectBlock(Block original, Vector3 position, Quaternion rotation)
    {
        BlockPos blockPos = new BlockPos();

        if (original == Block.Air)
            return null;

        EmptyChunk chunk = original.world.GetComponent<EmptyChunk>();
        if (chunk == null)
        {
            chunk = (EmptyChunk)original.world.gameObject.AddComponent(typeof(EmptyChunk));
            chunk.world = original.world;
        }

        original.OnCreate(chunk, blockPos - blockPos.ContainingChunkCoordinates(), blockPos);

        return GOFromBlock(original, blockPos, position, rotation, chunk);
    }

    public static GameObject CreateGameObjectBlock(BlockPos blockPos, World world, Vector3 position, Quaternion rotation)
    {
        Block original = GetBlock(blockPos, world);
        if (original == Block.Air)
            return null;

        EmptyChunk chunk = world.GetComponent<EmptyChunk>();
        if (chunk == null)
        {
            chunk = (EmptyChunk)world.gameObject.AddComponent(typeof(EmptyChunk));
            chunk.world = world;
            chunk.pos = blockPos.ContainingChunkCoordinates();
        }

        original.OnCreate(chunk, blockPos - blockPos.ContainingChunkCoordinates(), blockPos);

        return GOFromBlock(original, blockPos, position, rotation, chunk);
    }

    static GameObject GOFromBlock(Block original, BlockPos blockPos, Vector3 position, Quaternion rotation, Chunk chunk)
    {
        GameObject go = (GameObject)GameObject.Instantiate(Resources.Load<GameObject>(chunk.world.config.pathToBlockPrefab), position, rotation);
        go.transform.localScale = new Vector3(Config.Env.BlockSize, Config.Env.BlockSize, Config.Env.BlockSize);

        MeshData meshData = new MeshData();

        original.AddBlockData(chunk, blockPos, blockPos, meshData);
        for (int i = 0; i < meshData.vertices.Count; i++)
        {
            meshData.vertices[i] -= (Vector3)blockPos;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = meshData.vertices.ToArray();
        mesh.triangles = meshData.triangles.ToArray();

        mesh.colors = meshData.colors.ToArray();

        mesh.uv = meshData.uv.ToArray();
        mesh.RecalculateNormals();

        go.GetComponent<Renderer>().material.mainTexture = chunk.world.textureIndex.atlas;
        go.GetComponent<MeshFilter>().mesh = mesh;

        return go;
    }

    public static BlockPos GetBlockPos(RaycastHit hit, bool adjacent = false)
    {
        World world = hit.collider.gameObject.GetComponent<Chunk>().world;
        return GetBlockPos(hit, world, adjacent);
    }

    public static BlockPos GetBlockPos(RaycastHit hit, World world, bool adjacent = false)
    {
        Vector3 pos = hit.point;
        pos = Quaternion.Inverse(world.gameObject.transform.rotation) * pos;
        pos -= world.gameObject.transform.position;

        Vector3 rotatedNormal = Quaternion.Inverse(world.gameObject.transform.rotation) * hit.normal;

        pos = new Vector3(
            MoveWithinBlock(pos.x, rotatedNormal.x, adjacent),
            MoveWithinBlock(pos.y, rotatedNormal.y, adjacent),
            MoveWithinBlock(pos.z, rotatedNormal.z, adjacent)
            );

        return pos;
    }

    static float MoveWithinBlock(float pos, float norm, bool adjacent = false)
    {
        float minHalfBlock = Config.Env.BlockSize / 2 - 0.01f;
        float maxHalfBlock = Config.Env.BlockSize / 2 + 0.01f;
        //Because of float imprecision we can't guarantee a hit on the side of a

        //Get the distance of this position from the nearest block center
        //accounting for the size of the block
        float offset = pos - ((int)(pos/Config.Env.BlockSize) * Config.Env.BlockSize);
        if ((offset > minHalfBlock && offset < maxHalfBlock) || (offset < -minHalfBlock && offset > -maxHalfBlock))
        {
            if (adjacent)
            {
                pos += (norm / 2 * Config.Env.BlockSize);
            }
            else
            {
                pos -= (norm / 2 * Config.Env.BlockSize);
            }
        }

        return pos;
    }

    public static bool SetBlock(RaycastHit hit, Block block, bool adjacent = false)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();

        if (chunk == null)
            return false;

        BlockPos pos = GetBlockPos(hit, adjacent);
        chunk.world.blocks.Set(pos, block);

        //if (Config.Toggle.BlockLighting)
        //{
        //    BlockLight.LightArea(chunk.world, pos);
        //}

        return true;
    }

    public static bool SetBlock(BlockPos pos, Block block, World world)
    {
        Chunk chunk = world.chunks.Get(pos);
        if (chunk == null)
            return false;

        chunk.world.blocks.Set(pos, block);

        //if (Config.Toggle.BlockLighting)
        //{
        //    BlockLight.LightArea(world, pos);
        //}

        return true;
    }

    public static Block GetBlock(RaycastHit hit)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();
        if (chunk == null)
            return Block.Air;

        BlockPos pos = GetBlockPos(hit, false);

        return GetBlock(pos, chunk.world);
    }

    public static Block GetBlock(BlockPos pos, World world)
    {
        Block block = world.blocks.Get(pos);

        return block;
    }

    /// <summary>
    /// Saves all chunks currently loaded, if UseMultiThreading is enabled it saves the chunks
    ///  asynchronously and the SaveProgress object returned will show the progress
    /// </summary>
    /// <param name="world">Optional parameter for the world to save chunks for, if left
    /// empty it will use the world Singleton instead</param>
    /// <returns>A SaveProgress object to monitor the save.</returns>
    public static SaveProgress SaveAll(World world)
    {
        //Create a saveprogress object with positions of all the chunks in the world
        //Then save each chunk and update the saveprogress's percentage for each save
        SaveProgress saveProgress = new SaveProgress(world.chunks.posCollection);
        List<Chunk> chunksToSave = new List<Chunk>();
        chunksToSave.AddRange(world.chunks.chunkCollection);

        if (Config.Toggle.UseMultiThreading)
        {
            Thread thread = new Thread(() =>
           {

               foreach (var chunk in chunksToSave)
               {

                   while (!chunk.blocks.contentsGenerated || chunk.logic.GetFlag(Flag.busy))
                   {
                       Thread.Sleep(0);
                   }

                   Serialization.SaveChunk(chunk);

                   saveProgress.SaveCompleteForChunk(chunk.pos);
               }
           });
            thread.Start();
        }
        else
        {
            foreach (var chunk in chunksToSave)
            {
                Serialization.SaveChunk(chunk);
                saveProgress.SaveCompleteForChunk(chunk.pos);
            }
        }

        return saveProgress;
    }
}