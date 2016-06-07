using System.Collections;
using NUnit.Framework;
using Voxelmetric.Code.Utilities;

public class CoroutineUtilsTest {

    private class Tester {
        public int innerCalls = 0;

        public IEnumerator CoroutineTester() {
            for (int i = 0; i < 10; ++i) {
                yield return i;
                ++innerCalls;
            }
        }

        public IEnumerator NestedCoroutineTester() {
            for (int i = 0; i < 3; ++i) {
                var enumerator = CoroutineTester();
                while (enumerator.MoveNext())
                    yield return enumerator.Current;
            }
        }
    }

    [Test]
    public void DoCoroutineTest() {
        var tester = new Tester();
        CoroutineUtils.DoCoroutine(tester.CoroutineTester());
        Assert.AreEqual(10, tester.innerCalls);
    }

    [Test]
    public void NestCoroutineTest() {
        var tester = new Tester();
        CoroutineUtils.DoCoroutine(tester.NestedCoroutineTester());
        Assert.AreEqual(30, tester.innerCalls);
    }

}
