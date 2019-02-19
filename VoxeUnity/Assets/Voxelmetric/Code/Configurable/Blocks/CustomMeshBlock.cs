using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Geometry.Batchers;
using Voxelmetric.Code.Load_Resources.Blocks;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

public class CustomMeshBlock: Block
{
    public CustomMeshBlockConfig meshConfig
    {
        get { return (CustomMeshBlockConfig)Config; }
    }

    public override void OnInit(BlockProvider blockProvider)
    {
        base.OnInit(blockProvider);

        Custom = true;
    }

    public override void BuildBlock(Chunk chunk, ref Vector3Int localPos, int materialID)
    {
        var data = meshConfig.data;
        Rect texture = data.textures!=null
                           ? data.textures.GetTexture(chunk, ref localPos, Direction.down)
                           : new Rect();

        RenderGeometryBatcher batcher = chunk.RenderGeometryHandler.Batcher;

        if (data.uvs==null)
            batcher.AddMeshData(materialID, data.tris, data.verts, data.colors, localPos);
        else if (data.colors==null)
            batcher.AddMeshData(materialID, data.tris, data.verts, data.uvs, ref texture, localPos);
        else
            batcher.AddMeshData(materialID, data.tris, data.verts, data.colors, data.uvs, ref texture, localPos);
    }
}
