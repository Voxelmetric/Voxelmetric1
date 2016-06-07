namespace Voxelmetric.Code.Core.Interface
{
    internal interface IChunkRender
    {
        void Reset();
        
        void BuildMeshData();

        void BuildMesh();
    }
}
