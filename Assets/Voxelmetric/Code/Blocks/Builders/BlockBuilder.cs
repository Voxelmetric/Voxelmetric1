using UnityEngine;
using System;

[Serializable]
public static class BlockBuilder
{
    public static void BuildRenderer(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction)
    {
        AddQuadToMeshData(chunk, pos, meshData, direction, false);
    }

    public static void BuildCollider(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction)
    {
        AddQuadToMeshData(chunk, pos, meshData, direction, true);
    }

    public static void BuildColors(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction)
    {
        bool nSolid = false;
        bool eSolid = false;
        bool sSolid = false;
        bool wSolid = false;

        bool wnSolid = false;
        bool neSolid = false;
        bool esSolid = false;
        bool swSolid = false;

        float light = 0;

        switch (direction)
        {
            case Direction.up:
                nSolid = chunk.GetBlock(pos.Add(0, 1, 1)).controller.IsSolid(Direction.south);
                eSolid = chunk.GetBlock(pos.Add(1, 1, 0)).controller.IsSolid(Direction.west);
                sSolid = chunk.GetBlock(pos.Add(0, 1, -1)).controller.IsSolid(Direction.north);
                wSolid = chunk.GetBlock(pos.Add(-1, 1, 0)).controller.IsSolid(Direction.east);

                wnSolid = chunk.GetBlock(pos.Add(-1, 1, 1)).controller.IsSolid(Direction.east) && chunk.GetBlock(pos.Add(-1, 1, 1)).controller.IsSolid(Direction.south);
                neSolid = chunk.GetBlock(pos.Add(1, 1, 1)).controller.IsSolid(Direction.south) && chunk.GetBlock(pos.Add(1, 1, 1)).controller.IsSolid(Direction.west);
                esSolid = chunk.GetBlock(pos.Add(1, 1, -1)).controller.IsSolid(Direction.west) && chunk.GetBlock(pos.Add(1, 1, -1)).controller.IsSolid(Direction.north);
                swSolid = chunk.GetBlock(pos.Add(-1, 1, -1)).controller.IsSolid(Direction.north) && chunk.GetBlock(pos.Add(-1, 1, -1)).controller.IsSolid(Direction.east);

                light = chunk.GetBlock(pos.Add(0, 1, 0)).data1 / 255f;

                break;
            case Direction.down:
                nSolid = chunk.GetBlock(pos.Add(0, -1, -1)).controller.IsSolid(Direction.south);
                eSolid = chunk.GetBlock(pos.Add(1, -1, 0)).controller.IsSolid(Direction.west);
                sSolid = chunk.GetBlock(pos.Add(0, -1, 1)).controller.IsSolid(Direction.north);
                wSolid = chunk.GetBlock(pos.Add(-1, -1, 0)).controller.IsSolid(Direction.east);

                wnSolid = chunk.GetBlock(pos.Add(-1, -1, -1)).controller.IsSolid(Direction.east) && chunk.GetBlock(pos.Add(-1, -1, -1)).controller.IsSolid(Direction.south);
                neSolid = chunk.GetBlock(pos.Add(1, -1, -1)).controller.IsSolid(Direction.south) && chunk.GetBlock(pos.Add(1, -1, -1)).controller.IsSolid(Direction.west);
                esSolid = chunk.GetBlock(pos.Add(1, -1, 1)).controller.IsSolid(Direction.west) && chunk.GetBlock(pos.Add(1, -1, 1)).controller.IsSolid(Direction.north);
                swSolid = chunk.GetBlock(pos.Add(-1, -1, 1)).controller.IsSolid(Direction.north) && chunk.GetBlock(pos.Add(-1, -1, 1)).controller.IsSolid(Direction.east);

                light = chunk.GetBlock(pos.Add(0, -1, 0)).data1 / 255f;

                break;
            case Direction.north:
                nSolid = chunk.GetBlock(pos.Add(1, 0, 1)).controller.IsSolid(Direction.west);
                eSolid = chunk.GetBlock(pos.Add(0, 1, 1)).controller.IsSolid(Direction.down);
                sSolid = chunk.GetBlock(pos.Add(-1, 0, 1)).controller.IsSolid(Direction.east);
                wSolid = chunk.GetBlock(pos.Add(0, -1, 1)).controller.IsSolid(Direction.up);

                esSolid = chunk.GetBlock(pos.Add(-1, 1, 1)).controller.IsSolid(Direction.east) && chunk.GetBlock(pos.Add(-1, 1, 1)).controller.IsSolid(Direction.south);
                neSolid = chunk.GetBlock(pos.Add(1, 1, 1)).controller.IsSolid(Direction.south) && chunk.GetBlock(pos.Add(1, 1, 1)).controller.IsSolid(Direction.west);
                wnSolid = chunk.GetBlock(pos.Add(1, -1, 1)).controller.IsSolid(Direction.west) && chunk.GetBlock(pos.Add(1, -1, 1)).controller.IsSolid(Direction.north);
                swSolid = chunk.GetBlock(pos.Add(-1, -1, 1)).controller.IsSolid(Direction.north) && chunk.GetBlock(pos.Add(-1, -1, 1)).controller.IsSolid(Direction.east);

                light = chunk.GetBlock(pos.Add(0, 0, 1)).data1 / 255f;

                break;
            case Direction.east:
                nSolid = chunk.GetBlock(pos.Add(1, 0, -1)).controller.IsSolid(Direction.up);
                eSolid = chunk.GetBlock(pos.Add(1, 1, 0)).controller.IsSolid(Direction.west);
                sSolid = chunk.GetBlock(pos.Add(1, 0, 1)).controller.IsSolid(Direction.down);
                wSolid = chunk.GetBlock(pos.Add(1, -1, 0)).controller.IsSolid(Direction.east);

                esSolid = chunk.GetBlock(pos.Add(1, 1, 1)).controller.IsSolid(Direction.west) && chunk.GetBlock(pos.Add(1, 1, 1)).controller.IsSolid(Direction.north);
                neSolid = chunk.GetBlock(pos.Add(1, 1, -1)).controller.IsSolid(Direction.south) && chunk.GetBlock(pos.Add(1, 1, -1)).controller.IsSolid(Direction.west);
                wnSolid = chunk.GetBlock(pos.Add(1, -1, -1)).controller.IsSolid(Direction.east) && chunk.GetBlock(pos.Add(1, -1, -1)).controller.IsSolid(Direction.north);
                swSolid = chunk.GetBlock(pos.Add(1, -1, 1)).controller.IsSolid(Direction.north) && chunk.GetBlock(pos.Add(1, -1, 1)).controller.IsSolid(Direction.east);

                light = chunk.GetBlock(pos.Add(1, 0, 0)).data1 / 255f;

                break;
            case Direction.south:
                nSolid = chunk.GetBlock(pos.Add(-1, 0, -1)).controller.IsSolid(Direction.down);
                eSolid = chunk.GetBlock(pos.Add(0, 1, -1)).controller.IsSolid(Direction.west);
                sSolid = chunk.GetBlock(pos.Add(1, 0, -1)).controller.IsSolid(Direction.up);
                wSolid = chunk.GetBlock(pos.Add(0, -1, -1)).controller.IsSolid(Direction.south);

                esSolid = chunk.GetBlock(pos.Add(1, 1, -1)).controller.IsSolid(Direction.west) && chunk.GetBlock(pos.Add(1, 1, -1)).controller.IsSolid(Direction.north);
                neSolid = chunk.GetBlock(pos.Add(-1, 1, -1)).controller.IsSolid(Direction.south) && chunk.GetBlock(pos.Add(-1, 1, -1)).controller.IsSolid(Direction.west);
                wnSolid = chunk.GetBlock(pos.Add(-1, -1, -1)).controller.IsSolid(Direction.east) && chunk.GetBlock(pos.Add(-1, -1, -1)).controller.IsSolid(Direction.north);
                swSolid = chunk.GetBlock(pos.Add(1, -1, -1)).controller.IsSolid(Direction.north) && chunk.GetBlock(pos.Add(1, -1, -1)).controller.IsSolid(Direction.east);

                light = chunk.GetBlock(pos.Add(0, 0, -1)).data1 / 255f;

                break;
            case Direction.west:
                nSolid = chunk.GetBlock(pos.Add(-1, 0, 1)).controller.IsSolid(Direction.up);
                eSolid = chunk.GetBlock(pos.Add(-1, 1, 0)).controller.IsSolid(Direction.west);
                sSolid = chunk.GetBlock(pos.Add(-1, 0, -1)).controller.IsSolid(Direction.down);
                wSolid = chunk.GetBlock(pos.Add(-1, -1, 0)).controller.IsSolid(Direction.east);

                esSolid = chunk.GetBlock(pos.Add(-1, 1, -1)).controller.IsSolid(Direction.west) && chunk.GetBlock(pos.Add(-1, 1, -1)).controller.IsSolid(Direction.north);
                neSolid = chunk.GetBlock(pos.Add(-1, 1, 1)).controller.IsSolid(Direction.south) && chunk.GetBlock(pos.Add(-1, 1, 1)).controller.IsSolid(Direction.west);
                wnSolid = chunk.GetBlock(pos.Add(-1, -1, 1)).controller.IsSolid(Direction.east) && chunk.GetBlock(pos.Add(-1, -1, 1)).controller.IsSolid(Direction.north);
                swSolid = chunk.GetBlock(pos.Add(-1, -1, -1)).controller.IsSolid(Direction.north) && chunk.GetBlock(pos.Add(-1, -1, -1)).controller.IsSolid(Direction.east);

                light = chunk.GetBlock(pos.Add(-1, 0, 0)).data1 / 255f;

                break;
            default:
                Debug.LogError("Direction not recognized");
                break;
        }

        AddColors(meshData, wnSolid, nSolid, neSolid, eSolid, esSolid, sSolid, swSolid, wSolid, light);
    }

