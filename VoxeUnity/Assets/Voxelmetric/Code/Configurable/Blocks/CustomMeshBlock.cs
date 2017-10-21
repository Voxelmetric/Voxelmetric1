using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Geometry.GeometryBatcher;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

public class CustomMeshBlock : Block
{
    public CustomMeshBlockConfig customMeshConfig { get { return (CustomMeshBlockConfig)Config; } }

    public override void OnInit(BlockProvider blockProvider)
    {
        Custom = true;
    }

    public override void BuildBlock(Chunk chunk, ref Vector3Int localPos, int materialID)
    {
        Rect texture = customMeshConfig.texture!=null
                           ? customMeshConfig.texture.GetTexture(chunk, ref localPos, Direction.down)
                           : new Rect();

        RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;
        batcher.UseColors(materialID);
        if (customMeshConfig.texture!=null)
            batcher.UseTextures(materialID);
            
        batcher.AddMeshData(customMeshConfig.tris, customMeshConfig.verts, ref texture, localPos, materialID);
    }
}
