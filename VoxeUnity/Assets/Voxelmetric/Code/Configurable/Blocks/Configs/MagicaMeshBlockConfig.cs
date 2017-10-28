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
    private RenderGeometryBuffer m_geomBuffer = null;
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

                            // TODO: Implement support for colors
                            blocks.SetInner(
                                index, data.chunk.data[i]==0
                                           ? BlockProvider.AirBlock
                                           : new BlockData(type, true)//new BlockData((ushort)i, true)
                            );
                        }
                    }
                }

                Block block = world.blockProvider.BlockTypes[type];

                block.Custom = false;
                m_geomBuffer = new RenderGeometryBuffer()
                {
                    Colors = new List<Color32>()
                };

                {
                    // Build the mesh
                    CubeMeshBuilder meshBuilder = new CubeMeshBuilder(m_scale, size)
                    {
                        SideMask = 0,
                        Type = type,
                        Palette = data.palette
                    };
                    meshBuilder.Build(chunk);

                    // Convert lists to arrays
                    verts = m_geomBuffer.Vertices.ToArray();
                    colors = m_geomBuffer.Colors.ToArray();
                    tris = m_geomBuffer.Triangles.ToArray();
                }
                m_geomBuffer = null;
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

    public bool IsInitPhase
    {
        get { return m_geomBuffer!=null; }
    }

    public void AddFace(Vector3[] verts, Color32[] cols, bool backFace)
    {
        Assert.IsTrue(verts.Length==4);

        // Add data to the render buffer
        m_geomBuffer.Vertices.AddRange(verts);
        m_geomBuffer.Colors.AddRange(cols);
        m_geomBuffer.AddIndices(m_geomBuffer.Vertices.Count, backFace);
    }
}
