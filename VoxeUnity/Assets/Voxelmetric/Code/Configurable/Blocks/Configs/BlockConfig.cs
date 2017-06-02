using System;
using System.Collections;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Blocks;

/// <summary>
/// BlockConfigs define constants for block types. Things like if the block is solid,
/// the block's texture etc. We could have used static variables in the block class
/// but the same block class can be used by different blocks - for example block cube
/// can be used by any cube block with a different texture for each block by defining the
/// texture for each of them in the block's json config. Then a BlockConfig will be
/// created for each block type and stored in BlockIndex referenced by the block type.
/// </summary>
public class BlockConfig
{
    //! Block type. Set externally by BlockIndex class when config is loaded
    public ushort type = 1;

    #region Parameters read from config

    //! Unique identifier of block config
    public ushort typeInConfig { get; protected set; }
    //! Unique identifier of block config
    public string name { get; protected set; }

    private string m_className;
    public string className
    {
        get { return m_className; }
        set
        {
            m_className = value;
            blockClass = Type.GetType(value + ", " + typeof(Block).Assembly, false);
        }
    }

    public Type blockClass { get; protected set; }

    public bool solid { get; protected set; }
    public bool transparent { get; protected set; }
    public bool raycastHit { get; protected set; }
    public bool raycastHitOnRemoval { get; protected set; }
    public int renderMaterialID { get; protected set; }
    public int physicMaterialID { get; protected set; }
    
    #endregion

    public static BlockConfig CreateAirBlockConfig(World world)
    {
        return new BlockConfig
        {
            name = "air",
            typeInConfig = BlockProvider.AirType,
            className = "Block",
            solid = false,
            transparent = true,
            physicMaterialID = -1
        };
    }

    /// <summary>
    /// Assigns the variables in the config from a hashtable. When overriding this
    /// remember to call the base function first.
    /// </summary>
    /// <param name="config">Hashtable of the json config for the block</param>
    /// <param name="world">The world this block type belongs to</param>
    public virtual bool OnSetUp(Hashtable config, World world)
    {
        // Obligatory parameters
        {
            string tmpName;
            if (!_GetPropertyFromConfig(config, "name", out tmpName))
            {
                Debug.LogError("Parameter 'name' missing from config");
                return false;
            }
            name = tmpName;

            long tmpTypeInConfig;
            if (!_GetPropertyFromConfig(config, "type", out tmpTypeInConfig))
            {
                Debug.LogError("Parameter 'type' missing from config");
                return false;
            }
            typeInConfig = (ushort)tmpTypeInConfig;
        }
        
        // Optional parameters
        {
            className = _GetPropertyFromConfig(config, "blockClass", "Block");
            solid = _GetPropertyFromConfig(config, "solid", true);
            transparent = _GetPropertyFromConfig(config, "transparent", false);
            raycastHit = _GetPropertyFromConfig(config, "raycastHit", solid);
            raycastHitOnRemoval = _GetPropertyFromConfig(config, "raycastHitOnRemoval", solid);

            // Try to associate requested render materials with one of world's materials
            {
                renderMaterialID = 0;
                string materialName = _GetPropertyFromConfig(config, "material", "");
                for (int i = 0; i<world.renderMaterials.Length; i++)
                    if (world.renderMaterials[i].name.Equals(materialName))
                    {
                        renderMaterialID = i;
                        break;
                    }
            }

            // Try to associate requested physic materials with one of world's materials
            {
                physicMaterialID = solid ? 0 : -1; // solid objects will collide by default
                string materialName = _GetPropertyFromConfig(config, "materialPx", "");
                for (int i = 0; i < world.physicsMaterials.Length; i++)
                    if (world.physicsMaterials[i].name.Equals(materialName))
                    {
                        physicMaterialID = i;
                        break;
                    }
            }
        }

        return true;
    }

    public virtual bool OnPostSetUp(World world)
    {
        return true;
    }

    public override string ToString()
    {
        return name;
    }

    protected static bool _GetPropertyFromConfig<T>(Hashtable config, string key, out T ret)
    {
        if (config.ContainsKey(key))
        {
            ret = (T)config[key];
            return true;
        }

        ret = default(T);
        return false;
    }

    protected static T _GetPropertyFromConfig<T>(Hashtable config, string key, T defaultValue)
    {
        if (config.ContainsKey(key))
            return (T)config[key];

        return defaultValue;
    }
}