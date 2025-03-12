using IVSoftware.Portable.Threading;
using System.Diagnostics;

namespace OnAwaited.MSTest
{
    [TestClass]
    public sealed class TestClass
    {
        const string 
            ERROR = "ERROR", 
            HELLO_WORLD = "Hello World!",
            TYPE_NAME_ERROR = "UNEXPECTED: Type Name Error.";

        /// <summary>
        /// This test is a demonstration of "awaiting the unawaitable" while
        /// also 
        /// </summary>
        [TestMethod]
        public async Task MainTest()
        {
            var mockUT = new MockClassUnderTest();
            var callbacks = new Dictionary<string, int>();
            var stopwatch = new Stopwatch();
            AwaitedEventArgs? currentEvent = null!;
            SemaphoreSlim awaiter = new SemaphoreSlim(1, 1);
            try
            {
                Extensions.Awaited += localOnAwaited;

                foreach (var testResponse in Enum.GetValues<TestResponse>())
                {
                    mockUT.TestResponse = testResponse; // Setup.

                    awaiter.Wait(0);
                    stopwatch.Restart();
                    mockUT.ButtonClickMe.PerformClick();
                    await awaiter.WaitAsync();
                    stopwatch.Stop();
                    switch (testResponse)
                    {
                        case TestResponse.Default:
                            Assert.AreEqual(1, callbacks["ExecAsyncTask"], "Expecting Caller to match ");
                            Assert.IsTrue(currentEvent?.Args is Dictionary<string, object>, "Expecting Args redirect to dict.");
                            break;
                        case TestResponse.HelloWorldError:
                            Assert.AreEqual(1, callbacks[ERROR], "Expecting this call produces Caller error.");
                            Assert.AreEqual(currentEvent?.Args, HELLO_WORLD);
                            break;
                        case TestResponse.HelloWorldArgs:
                            Assert.AreEqual(2, callbacks["ExecAsyncTask"], "Expecting Caller to match ");
                            Assert.AreEqual(currentEvent?.Args, HELLO_WORLD);
                            break;
                        case TestResponse.CollectionInitializer:
                            Assert.AreEqual(3, callbacks["ExecAsyncTask"], "Expecting Caller to match ");
                            { }
                            Assert.AreEqual(2, currentEvent?.Count);
                            break;
                        default: throw new NotImplementedException();
                    }
                }
                Assert.AreEqual(0, callbacks[TYPE_NAME_ERROR], "Type name errors are categorically unexpected.");
            }
            finally
            {
                Extensions.Awaited -= localOnAwaited;
                awaiter.Wait(0);
                awaiter.Release();
            }

            void localOnAwaited(object? sender, AwaitedEventArgs e)
            {
                currentEvent = e;
                callbacks.Increment(e.Args.GetType().FullName ?? TYPE_NAME_ERROR);
                switch(e.Caller)
                {
                    case string s when s.StartsWith(ERROR):
                        callbacks.Increment(ERROR);
                        break;
                    default:
                        callbacks.Increment(e.Caller);
                        break;
                }
                awaiter.Release();
            }
        }

        enum TestResponse
        {
            Default,
            HelloWorldError,
            HelloWorldArgs,
            CollectionInitializer,
        }
        class MockClassUnderTest
        {
            public TestResponse TestResponse { get; set; }
            public MockButton ButtonClickMe { get; } = new MockButton
            {
                Text = "Click Me",
            };
            public MockClassUnderTest() => ButtonClickMe.Clicked += ExecAsyncTask;

            protected virtual void ExecAsyncTask(object? sender, EventArgs e)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(1.1));
                    switch (TestResponse)
                    {
                        case TestResponse.Default:
                            this.OnAwaited();
                            break;
                        case TestResponse.HelloWorldError:
                            this.OnAwaited(new AwaitedEventArgs(HELLO_WORLD));
                            break;
                        case TestResponse.HelloWorldArgs:
                            this.OnAwaited(new AwaitedEventArgs(args: HELLO_WORLD));
                            break;
                        case TestResponse.CollectionInitializer:
                            this.OnAwaited(new AwaitedEventArgs {
                                { "stringKey", "Hello World" },       // String value
                                { "intKey", 42 },                     // Integer value
                                { "objectKey", new { Name = "IVSoft", Version = 1.0 } }  // Anonymous object
                            });
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                });
            }
        }
        class MockButton
        {
            public string Text { get; set; } = string.Empty;
            public void PerformClick() => Clicked?.Invoke(this, EventArgs.Empty);
            public event EventHandler? Clicked;
        }
    }
    public static partial class TestExtensions
    {
        public static int Increment(this Dictionary<string, int> @this, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException($"The {nameof(key)} argument cannot be null or empty");
            if (@this.TryGetValue(key, out var value)) value++;
            else value = 1;
            @this[key] = value;
            return value;
        }
    }
}
