using UnityEngine;
using System.Collections;
using System;

[Serializable]
public static class BlockBuilder
{
    public static void BuildRenderer(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, Block block)
    {
        AddQuadToMeshData(chunk, pos, meshData, direction, block, false);
    }

    public static void BuildCollider(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, Block block)
    {
        AddQuadToMeshData(chunk, pos, meshData, direction, block, true);
    }

    public static void BuildColors(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, Block block)
    {
        bool nSolid = false;
        bool eSolid = false;
        bool sSolid = false;
        bool wSolid = false;

        bool wnSolid = false;
        bool neSolid = false;
        bool esSolid = false;
        bool swSolid = false;

        switch (direction)
        {
            case Direction.up:
                nSolid = chunk.GetBlock(pos.Add( 0, 1, 1)).Block().IsSolid(Direction.south);
                eSolid = chunk.GetBlock(pos.Add( 1, 1, 0)).Block().IsSolid(Direction.west);
                sSolid = chunk.GetBlock(pos.Add( 0, 1,-1)).Block().IsSolid(Direction.north);
                wSolid = chunk.GetBlock(pos.Add(-1, 1, 0)).Block().IsSolid(Direction.east);

                wnSolid = chunk.GetBlock(pos.Add(-1, 1, 1)).Block().IsSolid(Direction.east) && chunk.GetBlock(pos.Add(-1, 1, 1)).Block().IsSolid(Direction.south);
                neSolid = chunk.GetBlock(pos.Add(1, 1, 1)).Block().IsSolid(Direction.south) && chunk.GetBlock(pos.Add( 1, 1, 1)).Block().IsSolid(Direction.west);
                esSolid = chunk.GetBlock(pos.Add(1, 1, -1)).Block().IsSolid(Direction.west) && chunk.GetBlock(pos.Add(1,1,-1)).Block().IsSolid(Direction.north);
                swSolid = chunk.GetBlock(pos.Add(-1, 1, -1)).Block().IsSolid(Direction.north) && chunk.GetBlock(pos.Add(-1,1,-1)).Block().IsSolid(Direction.east);
                break;
            case Direction.down:
                nSolid = chunk.GetBlock(pos.Add(0,-1,-1)).Block().IsSolid(Direction.south);
                eSolid = chunk.GetBlock(pos.Add(1,-1,0)).Block().IsSolid(Direction.west);
                sSolid = chunk.GetBlock(pos.Add(0,-1,1)).Block().IsSolid(Direction.north);
                wSolid = chunk.GetBlock(pos.Add(-1,-1,0)).Block().IsSolid(Direction.east);

                wnSolid = chunk.GetBlock(pos.Add(-1, -1, -1)).Block().IsSolid(Direction.east) && chunk.GetBlock(pos.Add(-1,-1,-1)).Block().IsSolid(Direction.south);
                neSolid = chunk.GetBlock(pos.Add(1, -1, -1)).Block().IsSolid(Direction.south) && chunk.GetBlock(pos.Add(1, -1, -1)).Block().IsSolid(Direction.west);
                esSolid = chunk.GetBlock(pos.Add(1, -1, 1)).Block().IsSolid(Direction.west) && chunk.GetBlock(pos.Add(1, -1, 1)).Block().IsSolid(Direction.north);
                swSolid = chunk.GetBlock(pos.Add(-1, -1, 1)).Block().IsSolid(Direction.north) && chunk.GetBlock(pos.Add(-1, -1,1)).Block().IsSolid(Direction.east);
                break;
            case Direction.north:
                nSolid = chunk.GetBlock(pos.Add(1,0,1)).Block().IsSolid(Direction.west);
                eSolid = chunk.GetBlock(pos.Add(0,1,1)).Block().IsSolid(Direction.down);
                sSolid = chunk.GetBlock(pos.Add(-1,0,1)).Block().IsSolid(Direction.east);
                wSolid = chunk.GetBlock(pos.Add(0,-1,1)).Block().IsSolid(Direction.up);

                esSolid = chunk.GetBlock(pos.Add(-1, 1, 1)).Block().IsSolid(Direction.east) && chunk.GetBlock(pos.Add(-1,1,1)).Block().IsSolid(Direction.south);
                neSolid = chunk.GetBlock(pos.Add(1, 1, 1)).Block().IsSolid(Direction.south) && chunk.GetBlock(pos.Add(1,1,1)).Block().IsSolid(Direction.west);
                wnSolid = chunk.GetBlock(pos.Add(1, -1, 1)).Block().IsSolid(Direction.west) && chunk.GetBlock(pos.Add(1, -1, 1)).Block().IsSolid(Direction.north);
                swSolid = chunk.GetBlock(pos.Add(-1, -1, 1)).Block().IsSolid(Direction.north) && chunk.GetBlock(pos.Add(-1, -1, 1)).Block().IsSolid(Direction.east);
                break;
            case Direction.east:
                nSolid = chunk.GetBlock(pos.Add(1,0,-1)).Block().IsSolid(Direction.up);
                eSolid = chunk.GetBlock(pos.Add(1,1,0)).Block().IsSolid(Direction.west);
                sSolid = chunk.GetBlock(pos.Add(1,0,1)).Block().IsSolid(Direction.down);
                wSolid = chunk.GetBlock(pos.Add(1,-1,0)).Block().IsSolid(Direction.east);

                esSolid = chunk.GetBlock(pos.Add(1, 1, 1)).Block().IsSolid(Direction.west) && chunk.GetBlock(pos.Add(1, 1, 1)).Block().IsSolid(Direction.north);
                neSolid = chunk.GetBlock(pos.Add(1, 1, -1)).Block().IsSolid(Direction.south) && chunk.GetBlock(pos.Add(1, 1, -1)).Block().IsSolid(Direction.west);
                wnSolid = chunk.GetBlock(pos.Add(1, -1, -1)).Block().IsSolid(Direction.east) && chunk.GetBlock(pos.Add(1, -1, -1)).Block().IsSolid(Direction.north);
                swSolid = chunk.GetBlock(pos.Add(1, -1, 1)).Block().IsSolid(Direction.north) && chunk.GetBlock(pos.Add(1, -1, 1)).Block().IsSolid(Direction.east);
                break;
            case Direction.south:
                nSolid = chunk.GetBlock(pos.Add(-1,0,-1)).Block().IsSolid(Direction.down);
                eSolid = chunk.GetBlock(pos.Add(0,1,-1)).Block().IsSolid(Direction.west);
                sSolid = chunk.GetBlock(pos.Add(1,0,-1)).Block().IsSolid(Direction.up);
                wSolid = chunk.GetBlock(pos.Add(0,-1,-1)).Block().IsSolid(Direction.south);

                esSolid = chunk.GetBlock(pos.Add(1, 1, -1)).Block().IsSolid(Direction.west) && chunk.GetBlock(pos.Add(1, 1, -1)).Block().IsSolid(Direction.north);
                neSolid = chunk.GetBlock(pos.Add(-1, 1, -1)).Block().IsSolid(Direction.south) && chunk.GetBlock(pos.Add(-1, 1, -1)).Block().IsSolid(Direction.west);
                wnSolid = chunk.GetBlock(pos.Add(-1, -1, -1)).Block().IsSolid(Direction.east) && chunk.GetBlock(pos.Add(-1, -1, -1)).Block().IsSolid(Direction.north);
                swSolid = chunk.GetBlock(pos.Add(1, -1, -1)).Block().IsSolid(Direction.north) && chunk.GetBlock(pos.Add(1, -1, -1)).Block().IsSolid(Direction.east);
                break;
            case Direction.west:
                nSolid = chunk.GetBlock(pos.Add(-1,0,1)).Block().IsSolid(Direction.up);
                eSolid = chunk.GetBlock(pos.Add(-1,1,0)).Block().IsSolid(Direction.west);
                sSolid = chunk.GetBlock(pos.Add(-1,0,-1)).Block().IsSolid(Direction.down);
                wSolid = chunk.GetBlock(pos.Add(-1,-1,0)).Block().IsSolid(Direction.east);

                esSolid = chunk.GetBlock(pos.Add(-1, 1, -1)).Block().IsSolid(Direction.west) && chunk.GetBlock(pos.Add(-1, 1, -1)).Block().IsSolid(Direction.north);
                neSolid = chunk.GetBlock(pos.Add(-1, 1, 1)).Block().IsSolid(Direction.south) && chunk.GetBlock(pos.Add(-1, 1, 1)).Block().IsSolid(Direction.west);
                wnSolid = chunk.GetBlock(pos.Add(-1, -1, 1)).Block().IsSolid(Direction.east) && chunk.GetBlock(pos.Add(-1, -1, 1)).Block().IsSolid(Direction.north);
                swSolid = chunk.GetBlock(pos.Add(-1, -1, -1)).Block().IsSolid(Direction.north) && chunk.GetBlock(pos.Add(-1, -1, -1)).Block().IsSolid(Direction.east);
                break;
            default:
                Debug.LogError("Direction not recognized");
                break;
        }

        AddColors(meshData, wnSolid, nSolid, neSolid, eSolid, esSolid, sSolid, swSolid, wSolid);
    }

