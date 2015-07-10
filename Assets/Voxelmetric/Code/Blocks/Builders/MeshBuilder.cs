using System.Collections;
using UnityEngine;

public class MeshBuilder {

    public static void CrossMeshRenderer(Chunk chunk, BlockPos pos, MeshData meshData, TextureCollection texture, Block block)
    {
        float halfBlock = (Config.Env.BlockSize / 2) + Config.Env.BlockFacePadding;
        float colliderOffest = 0.05f * Config.Env.BlockSize;
        float blockHeight = halfBlock * 2 * (block.data2 / 255f);

        //Converting the position to a vector adjusts it based on block size and gives us real world coordinates for x, y and z
        Vector3 vPos = pos;

        float blockLight = ( (block.data1/255f) * Config.Env.BlockLightStrength) + (0.8f*Config.Env.AOStrength);

        meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z + halfBlock));
        meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock + blockHeight, vPos.z + halfBlock));
        meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock + blockHeight, vPos.z - halfBlock));
        meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z - halfBlock));
        meshData.AddQuadTriangles();
        BlockBuilder.BuildTexture(chunk, vPos, meshData, Direction.north, texture);
        meshData.AddColors(blockLight, blockLight, blockLight, blockLight, blockLight);

        meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z - halfBlock));
        meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock + blockHeight, vPos.z - halfBlock));
        meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock + blockHeight, vPos.z + halfBlock));
        meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z + halfBlock));
        meshData.AddQuadTriangles();
        BlockBuilder.BuildTexture(chunk, vPos, meshData, Direction.north, texture);
        meshData.AddColors(blockLight, blockLight, blockLight, blockLight, blockLight);

        meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z + halfBlock));
        meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock + blockHeight, vPos.z + halfBlock));
        meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock + blockHeight, vPos.z - halfBlock));
        meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z - halfBlock));
        meshData.AddQuadTriangles();
        BlockBuilder.BuildTexture(chunk, vPos, meshData, Direction.north, texture);
        meshData.AddColors(blockLight, blockLight, blockLight, blockLight, blockLight);

        meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z - halfBlock));
        meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock + blockHeight, vPos.z - halfBlock));
        meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock + blockHeight, vPos.z + halfBlock));
        meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z + halfBlock));
        meshData.AddQuadTriangles();
        BlockBuilder.BuildTexture(chunk, vPos, meshData, Direction.north, texture);
        meshData.AddColors(blockLight, blockLight, blockLight, blockLight, blockLight);

        meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock + colliderOffest, vPos.z + halfBlock), collisionMesh: true);
        meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock + colliderOffest, vPos.z + halfBlock), collisionMesh: true);
        meshData.AddVertex(new Vector3(vPos.x + halfBlock, vPos.y - halfBlock + colliderOffest, vPos.z - halfBlock), collisionMesh: true);
        meshData.AddVertex(new Vector3(vPos.x - halfBlock, vPos.y - halfBlock + colliderOffest, vPos.z - halfBlock), collisionMesh: true);
        meshData.AddQuadTriangles(collisionMesh:true);
    }
}
