using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class EditLayers
{
    bool resourcesFetched = false;
    int selected;
    Vector2 listScroll = new Vector2();
    Vector2 editScroll = new Vector2();

    List<string> blockNames = new List<string>();
    TerrainGen gen;

    public void LayersTab()
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
            GetTerrainLayers();
            resourcesFetched = true;
        }

        GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.ExpandHeight(true) });

        GUILayout.BeginHorizontal();
        GUILayout.Label("Layers", new GUIStyle { fontSize = 28 });

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("New", new GUIStyle("button") { fontSize = 20 }))
        {
            selected = -1;
        }

        if (GUILayout.Button("Refresh", new GUIStyle("button") { fontSize = 20 }))
        {
            GetDefinedBlocks();
            GetTerrainLayers();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        listScroll = GUILayout.BeginScrollView(listScroll, new GUIStyle(GUI.skin.box));
        for (int i = 0; i < gen.layerOrder.Length; i++)
        {
            FontStyle fontStyle = FontStyle.Normal;
            if (i == selected)
            {
                fontStyle = FontStyle.Bold;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button((i + 1) + ": " + gen.layerOrder[i].layerName,
                new GUIStyle(GUI.skin.box) { fontSize = 14, fontStyle = fontStyle, padding = new RectOffset(10, 10, 10, 10) },
                new GUILayoutOption[] { GUILayout.ExpandWidth(true) }
            ))
            {
                selected = i;
            }

            if (GUILayout.Button(@"^",
                new GUIStyle(GUI.skin.box) { fontSize = 14, padding = new RectOffset(10, 10, 10, 10) },
                new GUILayoutOption[] { GUILayout.MinWidth(40) }
            ))
            {
                MoveLayerOrder(i - 1, i);
            }

            if (GUILayout.Button("v",
                new GUIStyle(GUI.skin.box) { fontSize = 14, padding = new RectOffset(10, 10, 10, 10) },
                new GUILayoutOption[] { GUILayout.MinWidth(40) }
            ))
            {
                MoveLayerOrder(i + 1, i);
            }

            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();

        GUILayout.Space(10);

        GUILayout.EndVertical();
    }

    void BlockEdit()
    {
        editScroll = GUILayout.BeginScrollView(editScroll, new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(15, 15, 15, 15),
            margin = new RectOffset(15, 0, 0, 19)
        }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true) });

        if (selected < gen.layerOrder.Length && selected >= 0 && gen.layerOrder.Length != 0)
        {
            GUILayout.Label("Edit: " + gen.layerOrder[selected].layerName, new GUIStyle { fontSize = 18, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
            GUILayout.Space(30);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
            gen.layerOrder[selected].layerName = EditorGUILayout.TextField(gen.layerOrder[selected].layerName,
                new GUIStyle(GUI.skin.textField) { fontSize = 14, }, new GUILayoutOption[] { GUILayout.Height(18), GUILayout.ExpandWidth(true) });
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.Space(10);
            int i = 0;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Layer type", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
            i = EditorGUILayout.Popup((int)gen.layerOrder[selected].layerType, new string[] { "Absolute", "Additive", "Surface", "Structure", "Chance" },
                new GUIStyle(GUI.skin.textField) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
            if (i >= 0)
                gen.layerOrder[selected].layerType = (TerrainLayer.LayerType)i;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            if (gen.layerOrder[selected].layerType == TerrainLayer.LayerType.Surface)
            {
                GUILayout.Box("Surface layers are used for a 1 block thick layer that covers all the terrain",
               new GUIStyle(GUI.skin.box) { fontSize = 14, padding = new RectOffset(10, 10, 10, 10) }, new GUILayoutOption[]{ GUILayout.ExpandWidth(true)});
            }
            else if (gen.layerOrder[selected].layerType == TerrainLayer.LayerType.Absolute )
            {
                GUILayout.Box("Absolute layers replace terrain starting from the bottom up to their generated height, additive layers add their height on top of the terrain generated so far. For these layers min and max height are the heights the noise generation will keep within, frequency is the distance between the peaks and valleys in the noise and exponent is used by getting the height after the noise and getting it to the power of the supplied exponent.",
               new GUIStyle(GUI.skin.box) { fontSize = 14, padding = new RectOffset(10, 10, 10, 10) }, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
            }
            else if (gen.layerOrder[selected].layerType == TerrainLayer.LayerType.Chance)
            {
                GUILayout.Box("Chance layers create a block if a random chance is larger than the percentage you define, use it for distributing blocks randomly onto the terrain.",
               new GUIStyle(GUI.skin.box) { fontSize = 14, padding = new RectOffset(10, 10, 10, 10) }, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (gen.layerOrder[selected].layerType != TerrainLayer.LayerType.Structure)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Block type", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                i = EditorGUILayout.Popup(blockNames.IndexOf(gen.layerOrder[selected].blockName), blockNames.ToArray(),
                    new GUIStyle(GUI.skin.textField) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                if (i >= 0)
                    gen.layerOrder[selected].blockName = blockNames[i];
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                if (gen.layerOrder[selected].layerType != TerrainLayer.LayerType.Surface && gen.layerOrder[selected].layerType != TerrainLayer.LayerType.Chance)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Min height", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                    gen.layerOrder[selected].baseHeight = (int)EditorGUILayout.FloatField(gen.layerOrder[selected].baseHeight,
                        new GUIStyle(GUI.skin.textField) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.Height(18), GUILayout.Width(48) });
                    GUILayout.Space(10);
                    gen.layerOrder[selected].baseHeight = (int)GUILayout.HorizontalSlider((float)gen.layerOrder[selected].baseHeight, 0f, 256f,
                        new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                    GUILayout.Space(10);
                    GUILayout.EndHorizontal();

                    GUILayout.Space(10);

                    GUILayout.BeginHorizontal();
                    int maxHeight = gen.layerOrder[selected].amplitude + gen.layerOrder[selected].baseHeight;
                    GUILayout.Label("Max height", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                    maxHeight = (int)EditorGUILayout.FloatField(maxHeight, new GUIStyle(GUI.skin.textField) { fontSize = 14 },
                        new GUILayoutOption[] { GUILayout.Height(18), GUILayout.Width(48) });
                    GUILayout.Space(10);
                    maxHeight = (int)GUILayout.HorizontalSlider(maxHeight, 0f, 256f,
                        new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                    gen.layerOrder[selected].amplitude = maxHeight - gen.layerOrder[selected].baseHeight;
                    GUILayout.Space(10);
                    GUILayout.EndHorizontal();

                    GUILayout.Space(10);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Frequency", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                    gen.layerOrder[selected].frequency = (int)EditorGUILayout.FloatField(gen.layerOrder[selected].frequency,
                new GUIStyle(GUI.skin.textField) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.Height(18), GUILayout.Width(48) });
                    GUILayout.Space(10);
                    gen.layerOrder[selected].frequency = (int)GUILayout.HorizontalSlider((float)gen.layerOrder[selected].frequency, 0f, 256f,
                        new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                    GUILayout.Space(10);
                    GUILayout.EndHorizontal();

                    GUILayout.Space(10);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Exponent", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                    gen.layerOrder[selected].exponent = EditorGUILayout.FloatField(gen.layerOrder[selected].exponent,
                        new GUIStyle(GUI.skin.textField) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.Height(18), GUILayout.Width(48) });
                    GUILayout.Space(10);
                    gen.layerOrder[selected].exponent = GUILayout.HorizontalSlider(gen.layerOrder[selected].exponent, 1f, 3f,
                        new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                    GUILayout.Space(10);
                    GUILayout.EndHorizontal();

                    GUILayout.Space(10);
                }
            }

            if (gen.layerOrder[selected].layerType == TerrainLayer.LayerType.Chance)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Chance to create block", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                gen.layerOrder[selected].chanceToSpawnBlock = (int)EditorGUILayout.FloatField(gen.layerOrder[selected].chanceToSpawnBlock,
                    new GUIStyle(GUI.skin.textField) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.Height(18), GUILayout.Width(48) });
                GUILayout.Space(10);
                gen.layerOrder[selected].chanceToSpawnBlock = (int)GUILayout.HorizontalSlider(gen.layerOrder[selected].chanceToSpawnBlock, 0, 100,
                    new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                GUILayout.Space(10);
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
            }

            if (gen.layerOrder[selected].layerType == TerrainLayer.LayerType.Structure)
            {
                GUILayout.Box("For structure layers you can define how often the structure should be generated, this is not an actual percentage, per block. You can also define the Structure class to use for building.",
               new GUIStyle(GUI.skin.box) { fontSize = 14, padding = new RectOffset(10, 10, 10, 10) });

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Percentage", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                gen.layerOrder[selected].chanceToSpawnBlock = (int)EditorGUILayout.FloatField(gen.layerOrder[selected].chanceToSpawnBlock,
                    new GUIStyle(GUI.skin.textField) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.Height(18), GUILayout.Width(48) });
                GUILayout.Space(10);
                gen.layerOrder[selected].chanceToSpawnBlock = (int)GUILayout.HorizontalSlider((float)gen.layerOrder[selected].chanceToSpawnBlock, 1f, 100f,
                    new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(18) });
                GUILayout.Space(10);
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Structure name", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                gen.layerOrder[selected].structureClassName = EditorGUILayout.TextField(gen.layerOrder[selected].structureClassName,
                    new GUIStyle(GUI.skin.textField) { fontSize = 14, }, new GUILayoutOption[] { GUILayout.Height(18), GUILayout.ExpandWidth(true) });
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }

            

            if (gen.layerOrder[selected].customTerrainLayer)
            {
                GUILayout.Box("This custom layer uses a class that extends Terrain Layer for generation. All the properties above will be passed to your terrain class for convenience but you don't need to use them.",
               new GUIStyle(GUI.skin.box) { fontSize = 14, padding = new RectOffset(10, 10, 10, 10) });

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Custom terrain layer name", new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleCenter }, new GUILayoutOption[] { GUILayout.Width(200) });
                gen.layerOrder[selected].terrainLayerClassName = EditorGUILayout.TextField(gen.layerOrder[selected].terrainLayerClassName,
                    new GUIStyle(GUI.skin.textField) { fontSize = 14 }, new GUILayoutOption[] { GUILayout.Height(18), GUILayout.ExpandWidth(true) });
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

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
                Object.DestroyImmediate(gen.layerOrder[selected]);
                GetTerrainLayers();
                selected = -1;
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (GUI.changed && selected < gen.layerOrder.Length && selected >=0)
                EditorUtility.SetDirty(gen.layerOrder[selected]);
        }
        else if (selected == -1)
        {
            NewBlock();
        }

        GUILayout.EndScrollView();
    }

    void NewBlock()
    {
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) });
        GUILayout.Box("This type of layer fills from the bottom to a certain height replacing what is already there",
                new GUIStyle(GUI.skin.box) { fontSize = 14, padding = new RectOffset(10, 10, 10, 10) });

        if (GUILayout.Button("Absolute",
                new GUIStyle(GUI.skin.button) { fontSize = 20, padding = new RectOffset(10, 10, 10, 10) },
                new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) }
            ))
        {
            TerrainLayer newLayer = LayersGO().AddComponent<TerrainLayer>();
            newLayer.layerType = TerrainLayer.LayerType.Absolute;
            AddToLayerOrder(newLayer);
            GetTerrainLayers();
        }
        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) });
        GUILayout.Box("This layer type adds to the top of whatever is already there",
                new GUIStyle(GUI.skin.box) { fontSize = 14, padding = new RectOffset(10, 10, 10, 10) });

        if (GUILayout.Button("Additive",
                new GUIStyle(GUI.skin.button) { fontSize = 20, padding = new RectOffset(10, 10, 10, 10) },
                new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) }
            ))
        {
            TerrainLayer newLayer = LayersGO().AddComponent<TerrainLayer>();
            newLayer.layerType = TerrainLayer.LayerType.Absolute;
            AddToLayerOrder(newLayer);
            GetTerrainLayers();
        }
        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) });
        GUILayout.Box("This layer type is for one block thick top layers what should blend with other surface layers at biome edges",
                new GUIStyle(GUI.skin.box) { fontSize = 14, padding = new RectOffset(10, 10, 10, 10) });

        if (GUILayout.Button("Surface",
                new GUIStyle(GUI.skin.button) { fontSize = 20, padding = new RectOffset(10, 10, 10, 10) },
                new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) }
            ))
        {
            TerrainLayer newLayer = LayersGO().AddComponent<TerrainLayer>();
            newLayer.layerType = TerrainLayer.LayerType.Absolute;
            AddToLayerOrder(newLayer);
            GetTerrainLayers();
        }
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) });
        GUILayout.Box("This layer is used to place structures defined in a GeneratedStructure class on the terrain",
                new GUIStyle(GUI.skin.box) { fontSize = 14, padding = new RectOffset(10, 10, 10, 10) });

        if (GUILayout.Button("Structure",
                new GUIStyle(GUI.skin.button) { fontSize = 20, padding = new RectOffset(10, 10, 10, 10) },
                new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) }
            ))
        {
            TerrainLayer newLayer = LayersGO().AddComponent<TerrainLayer>();
            newLayer.layerType = TerrainLayer.LayerType.Absolute;
            AddToLayerOrder(newLayer);
            GetTerrainLayers();
        }
        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) });
        GUILayout.Box("References a custom terrain layer defined as a class extending TerrainLayer",
                new GUIStyle(GUI.skin.box) { fontSize = 14, padding = new RectOffset(10, 10, 10, 10) });

        if (GUILayout.Button("Custom",
                new GUIStyle(GUI.skin.button) { fontSize = 20, padding = new RectOffset(10, 10, 10, 10) },
                new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) }
            ))
        {
            TerrainLayer newLayer = LayersGO().AddComponent<TerrainLayer>();
            newLayer.layerType = TerrainLayer.LayerType.Absolute;
            newLayer.customTerrainLayer = true;
            AddToLayerOrder(newLayer);
            GetTerrainLayers();
        }
        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) });
        GUILayout.Box("This layer is used to create blocks randomly based on a percentage chance to spawn.",
                new GUIStyle(GUI.skin.box) { fontSize = 14, padding = new RectOffset(10, 10, 10, 10) });

        if (GUILayout.Button("Chance",
                new GUIStyle(GUI.skin.button) { fontSize = 20, padding = new RectOffset(10, 10, 10, 10) },
                new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(40) }
            ))
        {
            TerrainLayer newLayer = LayersGO().AddComponent<TerrainLayer>();
            newLayer.layerType = TerrainLayer.LayerType.Chance;
            AddToLayerOrder(newLayer);
            GetTerrainLayers();
        }
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();


    }

    GameObject LayersGO()
    {
        var layersTransform = Voxelmetric.resources.worlds[0].gameObject.transform.FindChild("Terrain Layers");
        GameObject layersGO;
        if (layersTransform != null)
        {
            layersGO = layersTransform.gameObject;
        }
        else
        {
            layersGO = new GameObject();
            layersGO.name = "Terrain Layers";
            layersGO.transform.parent = Voxelmetric.resources.worlds[0].gameObject.transform;
        }
        return layersGO;
    }

    void GetDefinedBlocks()
    {
        Voxelmetric.resources.textureIndex = new TextureIndex();
        var definitions = World.instance.gameObject.GetComponentsInChildren<BlockDefinition>();
        blockNames.Add("air");

        for (int i = 0; i < definitions.Length; i++)
        {
            blockNames.Add(definitions[i].Controller().Name());
        }

    }

    void MoveLayerOrder(int newIndex, int oldIndex)
    {
        if (newIndex >= gen.layerOrder.Length || newIndex < 0)
            return;

        if (selected == oldIndex)
            selected = newIndex;

        List<TerrainLayer> layers = new List<TerrainLayer>();
        layers.AddRange(gen.layerOrder);
        layers.RemoveAt(oldIndex);
        layers.Insert(newIndex, gen.layerOrder[oldIndex]);
        gen.layerOrder = layers.ToArray();
        EditorUtility.SetDirty(gen);
    }

    void AddToLayerOrder(TerrainLayer layer)
    {
        List<TerrainLayer> layers = new List<TerrainLayer>();
        layers.AddRange(gen.layerOrder);
        layers.Add(layer);
        gen.layerOrder = layers.ToArray();
        EditorUtility.SetDirty(gen);
    }

    void GetTerrainLayers()
    {
        gen = World.instance.gameObject.GetComponent<TerrainGen>();
        List<TerrainLayer> layers = new List<TerrainLayer>();
        layers.AddRange(gen.layerOrder);
        if (layers.Remove(null))
        {
            EditorUtility.SetDirty(gen);
        }
        gen.layerOrder = layers.ToArray();
    }

}
