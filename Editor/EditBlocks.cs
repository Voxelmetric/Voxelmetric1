using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class EditBlocks {

    bool resourcesFetched = false;
    BlockController[] blocks = new BlockController[0];
    BlockDefinition[] definitions = new BlockDefinition[0];

    int selectedBlock;
    Vector2 blocksListScroll = new Vector2();
    Vector2 blockEditScroll = new Vector2();

    List<string> textureNames = new List<string>();

    public void BlocksTab()
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal(new GUILayoutOption[] { GUILayout.ExpandHeight(true), GUILayout.MinWidth(200), GUILayout.ExpandWidth(true), GUILayout.MaxWidth(300) });
        BlocksList();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal(new GUILayoutOption[] { GUILayout.ExpandHeight(true), GUILayout.MinWidth(500), GUILayout.ExpandWidth(true), GUILayout.MaxWidth(700) });
        BlockEdit();
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    void BlocksList()
    {
        if (!resourcesFetched)
        {
            GetDefinedBlocks();
            GetTextureNames();
            resourcesFetched = true;
        }

        GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.ExpandHeight(true) });

        GUILayout.BeginHorizontal();
        GUILayout.Label("Blocks", new GUIStyle { fontSize = 28 });

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("New", new GUIStyle("button") { fontSize = 20 }))
        {
            selectedBlock = -1;
        }

        if (GUILayout.Button("Refresh", new GUIStyle("button") { fontSize = 20 }))
        {
            GetDefinedBlocks();
            GetTextureNames();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        blocksListScroll = GUILayout.BeginScrollView(blocksListScroll, new GUIStyle(GUI.skin.box));
        for (int i = 0; i < blocks.Length; i++)
        {
            FontStyle fontStyle = FontStyle.Normal;
            if (i == selectedBlock)
            {
                fontStyle = FontStyle.Bold;
            }

            if (GUILayout.Button(blocks[i].Name(),
                new GUIStyle(GUI.skin.box) { fontSize = 20, fontStyle = fontStyle, padding = new RectOffset(10, 10, 10, 10) },
                new GUILayoutOption[] { GUILayout.ExpandWidth(true) }
            ))
            {
                selectedBlock = i;
            }

        }
        GUILayout.EndScrollView();

        GUILayout.Space(10);

        GUILayout.EndVertical();
    }

    void BlockEdit()
    {
        blockEditScroll = GUILayout.BeginScrollView(blockEditScroll, new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(15, 15, 15, 15),
            margin = new RectOffset(15, 0, 0, 19)
        }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true) });
        if (selectedBlock < blocks.Length && selectedBlock >= 0 && blocks.Length != 0 && textureNames.Count > 0)
        {
            GUILayout.Label("Edit: " + definitions[selectedBlock].GetType(), new GUIStyle { fontSize = 24, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
            GUILayout.Space(30);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
            definitions[selectedBlock].blockName = EditorGUILayout.TextField(definitions[selectedBlock].blockName,
                new GUIStyle(GUI.skin.textField) { fontSize = 14, }, new GUILayoutOption[] { GUILayout.Height(18), GUILayout.ExpandWidth(true) });
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            if (definitions[selectedBlock].GetType() == typeof(CrossMeshDefinition))
            {
                GUILayout.BeginHorizontal();
                CrossMeshDefinition crossMesh = (CrossMeshDefinition)definitions[selectedBlock];
                GUILayout.Label("Texture", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });

                int i = EditorGUILayout.Popup(textureNames.IndexOf(crossMesh.texture), textureNames.ToArray(),
                    new GUIStyle(GUI.skin.textField) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                if (i >= 0)
                    crossMesh.texture = textureNames[i];

                GUILayout.EndHorizontal();
            }
            else if (definitions[selectedBlock].GetType() == typeof(CubeDefinition))
            {

                CubeDefinition cube = (CubeDefinition)definitions[selectedBlock];

                GUILayout.Space(10);
                int i = 0;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Texture Top", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                i = EditorGUILayout.Popup(textureNames.IndexOf(cube.textures[0]), textureNames.ToArray(),
                    new GUIStyle(GUI.skin.textField) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                if (i >= 0)
                    cube.textures[0] = textureNames[i];
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Texture Bottom", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                i = EditorGUILayout.Popup(textureNames.IndexOf(cube.textures[1]), textureNames.ToArray(),
                    new GUIStyle(GUI.skin.textField) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                if (i >= 0)
                    cube.textures[1] = textureNames[i];
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Texture North", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                i = EditorGUILayout.Popup(textureNames.IndexOf(cube.textures[2]), textureNames.ToArray(),
                    new GUIStyle(GUI.skin.textField) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                if (i >= 0)
                    cube.textures[2] = textureNames[i];
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Texture East", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                i = EditorGUILayout.Popup(textureNames.IndexOf(cube.textures[3]), textureNames.ToArray(),
                    new GUIStyle(GUI.skin.textField) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                if (i >= 0)
                    cube.textures[3] = textureNames[i];
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Texture South", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                i = EditorGUILayout.Popup(textureNames.IndexOf(cube.textures[4]), textureNames.ToArray(),
                    new GUIStyle(GUI.skin.textField) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                if (i >= 0)
                    cube.textures[4] = textureNames[i];
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Texture West", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                i = EditorGUILayout.Popup(textureNames.IndexOf(cube.textures[5]), textureNames.ToArray(),
                    new GUIStyle(GUI.skin.textField) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                if (i >= 0)
                    cube.textures[5] = textureNames[i];
                GUILayout.EndHorizontal();

                GUILayout.Space(30);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Block is solid", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                cube.blockIsSolid = EditorGUILayout.Toggle(cube.blockIsSolid,
                    new GUIStyle(GUI.skin.toggle) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                GUILayout.EndHorizontal();

                if (!cube.blockIsSolid)
                {
                    GUILayout.Space(10);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Solid towards same type", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                    cube.solidTowardsSameType = EditorGUILayout.Toggle(cube.solidTowardsSameType,
                        new GUIStyle(GUI.skin.toggle) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                    GUILayout.EndHorizontal();
                }

            }
            else if (definitions[selectedBlock].GetType() == typeof(MeshDefinition))
            {
                MeshDefinition mesh = (MeshDefinition)definitions[selectedBlock];

                GUILayout.BeginHorizontal();
                GUILayout.Label("Mesh name", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                mesh.meshName = EditorGUILayout.TextField(mesh.meshName,
                    new GUIStyle(GUI.skin.textField) { fontSize = 14, }, new GUILayoutOption[] { GUILayout.Height(18), GUILayout.ExpandWidth(true) });
                GUILayout.EndHorizontal();
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Texture", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                int i = EditorGUILayout.Popup(textureNames.IndexOf(mesh.texture), textureNames.ToArray(),
                    new GUIStyle(GUI.skin.textField) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                if (i >= 0)
                    mesh.texture = textureNames[i];
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Block is solid upwards", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                mesh.blockIsSolid[0] = EditorGUILayout.Toggle(mesh.blockIsSolid[0],
                    new GUIStyle(GUI.skin.toggle) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                GUILayout.EndHorizontal();
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Block is solid downwards", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                mesh.blockIsSolid[1] = EditorGUILayout.Toggle(mesh.blockIsSolid[1],
                    new GUIStyle(GUI.skin.toggle) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                GUILayout.EndHorizontal();
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Block is solid north", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                mesh.blockIsSolid[2] = EditorGUILayout.Toggle(mesh.blockIsSolid[2],
                    new GUIStyle(GUI.skin.toggle) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                GUILayout.EndHorizontal();
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Block is solid east", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                mesh.blockIsSolid[3] = EditorGUILayout.Toggle(mesh.blockIsSolid[3],
                    new GUIStyle(GUI.skin.toggle) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                GUILayout.EndHorizontal();
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Block is solid south", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                mesh.blockIsSolid[4] = EditorGUILayout.Toggle(mesh.blockIsSolid[4],
                    new GUIStyle(GUI.skin.toggle) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Block is solid west", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                mesh.blockIsSolid[5] = EditorGUILayout.Toggle(mesh.blockIsSolid[5],
                    new GUIStyle(GUI.skin.toggle) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Mesh position offset", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                mesh.positionOffset = EditorGUILayout.Vector3Field("", mesh.positionOffset,
                    new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                GUILayout.EndHorizontal();
            }


            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Delete",
                new GUIStyle(GUI.skin.button) { fontSize = 20, padding = new RectOffset(10, 10, 10, 10) },
                new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) }
            ))
            {
                Object.DestroyImmediate(definitions[selectedBlock]);
                GetDefinedBlocks();
                selectedBlock = -1;
            }


            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (GUI.changed)
                EditorUtility.SetDirty(definitions[selectedBlock]);
        }
        else if (selectedBlock == -1)
        {
            NewBlock();
        }

        GUILayout.EndScrollView();
    }

    void NewBlock()
    {
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) });
        GUILayout.Box("A cube block textured on each side. If set to solid these blocks are the most efficient for large volumes",
                new GUIStyle(GUI.skin.box) { fontSize = 14, padding = new RectOffset(10, 10, 10, 10) });

        if (GUILayout.Button("Cube Block",
                new GUIStyle(GUI.skin.button) { fontSize = 20, padding = new RectOffset(10, 10, 10, 10) },
                new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40)}
            ))
        {
            BlocksGO().AddComponent<CubeDefinition>();
            GetDefinedBlocks();
        }
        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) });
        GUILayout.Box("A cross mesh with a variable height and x and z offset that works for wild grass or flowers.",
                new GUIStyle(GUI.skin.box) { fontSize = 14, padding = new RectOffset(10, 10, 10, 10) });

        if (GUILayout.Button("Cross Mesh Block",
                new GUIStyle(GUI.skin.button) { fontSize = 20, padding = new RectOffset(10, 10, 10, 10) },
                new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) }
            ))
        {
            BlocksGO().AddComponent<CrossMeshDefinition>();
            GetDefinedBlocks();
        }
        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) });
        GUILayout.Box("A block made from an imported mesh with an optional collider. These blocks are less optimized than cubes.",
                new GUIStyle(GUI.skin.box) { fontSize = 14, padding = new RectOffset(10, 10, 10, 10) });

        if (GUILayout.Button("Mesh Block",
                new GUIStyle(GUI.skin.button) { fontSize = 20, padding = new RectOffset(10, 10, 10, 10) },
                new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) }
            ))
        {
            Voxelmetric.resources.textureIndex = new TextureIndex();
            BlocksGO().AddComponent<MeshDefinition>();
            GetDefinedBlocks();
        }
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

    }

    GameObject BlocksGO()
    {
        var blocksTransform = Voxelmetric.resources.worlds[0].gameObject.transform.FindChild("Block Types");
        GameObject blocksGO;
        if (blocksTransform != null)
        {
            blocksGO = blocksTransform.gameObject;
        }
        else
        {
            blocksGO = new GameObject();
            blocksGO.name = "Block Types";
            blocksGO.transform.parent = Voxelmetric.resources.worlds[0].gameObject.transform;
        }
        return blocksGO;
    }

    void GetDefinedBlocks()
    {
        Voxelmetric.resources.textureIndex = new TextureIndex();
        definitions = Voxelmetric.resources.worlds[0].gameObject.GetComponentsInChildren<BlockDefinition>();

        blocks = new BlockController[definitions.Length];
        for (int i = 0; i < definitions.Length; i++)
        {
            blocks[i] = definitions[i].Controller();
        }
    }

    void GetTextureNames()
    {
        List<Texture2D> textures = new List<Texture2D>();
        textures.AddRange(Resources.LoadAll<Texture2D>(Config.Directories.TextureFolder));

        //textureNames = new string[textures.Length];
        List<string> names = new List<string>();
        for (int i = 0; i < textures.Count; i++)
        {
            if (!names.Contains(textures[i].name.Split('-')[0]))
            {
                names.Add(textures[i].name.Split('-')[0]);
            }
        }
        textureNames = names;
    }

}
