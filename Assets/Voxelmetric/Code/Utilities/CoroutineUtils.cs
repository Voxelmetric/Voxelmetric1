using System.Collections;

public class CoroutineUtils {

    public static void DoCoroutine(IEnumerator enumerator) {
        while (enumerator.MoveNext()) {
            var current = enumerator.Current;
        }
    }

}
