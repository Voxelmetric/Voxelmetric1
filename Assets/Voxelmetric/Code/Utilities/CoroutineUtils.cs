using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class CoroutineUtils {

    public static void DoCoroutine(IEnumerator enumerator) {
        while (enumerator.MoveNext()) {
            var current = enumerator.Current;
        }
    }

}