    public static void BuildTexture(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, Block block, Tile tilePos)
    {
        Vector2[] UVs = new Vector2[4];

        UVs[0] = new Vector2(Config.TileSize * tilePos.x + Config.TileSize, Config.TileSize * tilePos.y);
        UVs[1] = new Vector2(Config.TileSize * tilePos.x + Config.TileSize, Config.TileSize * tilePos.y + Config.TileSize);
        UVs[2] = new Vector2(Config.TileSize * tilePos.x, Config.TileSize * tilePos.y + Config.TileSize);
        UVs[3] = new Vector2(Config.TileSize * tilePos.x, Config.TileSize * tilePos.y);

        meshData.uv.AddRange(UVs);
    }

    public static void BuildTexture(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, Block block, Tile[] tiles)
    {
        Tile tilePos = new Tile();

        switch (direction)
        {
            case Direction.up:
                tilePos = tiles[0];
                break;
            case Direction.down:
                tilePos = tiles[1];
                break;
            case Direction.north:
                tilePos = tiles[2];
                break;
            case Direction.east:
                tilePos = tiles[3];
                break;
            case Direction.south:
                tilePos = tiles[4];
                break;
            case Direction.west:
                tilePos = tiles[5];
                break;
            default:
                break;
        }

        Vector2[] UVs = new Vector2[4];

        UVs[0] = new Vector2(Config.TileSize * tilePos.x + Config.TileSize, Config.TileSize * tilePos.y);
        UVs[1] = new Vector2(Config.TileSize * tilePos.x + Config.TileSize, Config.TileSize * tilePos.y + Config.TileSize);
        UVs[2] = new Vector2(Config.TileSize * tilePos.x, Config.TileSize * tilePos.y + Config.TileSize);
        UVs[3] = new Vector2(Config.TileSize * tilePos.x, Config.TileSize * tilePos.y);

        meshData.uv.AddRange(UVs);
    }

