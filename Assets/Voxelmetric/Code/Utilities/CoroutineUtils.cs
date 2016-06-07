using System.Collections;

namespace Voxelmetric.Code.Utilities
{
    public static class CoroutineUtils {

        public static void DoCoroutine(IEnumerator enumerator) {
            while (enumerator.MoveNext()) {
                var current = enumerator.Current;
            }
        }

    }
}
