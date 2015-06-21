using UnityEngine;
using System.Collections;

public class DirtDefinition : BlockDefenition {

    public override BlockController Controller()
    {
        return new Dirt();
    }

}
