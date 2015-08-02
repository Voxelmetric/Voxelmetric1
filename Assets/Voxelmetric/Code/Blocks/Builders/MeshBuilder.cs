using System.Collections;
using UnityEngine;

public class MeshBuilder {

    public static void CrossMeshRenderer(Chunk chunk, BlockPos pos, MeshData meshData, TextureCollection texture, Block block)
    {
        float halfBlock = (Config.Env.BlockSize / 2) + Config.Env.BlockFacePadding;
        float colliderOffest = 0.05f * Config.Env.BlockSize;
        float blockHeight = halfBlock * 2 * (block.data2 / 255f);

        float offsetX = (halfBlock * 2 * ((byte)(block.data3 & 0x0F) / 32f)) - (halfBlock/2);
        float offsetZ = (halfBlock * 2 * ((byte)((block.data3 & 0xF0) >> 4) / 32f)) - (halfBlock/2);

        //Converting the position to a vector adjusts it based on block size and gives us real world coordinates for x, y and z
        Vector3 vPos = pos;
        Vector3 vPosCollider = pos;
        vPos += new Vector3(offsetX, 0, offsetZ);

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

        meshData.AddVertex(new Vector3(vPosCollider.x - halfBlock, vPosCollider.y - halfBlock + colliderOffest, vPosCollider.z + halfBlock), collisionMesh: true);
        meshData.AddVertex(new Vector3(vPosCollider.x + halfBlock, vPosCollider.y - halfBlock + colliderOffest, vPosCollider.z + halfBlock), collisionMesh: true);
        meshData.AddVertex(new Vector3(vPosCollider.x + halfBlock, vPosCollider.y - halfBlock + colliderOffest, vPosCollider.z - halfBlock), collisionMesh: true);
        meshData.AddVertex(new Vector3(vPosCollider.x - halfBlock, vPosCollider.y - halfBlock + colliderOffest, vPosCollider.z - halfBlock), collisionMesh: true);
        meshData.AddQuadTriangles(collisionMesh:true);
    }
}
