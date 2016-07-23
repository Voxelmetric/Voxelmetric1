using System.Collections;
using Voxelmetric.Code.Core;

public class SolidBlockConfig : BlockConfig
{
    public bool solidTowardsSameType;
    public override bool SetUp(Hashtable config, World world)
    {
        if (!base.SetUp(config, world))
            return false;

        solidTowardsSameType = _GetPropertyFromConfig(config, "solidTowardsSameType", false);

        return true;
    }
}