    public static void BuildTexture(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, TextureCollection textureCollection)
    {
        Rect texture = textureCollection.GetTexture(chunk, pos, direction);
        Vector2[] UVs = new Vector2[4];

        UVs[0] = new Vector2(texture.x + texture.width, texture.y);
        UVs[1] = new Vector2(texture.x + texture.width, texture.y + texture.height);
        UVs[2] = new Vector2(texture.x, texture.y + texture.height);
        UVs[3] = new Vector2(texture.x, texture.y);

        meshData.uv.AddRange(UVs);
    }

    public static void BuildTexture(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, TextureCollection[] textureCollections)
    {
        Rect texture = new Rect();

        switch (direction)
        {
            case Direction.up:
                texture = textureCollections[0].GetTexture(chunk, pos, direction);
                break;
            case Direction.down:
                texture = textureCollections[1].GetTexture(chunk, pos, direction);
                break;
            case Direction.north:
                texture = textureCollections[2].GetTexture(chunk, pos, direction);
                break;
            case Direction.east:
                texture = textureCollections[3].GetTexture(chunk, pos, direction);
                break;
            case Direction.south:
                texture = textureCollections[4].GetTexture(chunk, pos, direction);
                break;
            case Direction.west:
                texture = textureCollections[5].GetTexture(chunk, pos, direction);
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

    static void AddQuadToMeshData(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, bool useCollisionMesh)
    {
        //Adding a tiny overlap between block meshes may solve floating point imprecision
        //errors causing pixel size gaps between blocks when looking closely
        float halfBlock = (Config.Env.BlockSize / 2) + Config.Env.BlockFacePadding;

        //Converting the position to a vector adjusts it based on block size and gives us real world coordinates for x, y and z
        Vector3 vPos = pos;

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

    static void AddColors(MeshData meshData, bool wnSolid, bool nSolid, bool neSolid, bool eSolid, bool esSolid, bool sSolid, bool swSolid, bool wSolid, float light)
    {
        float ne = 1;
        float es = 1;
        float sw = 1;
        float wn = 1;

        float aoContrast = 0.2f;

        if (nSolid)
        {
            wn -= aoContrast;
            ne -= aoContrast;
        }

        if (eSolid)
        {
            ne -= aoContrast;
            es -= aoContrast;
        }

        if (sSolid)
        {
            es -= aoContrast;
            sw -= aoContrast;
        }

        if (wSolid)
        {
            sw -= aoContrast;
            wn -= aoContrast;
        }

        if (neSolid)
            ne -= aoContrast;

        if (swSolid)
            sw -= aoContrast;

        if (wnSolid)
            wn -= aoContrast;

        if (esSolid)
            es -= aoContrast;

        meshData.AddColors(ne, es, sw, wn, light);
    }
}
