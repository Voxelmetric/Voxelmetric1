using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class CustomMeshBlock : Block {

    public CustomMeshBlockConfig customMeshConfig { get { return (CustomMeshBlockConfig)config; } }

    public override void AddBlockData(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData)
    {
        int initialVertCount = meshData.vertices.Count;
        //int colInitialVertCount = meshData.colVertices.Count;

        for (int i = 0; i < customMeshConfig.verts.Length; i++)
        {
            meshData.AddVertex(customMeshConfig.verts[i] + (Vector3)localPos);
            //meshData.colVertices.Add(customMeshConfig.verts[i] + (Vector3)localPos);

            if (customMeshConfig.uvs.Length == 0)
                meshData.uv.Add(new Vector2(0, 0));

            //Coloring of blocks is not yet implemented so just pass in full brightness
            meshData.colors.Add(new Color(1, 1, 1, 1));
        }

        if (customMeshConfig.uvs.Length != 0)
        {
            Rect texture;
            if (customMeshConfig.texture != null)
                texture = customMeshConfig.texture.GetTexture(chunk, localPos, globalPos, Direction.down);
            else
                texture = new Rect();


            for (int i = 0; i < customMeshConfig.uvs.Length; i++)
            {
                meshData.uv.Add(new Vector2(
                    (customMeshConfig.uvs[i].x * texture.width) + texture.x,
                    (customMeshConfig.uvs[i].y * texture.height) + texture.y)
                );
            }
        }

        for (int i = 0; i < customMeshConfig.tris.Length; i++)
        {
            meshData.AddTriangle(customMeshConfig.tris[i] + initialVertCount);
            //meshData.colTriangles.Add(customMeshConfig.tris[i] + colInitialVertCount);
        }
    }

}
