using System.Collections.Generic;

namespace Voxelmetric.Code.Rendering
{
    /// <summary>
    ///     A simple intermediate container for mesh data
    /// </summary>
    public class RenderBuffer
    {
        public readonly List<VertexData> Vertices = new List<VertexData>();
        public readonly List<int> Triangles = new List<int>();
        
        /// <summary>
        ///     Clear the render buffer
        /// </summary>
        public void Clear()
        {
            Vertices.Clear();
            Triangles.Clear();
        }

        public bool IsEmpty()
        {
            return (Vertices.Count <= 0);
        }
    }
}