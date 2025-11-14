using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.Threading;

namespace OnAwaited.MSTest;

[TestClass]
public class TestClass_CallerDisambiguation
{
    [TestMethod]
    public void Test_DefaultArgsAmbiguity()
    {
        AwaitedEventArgs eut;

        eut = new AwaitedEventArgs(caller: @"System.Collections.Generic.IList<T>.Item");
        Assert.AreEqual("System.Collections.Generic.IList<T>.Item", eut.Caller);
    }
}
