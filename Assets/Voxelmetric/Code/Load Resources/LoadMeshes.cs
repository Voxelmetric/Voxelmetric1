using UnityEngine;
using System.Collections;

public static class LoadMeshes {

    public static void GetAndLoadMeshBlocks()
    {
        Mesh[] meshes = Resources.LoadAll<Mesh>(Config.Directories.BlockMeshFolder);

        foreach (var mesh in meshes)
        {
            CustomMesh controller = new CustomMesh();
            controller.blockName = mesh.name;

            controller.verts = mesh.vertices;
            controller.tris = mesh.triangles;
            controller.uvs = mesh.uv;

            controller.collection = Block.index.textureIndex.GetTextureCollection(mesh.name);
            Block.index.AddBlockType(controller);
        }

    }

}
