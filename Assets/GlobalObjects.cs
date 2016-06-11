using UnityEngine;
using Voxelmetric.Code;
using Voxelmetric.Code.Common.Threading.Managers;

public class GlobalObjects : MonoBehaviour {

    void Awake()
    {
        Globals.InitWorkPool();
        Globals.InitIOPool();
        Globals.InitMemPools();
    }

	void Update ()
    {
        IOPoolManager.Commit();
	    WorkPoolManager.Commit();

	    //Debug.Log(Globals.MemPools.ToString());
    }
}
