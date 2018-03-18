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
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.Utilities.Import;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

public class CustomMeshBlockConfig: BlockConfig
{
    public class CustomMeshBlockData
    {
        public int[] tris;
        public Vector3[] verts;
        public Vector2[] uvs;
        public Color32[] colors;
        public TextureCollection textures;
    }

    private readonly CustomMeshBlockData m_data = new CustomMeshBlockData();
    public CustomMeshBlockData data { get { return m_data; }  }

    protected Vector3 m_meshOffset;
    protected string m_path;
    protected float m_scale;

    public override bool OnSetUp(Hashtable config, World world)
    {
        if (!base.OnSetUp(config, world))
            return false;

        solid = _GetPropertyFromConfig(config, "solid", false);
        m_data.textures = world.textureProvider.GetTextureCollection(_GetPropertyFromConfig(config, "texture", ""));

        m_meshOffset = new Vector3(
            Env.BlockSizeHalf + float.Parse(_GetPropertyFromConfig(config, "meshXOffset", "0"), CultureInfo.InvariantCulture),
            Env.BlockSizeHalf + float.Parse(_GetPropertyFromConfig(config, "meshYOffset", "0"), CultureInfo.InvariantCulture),
            Env.BlockSizeHalf + float.Parse(_GetPropertyFromConfig(config, "meshZOffset", "0"), CultureInfo.InvariantCulture)
        );
        m_path = _GetPropertyFromConfig(config, "meshFileLocation", "");

        long scaleInv;
        if (!_GetPropertyFromConfig(config, "scaleInv", out scaleInv) || scaleInv <= 0)
            scaleInv = 1;
        m_scale = 1f / scaleInv;
        
        return true;
    }

    public override bool OnPostSetUp(World world)
    {
        string meshLocation = world.config.meshFolder + "/" + m_path;
        return SetUpMesh(
            world,
            meshLocation,
            type,
            m_meshOffset,
            m_scale,
            out m_data.tris,
            out m_data.verts,
            out m_data.uvs,
            out m_data.colors
        );
    }

    private static bool BuildCustomMesh(
        string meshLocation,
        Vector3 meshOffset,
        float meshScale, // TODO: Implement scaling
        out int[] trisOut,
        out Vector3[] vertsOut,
        out Vector2[] uvsOut,
        out Color32[] colorsOut
    )
    {
        trisOut = null;
        vertsOut = null;
        uvsOut = null;
        colorsOut = null;

        // TODO: Why not simply holding a mesh object instead of creating all these arrays?
        GameObject meshGO = (GameObject)Resources.Load(meshLocation);

        int vertexCnt = 0;
        int triangleCnt = 0;

        //bool hasColors = false;
        //bool hasUVs = false;

        for (int GOIndex = 0; GOIndex<meshGO.transform.childCount; GOIndex++)
        {
            Mesh mesh = meshGO.transform.GetChild(GOIndex).GetComponent<MeshFilter>().sharedMesh;

            vertexCnt += mesh.vertices.Length;
            triangleCnt += mesh.triangles.Length;

            // Check whether allocating space for UVs is necessary
            //if (!hasUVs && mesh.uv != null && mesh.uv.Length > 0)
            //    hasUVs = true;

            // Check whether allocating space for colors is necessary
            //if (!hasColors && mesh.colors32 != null && mesh.colors32.Length > 0)
            //    hasColors = true;
        }

        // 6 indices & 4 vertices per quad
        Assert.IsTrue((vertexCnt * 3)>>1==triangleCnt);
        if ((vertexCnt * 3)>>1!=triangleCnt)
        {
            // A bad resource
            Debug.LogErrorFormat("Error loading mesh {0}. Number of triangles and vertices do not match!", meshLocation);
            return false;
        }

        trisOut = new int[triangleCnt];
        vertsOut = new Vector3[vertexCnt];
        //if (hasUVs)
            uvsOut = new Vector2[vertexCnt];
        //if (hasColors)
            colorsOut = new Color32[vertexCnt];

        int ti=0, vi=0;

        for (int GOIndex = 0; GOIndex<meshGO.transform.childCount; GOIndex++)
        {
            Mesh mesh = meshGO.transform.GetChild(GOIndex).GetComponent<MeshFilter>().sharedMesh;

            for (int i = 0; i<mesh.vertices.Length; i++, vi++)
            {
                vertsOut[vi] = mesh.vertices[i]+ meshOffset;

                //if (hasUVs)
                    uvsOut[vi] = mesh.uv.Length!=0 ? mesh.uv[i] : new Vector2();

                //if (hasColors)
                    colorsOut[vi] = mesh.colors32.Length!=0 ? mesh.colors32[i] : new Color32(255, 255, 255, 255);
            }

            for (int i = 0; i<mesh.triangles.Length; i++, ti++)
                trisOut[ti] = mesh.triangles[i];
        }

        return true;
    }

