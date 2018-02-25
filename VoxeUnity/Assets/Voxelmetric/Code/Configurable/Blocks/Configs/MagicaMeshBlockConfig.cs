using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code;
using Voxelmetric.Code.Builders;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Geometry.Buffers;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Utilities.Import;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

public class MagicaMeshBlockConfig: BlockConfig
{
    private Vector3 m_meshOffset;
    private string m_path;
    private float m_scale;

    public int[] tris { get; private set; }
    public Vector3[] verts { get; private set; }
    public Color32[] colors { get; private set; }

    public override bool OnSetUp(Hashtable config, World world)
    {
        if (!base.OnSetUp(config, world))
            return false;
        
        m_meshOffset = new Vector3(
            Env.BlockSizeHalf+float.Parse(_GetPropertyFromConfig(config, "meshXOffset", "0"), CultureInfo.InvariantCulture),
            Env.BlockSizeHalf+float.Parse(_GetPropertyFromConfig(config, "meshYOffset", "0"), CultureInfo.InvariantCulture),
            Env.BlockSizeHalf+float.Parse(_GetPropertyFromConfig(config, "meshZOffset", "0"), CultureInfo.InvariantCulture)
        );
        m_path = _GetPropertyFromConfig(config, "meshFileLocation", "");

        long scaleInv;
        if (!_GetPropertyFromConfig(config, "scaleInv", out scaleInv) || scaleInv<=0)
            scaleInv = 1;
        m_scale = 1f / scaleInv;

        return true;
    }

    public override bool OnPostSetUp(World world)
    {
        return SetUpMesh(world, world.config.meshFolder+"/"+m_path, m_meshOffset);
    }

    private bool SetUpMesh(World world, string meshLocation, Vector3 positionOffset)
    {
        FileStream fs = null;
        try
        {
            string fullPath = Directories.ResourcesFolder+"/"+meshLocation+".vox";
            fs = new FileStream(fullPath, FileMode.Open);
            using (BinaryReader br = new BinaryReader(fs))
            {
                // Load the magica vox model
                var data = MagicaVox.FromMagica(br);
                if (data==null)
                    return false;

                MagicaVox.MagicaVoxelChunk mvchunk = data.chunk;

                // Determine the biggest side
                int size = mvchunk.sizeX;
                if (mvchunk.sizeY>size)
                    size = mvchunk.sizeY;
                if (mvchunk.sizeZ>size)
                    size = mvchunk.sizeZ;

                // Determine the necessary size
                size += Env.ChunkPadding2;
                int pow = 1 + (int)Math.Log(size, 2);
                size = (1<<pow)-Env.ChunkPadding2;

                // Create a temporary chunk object
                Chunk chunk = new Chunk(size);
                chunk.Init(world, Vector3Int.zero);
                ChunkBlocks blocks = chunk.blocks;

                // Convert the model's data to our internal system
                for (int y = 0; y<mvchunk.sizeY; y++)
                {
                    for (int z = 0; z<mvchunk.sizeZ; z++)
                    {
                        for (int x = 0; x<mvchunk.sizeX; x++)
                        {
                            int index = Helpers.GetChunkIndex1DFrom3D(x, y, z, pow);
                            int i = Helpers.GetIndex1DFrom3D(x, y, z, mvchunk.sizeX, mvchunk.sizeZ);

                            ushort blockIndex = data.chunk.data[i];

                            Block colorBlock = world.blockProvider.BlockTypes[blockIndex];
                            blocks.SetInner(
                                index, data.chunk.data[i]==0
                                           ? BlockProvider.AirBlock
                                           : new BlockData(blockIndex, colorBlock.Solid)
                            );
                        }
                    }
                }

                Block block = world.blockProvider.BlockTypes[type];
                block.Custom = false;
                {
                    // Build the mesh
                    CubeMeshBuilder meshBuilder = new CubeMeshBuilder(m_scale, size)
                    {
                        SideMask = 0,
                        Palette = data.palette
                    };
                    meshBuilder.Build(chunk, out chunk.minBounds, out chunk.maxBounds);

                    var batcher = chunk.GeometryHandler.Batcher;
                    if (batcher.Buffers!=null && batcher.Buffers.Length>0)
                    {

                        List<Vector3> tmpVertices = new List<Vector3>();
                        List<Color32> tmpColors = new List<Color32>();
                        List<int> tmpTriangles = new List<int>();

                        // Only consider the default material for now
                        var buff = batcher.Buffers[0];
                        for (int i=0; i<buff.Count; i++)
                        {
                            tmpVertices.AddRange(buff[i].Vertices);
                            tmpColors.AddRange(buff[i].Colors);
                            tmpTriangles.AddRange(buff[i].Triangles);
                        }

                        // Convert lists to arrays
                        verts = tmpVertices.ToArray();
                        colors = tmpColors.ToArray();
                        tris = tmpTriangles.ToArray();
                    }
                }
                block.Custom = true;

                fs = null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
        finally
        {
            if (fs!=null)
                fs.Dispose();
        }

        return true;
    }
}
