using UnityEngine;
using System.Collections;

public class SandDefinition : BlockDefenition {

    public override BlockController Controller()
    {
        return new Sand();
    }

}
