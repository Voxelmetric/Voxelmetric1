using UnityEngine;
using System;

[Serializable]
public static class BlockBuilder
{
    public static void BuildRenderer(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction)
    {
        AddQuadToMeshData(chunk, localPos, globalPos, meshData, direction, false);
    }

    public static void BuildCollider(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction)
    {
        AddQuadToMeshData(chunk, localPos, globalPos, meshData, direction, true);
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

        switch (direction)
        {
            case Direction.up:
                nSolid = chunk.LocalGetBlock(localPos.Add(0, 1, 1)).controller.IsSolid(Direction.south);
                eSolid = chunk.LocalGetBlock(localPos.Add(1, 1, 0)).controller.IsSolid(Direction.west);
                sSolid = chunk.LocalGetBlock(localPos.Add(0, 1, -1)).controller.IsSolid(Direction.north);
                wSolid = chunk.LocalGetBlock(localPos.Add(-1, 1, 0)).controller.IsSolid(Direction.east);

                wnSolid = chunk.LocalGetBlock(localPos.Add(-1, 1, 1)).controller.IsSolid(Direction.east) && chunk.LocalGetBlock(localPos.Add(-1, 1, 1)).controller.IsSolid(Direction.south);
                neSolid = chunk.LocalGetBlock(localPos.Add(1, 1, 1)).controller.IsSolid(Direction.south) && chunk.LocalGetBlock(localPos.Add(1, 1, 1)).controller.IsSolid(Direction.west);
                esSolid = chunk.LocalGetBlock(localPos.Add(1, 1, -1)).controller.IsSolid(Direction.west) && chunk.LocalGetBlock(localPos.Add(1, 1, -1)).controller.IsSolid(Direction.north);
                swSolid = chunk.LocalGetBlock(localPos.Add(-1, 1, -1)).controller.IsSolid(Direction.north) && chunk.LocalGetBlock(localPos.Add(-1, 1, -1)).controller.IsSolid(Direction.east);

                //light = BlockDataMap.NonSolid.Light(chunk.LocalGetBlock(localPos.Add(0, 1, 0)))/ 15f;

                break;
            case Direction.down:
                nSolid = chunk.LocalGetBlock(localPos.Add(0, -1, -1)).controller.IsSolid(Direction.south);
                eSolid = chunk.LocalGetBlock(localPos.Add(1, -1, 0)).controller.IsSolid(Direction.west);
                sSolid = chunk.LocalGetBlock(localPos.Add(0, -1, 1)).controller.IsSolid(Direction.north);
                wSolid = chunk.LocalGetBlock(localPos.Add(-1, -1, 0)).controller.IsSolid(Direction.east);

                wnSolid = chunk.LocalGetBlock(localPos.Add(-1, -1, -1)).controller.IsSolid(Direction.east) && chunk.LocalGetBlock(localPos.Add(-1, -1, -1)).controller.IsSolid(Direction.south);
                neSolid = chunk.LocalGetBlock(localPos.Add(1, -1, -1)).controller.IsSolid(Direction.south) && chunk.LocalGetBlock(localPos.Add(1, -1, -1)).controller.IsSolid(Direction.west);
                esSolid = chunk.LocalGetBlock(localPos.Add(1, -1, 1)).controller.IsSolid(Direction.west) && chunk.LocalGetBlock(localPos.Add(1, -1, 1)).controller.IsSolid(Direction.north);
                swSolid = chunk.LocalGetBlock(localPos.Add(-1, -1, 1)).controller.IsSolid(Direction.north) && chunk.LocalGetBlock(localPos.Add(-1, -1, 1)).controller.IsSolid(Direction.east);

                //light = BlockDataMap.NonSolid.Light(chunk.LocalGetBlock(localPos.Add(0, -1, 0))) / 15f;

                break;
            case Direction.north:
                nSolid = chunk.LocalGetBlock(localPos.Add(1, 0, 1)).controller.IsSolid(Direction.west);
                eSolid = chunk.LocalGetBlock(localPos.Add(0, 1, 1)).controller.IsSolid(Direction.down);
                sSolid = chunk.LocalGetBlock(localPos.Add(-1, 0, 1)).controller.IsSolid(Direction.east);
                wSolid = chunk.LocalGetBlock(localPos.Add(0, -1, 1)).controller.IsSolid(Direction.up);

                esSolid = chunk.LocalGetBlock(localPos.Add(-1, 1, 1)).controller.IsSolid(Direction.east) && chunk.LocalGetBlock(localPos.Add(-1, 1, 1)).controller.IsSolid(Direction.south);
                neSolid = chunk.LocalGetBlock(localPos.Add(1, 1, 1)).controller.IsSolid(Direction.south) && chunk.LocalGetBlock(localPos.Add(1, 1, 1)).controller.IsSolid(Direction.west);
                wnSolid = chunk.LocalGetBlock(localPos.Add(1, -1, 1)).controller.IsSolid(Direction.west) && chunk.LocalGetBlock(localPos.Add(1, -1, 1)).controller.IsSolid(Direction.north);
                swSolid = chunk.LocalGetBlock(localPos.Add(-1, -1, 1)).controller.IsSolid(Direction.north) && chunk.LocalGetBlock(localPos.Add(-1, -1, 1)).controller.IsSolid(Direction.east);

                //light = BlockDataMap.NonSolid.Light(chunk.LocalGetBlock(localPos.Add(0, 0, 1))) / 15f;

                break;
            case Direction.east:
                nSolid = chunk.LocalGetBlock(localPos.Add(1, 0, -1)).controller.IsSolid(Direction.up);
                eSolid = chunk.LocalGetBlock(localPos.Add(1, 1, 0)).controller.IsSolid(Direction.west);
                sSolid = chunk.LocalGetBlock(localPos.Add(1, 0, 1)).controller.IsSolid(Direction.down);
                wSolid = chunk.LocalGetBlock(localPos.Add(1, -1, 0)).controller.IsSolid(Direction.east);

                esSolid = chunk.LocalGetBlock(localPos.Add(1, 1, 1)).controller.IsSolid(Direction.west) && chunk.LocalGetBlock(localPos.Add(1, 1, 1)).controller.IsSolid(Direction.north);
                neSolid = chunk.LocalGetBlock(localPos.Add(1, 1, -1)).controller.IsSolid(Direction.south) && chunk.LocalGetBlock(localPos.Add(1, 1, -1)).controller.IsSolid(Direction.west);
                wnSolid = chunk.LocalGetBlock(localPos.Add(1, -1, -1)).controller.IsSolid(Direction.east) && chunk.LocalGetBlock(localPos.Add(1, -1, -1)).controller.IsSolid(Direction.north);
                swSolid = chunk.LocalGetBlock(localPos.Add(1, -1, 1)).controller.IsSolid(Direction.north) && chunk.LocalGetBlock(localPos.Add(1, -1, 1)).controller.IsSolid(Direction.east);

                //light = BlockDataMap.NonSolid.Light(chunk.LocalGetBlock(localPos.Add(1, 0, 0))) / 15f;

                break;
            case Direction.south:
                nSolid = chunk.LocalGetBlock(localPos.Add(-1, 0, -1)).controller.IsSolid(Direction.down);
                eSolid = chunk.LocalGetBlock(localPos.Add(0, 1, -1)).controller.IsSolid(Direction.west);
                sSolid = chunk.LocalGetBlock(localPos.Add(1, 0, -1)).controller.IsSolid(Direction.up);
                wSolid = chunk.LocalGetBlock(localPos.Add(0, -1, -1)).controller.IsSolid(Direction.south);

                esSolid = chunk.LocalGetBlock(localPos.Add(1, 1, -1)).controller.IsSolid(Direction.west) && chunk.LocalGetBlock(localPos.Add(1, 1, -1)).controller.IsSolid(Direction.north);
                neSolid = chunk.LocalGetBlock(localPos.Add(-1, 1, -1)).controller.IsSolid(Direction.south) && chunk.LocalGetBlock(localPos.Add(-1, 1, -1)).controller.IsSolid(Direction.west);
                wnSolid = chunk.LocalGetBlock(localPos.Add(-1, -1, -1)).controller.IsSolid(Direction.east) && chunk.LocalGetBlock(localPos.Add(-1, -1, -1)).controller.IsSolid(Direction.north);
                swSolid = chunk.LocalGetBlock(localPos.Add(1, -1, -1)).controller.IsSolid(Direction.north) && chunk.LocalGetBlock(localPos.Add(1, -1, -1)).controller.IsSolid(Direction.east);

                //light = BlockDataMap.NonSolid.Light(chunk.LocalGetBlock(localPos.Add(0, 0, -1))) / 15f;

                break;
            case Direction.west:
                nSolid = chunk.LocalGetBlock(localPos.Add(-1, 0, 1)).controller.IsSolid(Direction.up);
                eSolid = chunk.LocalGetBlock(localPos.Add(-1, 1, 0)).controller.IsSolid(Direction.west);
                sSolid = chunk.LocalGetBlock(localPos.Add(-1, 0, -1)).controller.IsSolid(Direction.down);
                wSolid = chunk.LocalGetBlock(localPos.Add(-1, -1, 0)).controller.IsSolid(Direction.east);

                esSolid = chunk.LocalGetBlock(localPos.Add(-1, 1, -1)).controller.IsSolid(Direction.west) && chunk.LocalGetBlock(localPos.Add(-1, 1, -1)).controller.IsSolid(Direction.north);
                neSolid = chunk.LocalGetBlock(localPos.Add(-1, 1, 1)).controller.IsSolid(Direction.south) && chunk.LocalGetBlock(localPos.Add(-1, 1, 1)).controller.IsSolid(Direction.west);
                wnSolid = chunk.LocalGetBlock(localPos.Add(-1, -1, 1)).controller.IsSolid(Direction.east) && chunk.LocalGetBlock(localPos.Add(-1, -1, 1)).controller.IsSolid(Direction.north);
                swSolid = chunk.LocalGetBlock(localPos.Add(-1, -1, -1)).controller.IsSolid(Direction.north) && chunk.LocalGetBlock(localPos.Add(-1, -1, -1)).controller.IsSolid(Direction.east);

                //light = BlockDataMap.NonSolid.Light(chunk.LocalGetBlock(localPos.Add(-1, 0, 0))) / 15f;

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
        Rect texture = textureCollection.GetTexture(chunk, globalPos, direction);
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
                texture = textureCollections[0].GetTexture(chunk, localPos, direction);
                break;
            case Direction.down:
                texture = textureCollections[1].GetTexture(chunk, localPos, direction);
                break;
            case Direction.north:
                texture = textureCollections[2].GetTexture(chunk, localPos, direction);
                break;
            case Direction.east:
                texture = textureCollections[3].GetTexture(chunk, localPos, direction);
                break;
            case Direction.south:
                texture = textureCollections[4].GetTexture(chunk, localPos, direction);
                break;
            case Direction.west:
                texture = textureCollections[5].GetTexture(chunk, localPos, direction);
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

    static void AddQuadToMeshData(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction, bool useCollisionMesh)
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
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z + halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z + halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z - halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z - halfBlock), useCollisionMesh);
                break;
            case Direction.down:
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z - halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z - halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z + halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z + halfBlock), useCollisionMesh);
                break;
            case Direction.north:
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z + halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z + halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z + halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z + halfBlock), useCollisionMesh);
                break;
            case Direction.east:
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z - halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z - halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z + halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z + halfBlock), useCollisionMesh);
                break;
            case Direction.south:
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z - halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z - halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z - halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z - halfBlock), useCollisionMesh);
                break;
            case Direction.west:
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z + halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z + halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z - halfBlock), useCollisionMesh);
                meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z - halfBlock), useCollisionMesh);
                break;
            default:
                Debug.LogError("Direction not recognized");
                break;
        }

        meshData.AddQuadTriangles(useCollisionMesh);
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
