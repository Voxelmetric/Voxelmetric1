using Assets.Voxelmetric.Code;
using Assets.Voxelmetric.Code.Common.Threading.Managers;
using UnityEngine;

public class GlobalObjects : MonoBehaviour {

    void Awake()
    {
        Globals.InitWorkPool();
        Globals.InitIOPool();
        Globals.InitNetworkPool();
        Globals.InitMemPools();
    }
	
	void Update ()
    {
        IOPoolManager.Commit();
	    NetworkPoolManager.Commit();
        WorkPoolManager.Commit();
	}
}
