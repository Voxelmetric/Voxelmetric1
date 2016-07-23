using System.Collections;
using Voxelmetric.Code.Core;

public class NonSolidBlockConfig : BlockConfig
{
    public override bool SetUp(Hashtable config, World world)
    {
        if (!base.SetUp(config, world))
            return false;

        //These are defined in the parent class but redefine them with non solid block defaults
        solid = _GetPropertyFromConfig(config, "solid", false);
        transparent = _GetPropertyFromConfig(config, "transparent", true);
        canBeWalkedOn = _GetPropertyFromConfig(config, "canBeWalkedOn", false);
        canBeWalkedThrough = _GetPropertyFromConfig(config, "canBeWalkedThrough", true);

        return false;
    }
}