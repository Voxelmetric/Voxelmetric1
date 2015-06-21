using UnityEngine;
using System.Collections;

public class LeavesDefinition : BlockDefenition {

    public override BlockController Controller()
    {
        return new Leaves();
    }

}
