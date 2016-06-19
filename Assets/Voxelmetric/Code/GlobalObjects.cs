using UnityEngine;
using Voxelmetric.Code.Common.Threading.Managers;

namespace Voxelmetric.Code
{
    public class GlobalObjects : MonoBehaviour {

        void Awake()
        {
            Globals.InitWorkPool();
            Globals.InitIOPool();
            Globals.InitMemPools();
            Globals.InitWatch();
        }

        void Update ()
        {
            IOPoolManager.Commit();
            WorkPoolManager.Commit();
        }
    }
}
