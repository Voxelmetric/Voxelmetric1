using Voxelmetric.Code.Rendering;

namespace Voxelmetric.Code.Core
{
    public class ChunkRender
    {
        private Chunk chunk;
        private DrawCallBatcher m_drawCallBatcher;

        public DrawCallBatcher batcher
        {
            get { return m_drawCallBatcher; }
        }

        public ChunkRender(Chunk chunk)
        {
            this.chunk = chunk;
            m_drawCallBatcher = new DrawCallBatcher(this.chunk);
        }

        public void Reset()
        {
            m_drawCallBatcher.Clear();
        }

        /// <summary> Updates the chunk based on its contents </summary>
        public void BuildMeshData()
        {
            Globals.CubeMeshBuilder.Build(chunk);
        }

        public void BuildMesh()
        {
            m_drawCallBatcher.Commit();
        }
    }
}