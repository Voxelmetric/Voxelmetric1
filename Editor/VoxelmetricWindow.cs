using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class VoxelmetricWindow : EditorWindow {

    int tab;
    EditBlocks blocks = new EditBlocks();
    EditLayers layers = new EditLayers();

    [MenuItem("Window/Voxelmetric")]
    public static void ShowWindow()
    {
        GetWindow(typeof(VoxelmetricWindow));
    }

    void OnGUI()
    {
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        tab = GUILayout.Toolbar(tab, new string[] { "Blocks", "Biomes", "Layers" },
            new GUILayoutOption[] { GUILayout.MinWidth(700), GUILayout.Height(35), GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000) });
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(25);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal( new GUILayoutOption[] { GUILayout.ExpandHeight(true), GUILayout.MinWidth(700), GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000) });
        switch (tab)
        {
            case 0:
                blocks.BlocksTab();
                break;
            case 1:
                BiomesTab();
                break;
            case 2:
                layers.LayersTab();
                break;
        }
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    

    void BiomesTab() { }

}