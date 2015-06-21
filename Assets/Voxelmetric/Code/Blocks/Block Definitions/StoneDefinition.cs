using UnityEngine;
using System.Collections;

public class StoneDefinition : BlockDefenition {

    public override BlockController Controller()
    {
        return new Stone();
    }

}
