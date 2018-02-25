using Voxelmetric.Code.Core;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

public class MagicaMeshBlock: Block
{
    public CustomMeshBlockConfig meshConfig
    {
        get { return (CustomMeshBlockConfig)Config; }
    }

    public override void BuildBlock(Chunk chunk, ref Vector3Int localPos, int materialID)
    {
        var data = meshConfig.data;
        var batcher = chunk.GeometryHandler.Batcher;
        batcher.AddMeshData(materialID, data.tris, data.verts, data.colors, localPos);
    }
}
