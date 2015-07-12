using UnityEngine;
using System.Collections;

public class CustomMesh : BlockController {

    public Vector3[] verts = new Vector3[0];
    public int[] tris = new int[0];
    public Vector2[] uvs = new Vector2[0];

    public TextureCollection collection;

    public string blockName;

    public override string Name()
    {
        return blockName;
    }

    public override bool IsSolid(Direction direction) { return false; }

    public override void AddBlockData(Chunk chunk, BlockPos pos, MeshData meshData, Block block)
    {
        int initialVertCount = meshData.vertices.Count;

        foreach (var vert in verts)
        {
            meshData.AddVertex(vert + (Vector3)pos);

            if (uvs.Length == 0)
                meshData.uv.Add(new Vector2(0, 0));

            float lighting;
            if (Config.Toggle.BlockLighting)
            {
                lighting = block.data1 / 255f;
            }
            else
            {
                lighting = 1;
            }
            meshData.colors.Add(new Color(lighting, lighting, lighting, 1));
        }

        if (uvs.Length != 0)
        {
            Rect texture;
            if (collection != null)
                texture = collection.GetTexture(chunk, pos, Direction.down);
            else
                texture = new Rect();


            foreach (var uv in uvs)
            {
                meshData.uv.Add(new Vector2((uv.x * texture.width) + texture.x, (uv.y * texture.height) + texture.y));
            }
        }

        foreach (var tri in tris)
        {
            meshData.AddTriangle(tri + initialVertCount);
        }
    }

    public override bool IsTransparent() { return true; }

}
