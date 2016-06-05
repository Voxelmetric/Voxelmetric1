using System.Collections;

public class NonSolidBlockConfig : BlockConfig
{
    public override void SetUp(Hashtable config, World world)
    {
        base.SetUp(config, world);

        //These are defined in the parent class but redefine them with non solid block defaults
        solid = _GetPropertyFromConfig(config, "solid", defaultValue: false);
        transparent = _GetPropertyFromConfig(config, "transparent", defaultValue: true);
        canBeWalkedOn = _GetPropertyFromConfig(config, "canBeWalkedOn", defaultValue: false);
        canBeWalkedThrough = _GetPropertyFromConfig(config, "canBeWalkedThrough", defaultValue: true);
    }
}