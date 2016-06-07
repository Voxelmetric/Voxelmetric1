using System;
using System.Collections;
using Voxelmetric.Code.Core;

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
    public int type;
    public World world;

    public string name;
    public Type blockClass;
    public bool solid;
    public bool transparent;
    public bool canBeWalkedOn;
    public bool canBeWalkedThrough;

    /// <summary>
    /// Assigns the variables in the config from a hashtable. When overriding this
    /// remember to call the base function first.
    /// </summary>
    /// <param name="config">Hashtable of the json config for the block</param>
    /// <param name="world">The world this block type belongs to</param>
    public virtual void SetUp(Hashtable config, World world)
    {
        this.world = world;
        type = _GetPropertyFromConfig(config, "type", defaultValue: -1);
        name = _GetPropertyFromConfig(config, "name", defaultValue: "block");

        string blockClassName = _GetPropertyFromConfig(config, "blockClass", defaultValue: "Block");
        blockClass = Type.GetType(blockClassName + ", " + typeof(Block).Assembly, false);

        solid = _GetPropertyFromConfig(config, "solid", defaultValue: true);
        transparent = _GetPropertyFromConfig(config, "transparent", defaultValue: false);
        canBeWalkedOn = _GetPropertyFromConfig(config, "canBeWalkedOn", defaultValue: true);
        canBeWalkedThrough = _GetPropertyFromConfig(config, "canBeWalkedThrough", defaultValue: false);
    }

    public override string ToString()
    {
        return name;
    }

    protected static T _GetPropertyFromConfig<T>(Hashtable config, object key, T defaultValue)
    {
        if (config.ContainsKey(key))
        {
            return (T)config[key];
        }
        else
        {
            return defaultValue;
        }
    }
}