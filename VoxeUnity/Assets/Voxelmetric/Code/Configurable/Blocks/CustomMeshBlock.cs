using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Geometry.Batchers;
using Voxelmetric.Code.Load_Resources.Blocks;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

public class CustomMeshBlock : Block
{
    public CustomMeshBlockConfig customMeshConfig { get { return (CustomMeshBlockConfig)Config; } }

    public override void OnInit(BlockProvider blockProvider)
    {
        base.OnInit(blockProvider);

        Custom = true;
    }

    public override void BuildBlock(Chunk chunk, ref Vector3Int localPos, int materialID)
    {
        Rect texture = customMeshConfig.texture!=null
                           ? customMeshConfig.texture.GetTexture(chunk, ref localPos, Direction.down)
                           : new Rect();

        RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;

        if (customMeshConfig.uvs==null)
            batcher.AddMeshData(materialID, customMeshConfig.tris, customMeshConfig.verts, customMeshConfig.colors, localPos);
        else if (customMeshConfig.colors==null)
            batcher.AddMeshData(materialID, customMeshConfig.tris, customMeshConfig.verts, customMeshConfig.uvs, ref texture, localPos);
        else
            batcher.AddMeshData(materialID, customMeshConfig.tris, customMeshConfig.verts, customMeshConfig.colors, customMeshConfig.uvs, ref texture, localPos);
    }
}
