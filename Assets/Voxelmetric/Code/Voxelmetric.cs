﻿using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using System.Collections;

public static class Voxelmetric
{
	//Used as a manager class with references to classes treated like singletons
	public static VoxelmetricResources resources = new VoxelmetricResources();

	public static GameObject CreateGameObjectBlock(Block original, Vector3 position, Quaternion rotation)
	{
		BlockPos blockPos = BlockPos.one * (Config.Env.ChunkSize/2);

		if (original.Type == Block.AirType)
			return null;

        EmptyChunk emptyChunk = original.World.EmptyChunk;
        original.OnCreate(emptyChunk, blockPos - blockPos.ContainingChunkCoordinates(), blockPos);

		return GOFromBlock(original, blockPos, position, rotation, emptyChunk);
	}

	public static GameObject CreateGameObjectBlock(BlockPos blockPos, World world, Vector3 position, Quaternion rotation)
	{
		Block original = GetBlock(blockPos, world);
		if (original.Type == Block.AirType)
			return null;

		EmptyChunk chunk = world.GetComponent<EmptyChunk>();
		if (chunk == null) {
			chunk = new EmptyChunk(original.World, blockPos.ContainingChunkCoordinates());
		}

		original.OnCreate(chunk, blockPos - blockPos.ContainingChunkCoordinates(), blockPos);

		return GOFromBlock(original, blockPos, position, rotation, chunk);
	}

	static GameObject GOFromBlock(Block original, BlockPos blockPos, Vector3 position, Quaternion rotation, Chunk chunk)
	{
		GameObject go = (GameObject)GameObject.Instantiate(Resources.Load<GameObject> (chunk.world.config.pathToBlockPrefab), position, rotation);
		go.transform.localScale = new Vector3(Config.Env.BlockSize, Config.Env.BlockSize, Config.Env.BlockSize);

        Vector3 blockPosVec = blockPos;
        MeshData meshData = new MeshData();

		original.AddBlockData(chunk, blockPos, blockPos, meshData);
		for (int i = 0; i < meshData.vertices.Count; i++)
			meshData.vertices[i] -= blockPosVec;

		Mesh mesh = new Mesh();
		mesh.vertices = meshData.vertices.ToArray();
		mesh.triangles = meshData.triangles.ToArray();

		mesh.colors = meshData.colors.ToArray();

		mesh.uv = meshData.uv.ToArray();
		mesh.RecalculateNormals();

		go.GetComponent<Renderer>().sharedMaterial.mainTexture = chunk.world.textureIndex.atlas;
		go.GetComponent<MeshFilter>().mesh = mesh;

		return go;
	}

	public static bool SetBlock(BlockPos pos, Block block, World world)
	{
        return world.SetBlock(pos, block);
	}

	public static Block GetBlock(BlockPos pos, World world)
	{
        return world.GetBlock(pos);
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
        List<Chunk> chunksToSave = new List<Chunk> ();
        chunksToSave.AddRange(world.chunks.chunkCollection);

        //Create a saveprogress object with positions of all the chunks in the world
        //Then save each chunk and update the saveprogress's percentage for each save
        SaveProgress saveProgress;
        if ( !world.UseMultiThreading )
            saveProgress = new SaveProgress(world.chunks.posCollection, chunksToSave);
        else
            saveProgress = new SaveProgress(world.chunks.posCollection);


        if (world.UseMultiThreading) {
			Thread thread = new Thread (() =>
			{
				foreach (var chunk in chunksToSave) {

					while (!chunk.blocks.contentsGenerated) {
						Thread.Sleep (0);
					}

                    if (!Serialization.SaveChunk(chunk))
                        saveProgress.SaveErrorForChunk(chunk.pos);
                    else
                        saveProgress.SaveCompleteForChunk(chunk.pos);
				}
			});
			thread.Start();
		}

		return saveProgress;
	}

    public static VmRaycastHit Raycast(Ray ray, World world, float range = 10000f)
    {
        return VmRaycast.Raycast(ray, world, range);
    }
}