    static void AddQuadToMeshData(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, Block block, bool useCollisionMesh)
    {
        switch (direction)
        {
            case Direction.up:
                meshData.AddVertex(new Vector3(pos.x - 0.5f, pos.y + 0.5f, pos.z + 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x + 0.5f, pos.y + 0.5f, pos.z + 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x + 0.5f, pos.y + 0.5f, pos.z - 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x - 0.5f, pos.y + 0.5f, pos.z - 0.5f), useCollisionMesh);
                break;
            case Direction.down:
                meshData.AddVertex(new Vector3(pos.x - 0.5f, pos.y - 0.5f, pos.z - 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x + 0.5f, pos.y - 0.5f, pos.z - 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x + 0.5f, pos.y - 0.5f, pos.z + 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x - 0.5f, pos.y - 0.5f, pos.z + 0.5f), useCollisionMesh);
                break;
            case Direction.north:
                meshData.AddVertex(new Vector3(pos.x + 0.5f, pos.y - 0.5f, pos.z + 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x + 0.5f, pos.y + 0.5f, pos.z + 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x - 0.5f, pos.y + 0.5f, pos.z + 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x - 0.5f, pos.y - 0.5f, pos.z + 0.5f), useCollisionMesh);
                break;
            case Direction.east:
                meshData.AddVertex(new Vector3(pos.x + 0.5f, pos.y - 0.5f, pos.z - 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x + 0.5f, pos.y + 0.5f, pos.z - 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x + 0.5f, pos.y + 0.5f, pos.z + 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x + 0.5f, pos.y - 0.5f, pos.z + 0.5f), useCollisionMesh);
                break;
            case Direction.south:
                meshData.AddVertex(new Vector3(pos.x - 0.5f, pos.y - 0.5f, pos.z - 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x - 0.5f, pos.y + 0.5f, pos.z - 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x + 0.5f, pos.y + 0.5f, pos.z - 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x + 0.5f, pos.y - 0.5f, pos.z - 0.5f), useCollisionMesh);
                break;
            case Direction.west:
                meshData.AddVertex(new Vector3(pos.x - 0.5f, pos.y - 0.5f, pos.z + 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x - 0.5f, pos.y + 0.5f, pos.z + 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x - 0.5f, pos.y + 0.5f, pos.z - 0.5f), useCollisionMesh);
                meshData.AddVertex(new Vector3(pos.x - 0.5f, pos.y - 0.5f, pos.z - 0.5f), useCollisionMesh);
                break;
            default:
                Debug.LogError("Direction not recognized");
                break;
        }

        meshData.AddQuadTriangles(useCollisionMesh);
    }

    static void AddColors(MeshData meshData, bool wnSolid, bool nSolid, bool neSolid, bool eSolid, bool esSolid, bool sSolid, bool swSolid, bool wSolid)
    {
        float ne = 1;
        float es = 1;
        float sw = 1;
        float wn = 1;

        if (nSolid)
        {
            wn -= 0.2f;
            ne -= 0.2f;
        }

        if (eSolid)
        {
            ne -= 0.2f;
            es -= 0.2f;
        }

        if (sSolid)
        {
            es -= 0.2f;
            sw -= 0.2f;
        }

        if (wSolid)
        {
            sw -= 0.2f;
            wn -= 0.2f;
        }

        if (neSolid)
            ne -= 0.2f;

        if (swSolid)
            sw -= 0.2f;

        if (wnSolid)
            wn -= 0.2f;

        if (esSolid)
            es -= 0.2f;

        meshData.AddColors(ne, es, sw, wn);
    }
}
