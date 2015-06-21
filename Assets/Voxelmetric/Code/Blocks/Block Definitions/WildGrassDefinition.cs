using UnityEngine;
using System.Collections;

public class WildGrassDefinition : BlockDefenition {

    public override BlockController Controller()
    {
        return new WildGrass();
    }

}
