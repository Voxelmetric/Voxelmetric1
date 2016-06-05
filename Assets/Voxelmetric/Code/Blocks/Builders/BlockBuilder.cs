using UnityEngine;
using System;

[Serializable]
public static class BlockBuilder
{
    public static void BuildRenderer(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction)
    {
        AddQuadToMeshData(chunk, localPos, globalPos, meshData, direction);
    }

    public static void BuildColors(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction)
    {
        bool nSolid = false;
        bool eSolid = false;
        bool sSolid = false;
        bool wSolid = false;

        bool wnSolid = false;
        bool neSolid = false;
        bool esSolid = false;
        bool swSolid = false;

        //float light = 0;

        ChunkBlocks blocks = chunk.blocks;

        switch (direction)
        {
            case Direction.up:
                nSolid = blocks.LocalGet(localPos.Add(0, 1, 1)).IsSolid(Direction.south);
                eSolid = blocks.LocalGet(localPos.Add(1, 1, 0)).IsSolid(Direction.west);
                sSolid = blocks.LocalGet(localPos.Add(0, 1, -1)).IsSolid(Direction.north);
                wSolid = blocks.LocalGet(localPos.Add(-1, 1, 0)).IsSolid(Direction.east);

                wnSolid = blocks.LocalGet(localPos.Add(-1, 1, 1)).IsSolid(Direction.east) && blocks.LocalGet(localPos.Add(-1, 1, 1)).IsSolid(Direction.south);
                neSolid = blocks.LocalGet(localPos.Add(1, 1, 1)).IsSolid(Direction.south) && blocks.LocalGet(localPos.Add(1, 1, 1)).IsSolid(Direction.west);
                esSolid = blocks.LocalGet(localPos.Add(1, 1, -1)).IsSolid(Direction.west) && blocks.LocalGet(localPos.Add(1, 1, -1)).IsSolid(Direction.north);
                swSolid = blocks.LocalGet(localPos.Add(-1, 1, -1)).IsSolid(Direction.north) && blocks.LocalGet(localPos.Add(-1, 1, -1)).IsSolid(Direction.east);

                //light = BlockDataMap.NonSolid.Light(blocks.LocalGet(localPos.Add(0, 1, 0)))/ 15f;

                break;
            case Direction.down:
                nSolid = blocks.LocalGet(localPos.Add(0, -1, -1)).IsSolid(Direction.south);
                eSolid = blocks.LocalGet(localPos.Add(1, -1, 0)).IsSolid(Direction.west);
                sSolid = blocks.LocalGet(localPos.Add(0, -1, 1)).IsSolid(Direction.north);
                wSolid = blocks.LocalGet(localPos.Add(-1, -1, 0)).IsSolid(Direction.east);

                wnSolid = blocks.LocalGet(localPos.Add(-1, -1, -1)).IsSolid(Direction.east) && blocks.LocalGet(localPos.Add(-1, -1, -1)).IsSolid(Direction.south);
                neSolid = blocks.LocalGet(localPos.Add(1, -1, -1)).IsSolid(Direction.south) && blocks.LocalGet(localPos.Add(1, -1, -1)).IsSolid(Direction.west);
                esSolid = blocks.LocalGet(localPos.Add(1, -1, 1)).IsSolid(Direction.west) && blocks.LocalGet(localPos.Add(1, -1, 1)).IsSolid(Direction.north);
                swSolid = blocks.LocalGet(localPos.Add(-1, -1, 1)).IsSolid(Direction.north) && blocks.LocalGet(localPos.Add(-1, -1, 1)).IsSolid(Direction.east);

                //light = BlockDataMap.NonSolid.Light(blocks.LocalGet(localPos.Add(0, -1, 0))) / 15f;

                break;
            case Direction.north:
                nSolid = blocks.LocalGet(localPos.Add(1, 0, 1)).IsSolid(Direction.west);
                eSolid = blocks.LocalGet(localPos.Add(0, 1, 1)).IsSolid(Direction.down);
                sSolid = blocks.LocalGet(localPos.Add(-1, 0, 1)).IsSolid(Direction.east);
                wSolid = blocks.LocalGet(localPos.Add(0, -1, 1)).IsSolid(Direction.up);

                esSolid = blocks.LocalGet(localPos.Add(-1, 1, 1)).IsSolid(Direction.east) && blocks.LocalGet(localPos.Add(-1, 1, 1)).IsSolid(Direction.south);
                neSolid = blocks.LocalGet(localPos.Add(1, 1, 1)).IsSolid(Direction.south) && blocks.LocalGet(localPos.Add(1, 1, 1)).IsSolid(Direction.west);
                wnSolid = blocks.LocalGet(localPos.Add(1, -1, 1)).IsSolid(Direction.west) && blocks.LocalGet(localPos.Add(1, -1, 1)).IsSolid(Direction.north);
                swSolid = blocks.LocalGet(localPos.Add(-1, -1, 1)).IsSolid(Direction.north) && blocks.LocalGet(localPos.Add(-1, -1, 1)).IsSolid(Direction.east);

                //light = BlockDataMap.NonSolid.Light(blocks.LocalGet(localPos.Add(0, 0, 1))) / 15f;

                break;
            case Direction.east:
                nSolid = blocks.LocalGet(localPos.Add(1, 0, -1)).IsSolid(Direction.up);
                eSolid = blocks.LocalGet(localPos.Add(1, 1, 0)).IsSolid(Direction.west);
                sSolid = blocks.LocalGet(localPos.Add(1, 0, 1)).IsSolid(Direction.down);
                wSolid = blocks.LocalGet(localPos.Add(1, -1, 0)).IsSolid(Direction.east);

                esSolid = blocks.LocalGet(localPos.Add(1, 1, 1)).IsSolid(Direction.west) && blocks.LocalGet(localPos.Add(1, 1, 1)).IsSolid(Direction.north);
                neSolid = blocks.LocalGet(localPos.Add(1, 1, -1)).IsSolid(Direction.south) && blocks.LocalGet(localPos.Add(1, 1, -1)).IsSolid(Direction.west);
                wnSolid = blocks.LocalGet(localPos.Add(1, -1, -1)).IsSolid(Direction.east) && blocks.LocalGet(localPos.Add(1, -1, -1)).IsSolid(Direction.north);
                swSolid = blocks.LocalGet(localPos.Add(1, -1, 1)).IsSolid(Direction.north) && blocks.LocalGet(localPos.Add(1, -1, 1)).IsSolid(Direction.east);

                //light = BlockDataMap.NonSolid.Light(blocks.LocalGet(localPos.Add(1, 0, 0))) / 15f;

                break;
            case Direction.south:
                nSolid = blocks.LocalGet(localPos.Add(-1, 0, -1)).IsSolid(Direction.down);
                eSolid = blocks.LocalGet(localPos.Add(0, 1, -1)).IsSolid(Direction.west);
                sSolid = blocks.LocalGet(localPos.Add(1, 0, -1)).IsSolid(Direction.up);
                wSolid = blocks.LocalGet(localPos.Add(0, -1, -1)).IsSolid(Direction.south);

                esSolid = blocks.LocalGet(localPos.Add(1, 1, -1)).IsSolid(Direction.west) && blocks.LocalGet(localPos.Add(1, 1, -1)).IsSolid(Direction.north);
                neSolid = blocks.LocalGet(localPos.Add(-1, 1, -1)).IsSolid(Direction.south) && blocks.LocalGet(localPos.Add(-1, 1, -1)).IsSolid(Direction.west);
                wnSolid = blocks.LocalGet(localPos.Add(-1, -1, -1)).IsSolid(Direction.east) && blocks.LocalGet(localPos.Add(-1, -1, -1)).IsSolid(Direction.north);
                swSolid = blocks.LocalGet(localPos.Add(1, -1, -1)).IsSolid(Direction.north) && blocks.LocalGet(localPos.Add(1, -1, -1)).IsSolid(Direction.east);

                //light = BlockDataMap.NonSolid.Light(blocks.LocalGet(localPos.Add(0, 0, -1))) / 15f;

                break;
            case Direction.west:
                nSolid = blocks.LocalGet(localPos.Add(-1, 0, 1)).IsSolid(Direction.up);
                eSolid = blocks.LocalGet(localPos.Add(-1, 1, 0)).IsSolid(Direction.west);
                sSolid = blocks.LocalGet(localPos.Add(-1, 0, -1)).IsSolid(Direction.down);
                wSolid = blocks.LocalGet(localPos.Add(-1, -1, 0)).IsSolid(Direction.east);

                esSolid = blocks.LocalGet(localPos.Add(-1, 1, -1)).IsSolid(Direction.west) && blocks.LocalGet(localPos.Add(-1, 1, -1)).IsSolid(Direction.north);
                neSolid = blocks.LocalGet(localPos.Add(-1, 1, 1)).IsSolid(Direction.south) && blocks.LocalGet(localPos.Add(-1, 1, 1)).IsSolid(Direction.west);
                wnSolid = blocks.LocalGet(localPos.Add(-1, -1, 1)).IsSolid(Direction.east) && blocks.LocalGet(localPos.Add(-1, -1, 1)).IsSolid(Direction.north);
                swSolid = blocks.LocalGet(localPos.Add(-1, -1, -1)).IsSolid(Direction.north) && blocks.LocalGet(localPos.Add(-1, -1, -1)).IsSolid(Direction.east);

                //light = BlockDataMap.NonSolid.Light(blocks.LocalGet(localPos.Add(-1, 0, 0))) / 15f;

                break;
            default:
                Debug.LogError("Direction not recognized");
                break;
        }

        if (chunk.world.config.addAOToMesh)
        {
            AddColorsAO(meshData, wnSolid, nSolid, neSolid, eSolid, esSolid, sSolid, swSolid, wSolid, chunk.world.config.ambientOcclusionStrength);
        }
        else
        {
            meshData.AddColors(1, 1, 1, 1, 1);
        }
    }

