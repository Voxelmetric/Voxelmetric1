using UnityEngine;
using System.Collections;

public class BlockDefinition : MonoBehaviour {

    public virtual BlockController Controller()
    {
        return new BlockAir();
    }
    
    public virtual void AddToBlocks()
    {
        Block.index.AddBlockType(Controller());
    }
}
