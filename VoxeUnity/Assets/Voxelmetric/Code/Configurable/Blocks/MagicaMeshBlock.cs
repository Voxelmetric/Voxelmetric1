using Voxelmetric.Code.Core;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

public class MagicaMeshBlock : Block
{
    public MagicaMeshBlockConfig magicMeshConfig
    {
        get
        {
            return (MagicaMeshBlockConfig)Config;
        }
    }

    public override void BuildBlock(Chunk chunk, ref Vector3Int localPos, int materialID)
    {
        var batcher = chunk.GeometryHandler.Batcher;
        batcher.AddMeshData(materialID, magicMeshConfig.tris, magicMeshConfig.verts, magicMeshConfig.colors, localPos);
    }
}
