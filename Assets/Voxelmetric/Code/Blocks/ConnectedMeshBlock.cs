using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class BlockConnected : Block
{
    //public BlockConnected() : base() { }
    //protected int[] connectsTo;
    //public string blockName;

    //public List<Vector3[]> verts = new List<Vector3[]>();
    //public List<int[]> tris = new List<int[]>();
    //public List<Vector2[]> uvs = new List<Vector2[]>();

    //public TextureCollection collection;

    //public override void SetUpController(BlockConfig config, World world)
    //{
    //    blockName = config.name;
    //    collection = world.textureIndex.GetTextureCollection(config.textures[0]);
    //    this.config = config;

    //    for (int i = 0; i < 7; i++)
    //    {
    //        verts.Add(null);
    //        tris.Add(null);
    //        uvs.Add(null);
    //    }

    //    GetMeshFromConfig(world.config.meshFolder, "meshUp", DirectionUtils.Get(Direction.up));
    //    GetMeshFromConfig(world.config.meshFolder, "meshDown", DirectionUtils.Get(Direction.down));
    //    GetMeshFromConfig(world.config.meshFolder, "meshNorth", DirectionUtils.Get(Direction.north));
    //    GetMeshFromConfig(world.config.meshFolder, "meshEast", DirectionUtils.Get(Direction.east));
    //    GetMeshFromConfig(world.config.meshFolder, "meshSouth", DirectionUtils.Get(Direction.south));
    //    GetMeshFromConfig(world.config.meshFolder, "meshWest", DirectionUtils.Get(Direction.west));
    //    GetMeshFromConfig(world.config.meshFolder, "meshDefault", 6);

    //    base.SetUpController(config, world);
    //}

    //public override Block OnCreate(Chunk chunk, BlockPos pos, Block block)
    //{
    //    if (connectsTo == null)
    //    {
            
    //        string[] connectsToNames = ((string)config.additionalProperties["connectsTo"]).Split(',');
    //        connectsTo = new int[connectsToNames.Length];
    //        for (int i = 0; i < connectsToNames.Length; i++)
    //        {
    //            connectsTo[i] = chunk.world.blockIndex.names[connectsToNames[i]];
    //        }
    //    } 

    //    return base.OnCreate(chunk, pos, block);
    //}

    //void GetMeshFromConfig(string meshFolder, string keyName, int arrayIndex) {
    //    if (config.additionalProperties.ContainsKey(keyName))
    //    {
    //        Vector3 offset = new Vector3();
    //        if (config.additionalProperties.ContainsKey(keyName + "Offset"))
    //        {
    //            Debug.Log((string)config.additionalProperties[keyName + "Offset"]);
    //            string[] offsetParams = ((string)config.additionalProperties[keyName + "Offset"]).Split(',');
    //            offset = new Vector3(float.Parse(offsetParams[0]), float.Parse(offsetParams[1]), float.Parse(offsetParams[2]));
    //        }

    //        SetUpMeshes(meshFolder + "/" + (string)config.additionalProperties[keyName], this, offset, arrayIndex);
    //    } else {
    //        verts[arrayIndex] = new Vector3[0];
    //        tris[arrayIndex] = new int[0];
    //        uvs[arrayIndex] = new Vector2[0];
    //    }
    //}

    //public override void AddBlockData(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Block block)
    //{
    //    for (int d = 0; d < 6; d++)
    //    {
    //        for (int i = 0; i < connectsTo.Length; i++)
    //        {
    //            Direction dir = DirectionUtils.Get(d);
    //            if (chunk.blocks.LocalGet(localPos.Add(dir)).type == connectsTo[i])
    //            {
    //                BuildFace(chunk, localPos, globalPos, meshData, dir, block);
    //                break;
    //            }
    //        }
    //    }

    //    AlwaysBuild(chunk, localPos, globalPos, meshData, block);
    //}

    //public virtual void BuildFace(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction, Block block)
    //{
    //    BuildMeshForIndex(chunk, localPos, globalPos, meshData, DirectionUtils.Get(direction), block);
    //}

    //public virtual void AlwaysBuild(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Block block)
    //{
    //    //The array at index 6 is beyond the arrays that correspond to directions
    //    BuildMeshForIndex(chunk, localPos, globalPos, meshData, 6, block);
    //}

    //protected virtual void BuildMeshForIndex(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, int index, Block block)
    //{
    //    int initialVertCount = meshData.vertices.Count;
    //    int colInitialVertCount = meshData.colVertices.Count;

    //    for (int i = 0; i < verts[index].Length; i++)
    //    {
    //        meshData.AddVertex(verts[index][i] + (Vector3)localPos);
    //        meshData.colVertices.Add(verts[index][i] + (Vector3)localPos);

    //        if (uvs[index].Length == 0)
    //            meshData.uv.Add(new Vector2(0, 0));

    //        //Coloring of blocks is not yet implemented so just pass in full brightness
    //        meshData.colors.Add(new Color(1, 1, 1, 1));
    //    }

    //    if (uvs[index].Length != 0)
    //    {
    //        Rect texture;
    //        if (collection != null)
    //            texture = collection.GetTexture(chunk, localPos, globalPos, Direction.down);
    //        else
    //            texture = new Rect();


    //        for (int i = 0; i < uvs[index].Length; i++)
    //        {
    //            meshData.uv.Add(new Vector2(
    //                (uvs[index][i].x * texture.width) + texture.x,
    //                (uvs[index][i].y * texture.height) + texture.y)
    //            );
    //        }
    //    }

    //    for (int i = 0; i < tris[index].Length; i++)
    //    {
    //        meshData.AddTriangle(tris[index][i] + initialVertCount);
    //        meshData.colTriangles.Add(tris[index][i] + colInitialVertCount);
    //    }
    //}

    //public override string Name(Block block) { return blockName; }

    //public override bool IsSolid(Block block, Direction direction) { return false; }

    //public override bool CanBeWalkedOn(Block block) { return false; }

    //public override bool CanBeWalkedThrough(Block block) { return false; }

    //public static void SetUpMeshes(string meshLocation, BlockConnected controller, Vector3 positionOffset, int arrayIndex)
    //{
    //    GameObject meshGO = (GameObject)Resources.Load(meshLocation);

    //    List<Vector3> verts = new List<Vector3>();
    //    List<int> tris = new List<int>();
    //    List<Vector2> uvs = new List<Vector2>();

    //    for (int GOIndex = 0; GOIndex < meshGO.transform.childCount; GOIndex++)
    //    {
    //        Mesh mesh = meshGO.transform.GetChild(GOIndex).GetComponent<MeshFilter>().sharedMesh;

    //        for (int i = 0; i < mesh.vertices.Length; i++)
    //        {
    //            verts.Add(mesh.vertices[i] + positionOffset);
    //        }

    //        for (int i = 0; i < mesh.triangles.Length; i++)
    //        {
    //            tris.Add(mesh.triangles[i]);
    //        }

    //        for (int i = 0; i < mesh.uv.Length; i++)
    //        {
    //            uvs.Add(mesh.uv[i]);
    //        }
    //    }

    //    controller.tris[arrayIndex] = tris.ToArray();
    //    controller.verts[arrayIndex] = verts.ToArray();
    //    controller.uvs[arrayIndex] = uvs.ToArray();
    //}
}