    public static void BuildTexture(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction, TextureCollection textureCollection)
    {
        Rect texture = textureCollection.GetTexture(chunk, localPos, globalPos, direction);
        Vector2[] UVs = new Vector2[4];

        UVs[0] = new Vector2(texture.x + texture.width, texture.y);
        UVs[1] = new Vector2(texture.x + texture.width, texture.y + texture.height);
        UVs[2] = new Vector2(texture.x, texture.y + texture.height);
        UVs[3] = new Vector2(texture.x, texture.y);

        meshData.uv.AddRange(UVs);
    }

    public static void BuildTexture(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction, TextureCollection[] textureCollections)
    {
        Rect texture = new Rect();

        switch (direction)
        {
            case Direction.up:
                texture = textureCollections[0].GetTexture(chunk, localPos, globalPos, direction);
                break;
            case Direction.down:
                texture = textureCollections[1].GetTexture(chunk, localPos, globalPos, direction);
                break;
            case Direction.north:
                texture = textureCollections[2].GetTexture(chunk, localPos, globalPos, direction);
                break;
            case Direction.east:
                texture = textureCollections[3].GetTexture(chunk, localPos, globalPos, direction);
                break;
            case Direction.south:
                texture = textureCollections[4].GetTexture(chunk, localPos, globalPos, direction);
                break;
            case Direction.west:
                texture = textureCollections[5].GetTexture(chunk, localPos, globalPos, direction);
                break;
            default:
                break;
        }

        Vector2[] UVs = new Vector2[4];

        UVs[0] = new Vector2(texture.x + texture.width, texture.y);
        UVs[1] = new Vector2(texture.x + texture.width, texture.y + texture.height);
        UVs[2] = new Vector2(texture.x, texture.y + texture.height);
        UVs[3] = new Vector2(texture.x, texture.y);

        meshData.uv.AddRange(UVs);
    }

