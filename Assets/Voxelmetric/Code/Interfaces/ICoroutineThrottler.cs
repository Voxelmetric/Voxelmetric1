using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public interface ICoroutineThrottler {
    /// <summary>
    /// Submits a repeating coroutine for time-sliced execution
    /// </summary>
    /// <param name="coroutineSupplier">
    /// A function that returns a coroutine.
    /// Returning null indicates that the coroutine should not continue to be executed
    /// </param>
    /// <param name="priority">
    /// Priority for the coroutine execution.
    /// High priority (large numbers) should take precedence over low (small numbers), but it depends on the implementation.
    /// </param>
    void StartCoroutineRepeater(Func<IEnumerator> coroutineSupplier, int priority);
}
