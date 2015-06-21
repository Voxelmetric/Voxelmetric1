using UnityEngine;
using System.Collections;

public class LogDefinition : BlockDefenition {

    public override BlockController Controller()
    {
        return new Log();
    }

}
