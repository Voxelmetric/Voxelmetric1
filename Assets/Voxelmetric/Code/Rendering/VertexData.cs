using UnityEngine;

namespace Voxelmetric.Code.Rendering
{
    public struct VertexDataFixed
    {
        public Vector3 Vertex;
        public Vector3 Normal;
        public Vector2 UV;
        public Color32 Color;
        public Vector4 Tangent;
    }

    public class VertexData
    {
        public Vector3 Vertex;
        public Vector3 Normal;
        public Vector2 UV;
        public Color32 Color;
        public Vector4 Tangent;
    }

    public static class VertexDataUtils
    {
        public static VertexDataFixed ClassToStruct(VertexData vertexData)
        {
            return new VertexDataFixed
            {
                Color = vertexData.Color,
                Normal = vertexData.Normal,
                Tangent = vertexData.Tangent,
                Vertex = vertexData.Vertex,
                UV = vertexData.UV
            };
        }
    }
}
