using UnityEngine;
using System.Collections;

public class GrassDefinition : BlockDefenition {

    public override BlockController Controller()
    {
        return new Grass();
    }

}
