using Voxelmetric.Code.Rendering;

namespace Voxelmetric.Code.Builders
{
    public interface IMeshBuilder
    {
        void BuildMesh(UnityEngine.Mesh mesh, RenderBuffer buffer);
    }
}
