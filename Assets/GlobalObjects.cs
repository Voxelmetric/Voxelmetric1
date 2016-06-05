using Assets.Voxelmetric.Code;
using Assets.Voxelmetric.Code.Common.Threading.Managers;
using UnityEngine;

public class GlobalObjects : MonoBehaviour {

    void Awake()
    {
        Globals.InitMemPools();
        Globals.InitIOPool();
        Globals.InitNetworkPool();
        Globals.InitWorkPool();
    }
	
	void Update ()
    {
        IOPoolManager.Commit();
	    NetworkPoolManager.Commit();
        WorkPoolManager.Commit();
	}
}
