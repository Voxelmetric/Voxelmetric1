using UnityEngine;
using System;
using System.Collections;

public class SolidBlockConfig : BlockConfig
{
    public bool solidTowardsSameType;
    public override void SetUp(Hashtable config, World world)
    {
        base.SetUp(config, world);
        solidTowardsSameType = _GetPropertyFromConfig(config, "solidTowardsSameType", false);
    }
}