    private static bool BuildMagicaMesh(
        World world,
        string meshLocation,
        ushort blockType,
        Vector3 meshOffset,
        float meshScale,
        out int[] trisOut,
        out Vector3[] vertsOut,
        out Color32[] colorsOut
    )
    {
        trisOut = null;
        vertsOut = null;
        colorsOut = null;

        FileStream fs = null;
        try
        {
            string fullPath = Directories.ResourcesFolder + "/" + meshLocation;
            fs = new FileStream(fullPath, FileMode.Open);
            using (BinaryReader br = new BinaryReader(fs))
            {
                // Load the magica vox model
                var data = MagicaVox.FromMagica(br);
                if (data == null)
                    return false;

                MagicaVox.MagicaVoxelChunk mvchunk = data.chunk;

                // Determine the biggest side
                int size = mvchunk.sizeX;
                if (mvchunk.sizeY > size)
                    size = mvchunk.sizeY;
                if (mvchunk.sizeZ > size)
                    size = mvchunk.sizeZ;

                // Determine the necessary size
                size += Env.ChunkPadding2;
                int pow = 1 + (int)Math.Log(size, 2);
                size = (1 << pow) - Env.ChunkPadding2;

                // Create a temporary chunk object
                Chunk chunk = new Chunk(size);
                chunk.Init(world, Vector3Int.zero);
                ChunkBlocks blocks = chunk.Blocks;

                // Convert the model's data to our internal system
                for (int y = 0; y < mvchunk.sizeY; y++)
                {
                    for (int z = 0; z < mvchunk.sizeZ; z++)
                    {
                        for (int x = 0; x < mvchunk.sizeX; x++)
                        {
                            int index = Helpers.GetChunkIndex1DFrom3D(x, y, z, pow);
                            int i = Helpers.GetIndex1DFrom3D(x, y, z, mvchunk.sizeX, mvchunk.sizeZ);

                            ushort blockIndex = data.chunk.data[i];

                            Block colorBlock = world.blockProvider.BlockTypes[blockIndex];
                            blocks.SetInner(
                                index, data.chunk.data[i] == 0
                                           ? BlockProvider.AirBlock
                                           : new BlockData(blockIndex, colorBlock.Solid)
                            );
                        }
                    }
                }

                Block block = world.blockProvider.BlockTypes[blockType];
                block.Custom = false;
                {
                    // Build the mesh
                    CubeMeshBuilder meshBuilder = new CubeMeshBuilder(meshScale, size)
                    {
                        SideMask = 0,
                        Palette = data.palette
                    };
                    meshBuilder.Build(chunk, out chunk.MinBounds, out chunk.NaxBounds);

                    var batcher = chunk.GeometryHandler.Batcher;
                    if (batcher.Buffers != null && batcher.Buffers.Length > 0)
                    {

                        List<Vector3> tmpVertices = new List<Vector3>();
                        List<Color32> tmpColors = new List<Color32>();
                        List<int> tmpTriangles = new List<int>();

                        // Only consider the default material for now
                        var buff = batcher.Buffers[0];
                        for (int i = 0; i < buff.Count; i++)
                        {
                            int sx = tmpVertices.Count;
                            tmpVertices.AddRange(buff[i].Vertices);
                            if (meshOffset!=Vector3.zero)
                            {
                                for (int j = sx; j<buff[i].Vertices.Count; j++)
                                    tmpVertices[j] += meshOffset;
                            }
                            tmpColors.AddRange(buff[i].Colors);
                            tmpTriangles.AddRange(buff[i].Triangles);
                        }

                        // Convert lists to arrays
                        vertsOut = tmpVertices.ToArray();
                        colorsOut = tmpColors.ToArray();
                        trisOut = tmpTriangles.ToArray();
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
            if (fs != null)
                fs.Dispose();
        }

        return true;
    }

    protected bool SetUpMesh(
        World world,
        string meshLocation,
        ushort blockType,
        Vector3 meshOffset,
        float meshScale,
        out int[] trisOut,
        out Vector3[] vertsOut,
        out Vector2[] uvsOut,
        out Color32[] colorsOut
        )
    {
        if (m_path.EndsWith(".vox"))
        {
            uvsOut = null;
            return BuildMagicaMesh(world, meshLocation, blockType, meshOffset, meshScale, out trisOut, out vertsOut, out colorsOut);
        }
        else
        {
            return BuildCustomMesh(meshLocation, meshOffset, meshScale, out trisOut, out vertsOut, out uvsOut, out colorsOut);
        }
    }
}