    static void AddQuadToMeshData(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction)
    {
        //Adding a tiny overlap between block meshes may solve floating point imprecision
        //errors causing pixel size gaps between blocks when looking closely
        float halfBlock = (Config.Env.BlockSize / 2) + Config.Env.BlockFacePadding;

        //Converting the position to a vector adjusts it based on block size and gives us real world coordinates for x, y and z
        Vector3 vPos = localPos;
        //Vector3 vPos = (pos - chunk.pos);

        switch (direction)
        {
            case Direction.up:
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z + halfBlock));
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z + halfBlock));
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z - halfBlock));
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z - halfBlock));
                break;
            case Direction.down:
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z - halfBlock));
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z - halfBlock));
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z + halfBlock));
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z + halfBlock));
                break;
            case Direction.north:
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z + halfBlock));
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z + halfBlock));
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z + halfBlock));
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z + halfBlock));
                break;
            case Direction.east:
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z - halfBlock));
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z - halfBlock));
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z + halfBlock));
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z + halfBlock));
                break;
            case Direction.south:
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z - halfBlock));
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z - halfBlock));
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z - halfBlock));
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z - halfBlock));
                break;
            case Direction.west:
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z + halfBlock));
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z + halfBlock));
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z - halfBlock));
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z - halfBlock));
                break;
            default:
                Debug.LogError("Direction not recognized");
                break;
        }

        meshData.AddQuadTriangles();
    }

    static void AddColorsAO(MeshData meshData, bool wnSolid, bool nSolid, bool neSolid, bool eSolid, bool esSolid, bool sSolid, bool swSolid, bool wSolid, float strength)
    {
        float ne = 1;
        float es = 1;
        float sw = 1;
        float wn = 1;

        strength /= 2;

        if (nSolid)
        {
            wn -= strength;
            ne -= strength;
        }

        if (eSolid)
        {
            ne -= strength;
            es -= strength;
        }

        if (sSolid)
        {
            es -= strength;
            sw -= strength;
        }

        if (wSolid)
        {
            sw -= strength;
            wn -= strength;
        }

        if (neSolid)
            ne -= strength;

        if (swSolid)
            sw -= strength;

        if (wnSolid)
            wn -= strength;

        if (esSolid)
            es -= strength;

        meshData.AddColors(ne, es, sw, wn, 1);
    }
}
