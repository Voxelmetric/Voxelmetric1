using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stopwatch = System.Diagnostics.Stopwatch;

public class CoroutineThrottle : ICoroutineThrottler {

    public const int TopPriority = 100;
    public const int NormalPriority = 50;

    private World world;

    public CoroutineThrottle(World world) {
        this.world = world;
    }

    public void StartCoroutineRepeater(Func<IEnumerator> coroutineSupplier, int priority) {
        world.StartCoroutine(LoopCoroutine(coroutineSupplier));
    }

    private static IEnumerator LoopCoroutine(Func<IEnumerator> coroutineSupplier) {
        IEnumerator e;
        do {
            e = coroutineSupplier();
            if(e != null) {
                long maxTime = 5; // Limit to 5 milliseconds
                int numDone = 0, numCheck = 16;
                Stopwatch stopwatch = Stopwatch.StartNew();
                while(e.MoveNext()) {
                    ++numDone;
                    if(numDone % numCheck == 0) {
                        if(stopwatch.ElapsedMilliseconds >= maxTime) {
                            stopwatch.Reset();
                            yield return e.Current;
                            stopwatch.Start();
                        }
                    }
                }
                yield return null;
            }
        } while(e != null);
    }
}
