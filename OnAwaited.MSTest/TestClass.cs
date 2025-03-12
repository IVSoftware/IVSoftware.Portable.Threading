using IVSoftware.Portable.Threading;
using static IVSoftware.Portable.Threading.Extensions;
using System.Diagnostics;

namespace OnAwaited.MSTest
{
    [TestClass]
    public sealed class TestClass
    {
        const string 
            EXEC = "ExecClick",
            ERROR = "ERROR", 
            HELLO_WORLD = "Hello World!",
            TYPE_NAME_ERROR = "UNEXPECTED: Type Name Error.";

        /// <summary>
        /// This test is a demonstration of "awaiting the unawaitable" async void.
        /// </summary>
        [TestMethod]
        public async Task AwaitAsynchronousVoid()
        {
            var mockUT = new MockClassUnderTest { TestMode = TestMode.Asynchronous };
            var callbacks = new Dictionary<string, int>();
            var stopwatch = new Stopwatch();
            AwaitedEventArgs? currentEvent = null!;
            SemaphoreSlim awaiter = new SemaphoreSlim(1, 1);
            try
            {
                Awaited += localOnAwaited;

                foreach (var testResponse in Enum.GetValues<TestResponse>())
                {
                    mockUT.TestResponse = testResponse; // Setup.

                    awaiter.Wait(0);
                    stopwatch.Restart();
                    mockUT.ButtonClickMe.PerformClick();
                    await awaiter.WaitAsync();
                    stopwatch.Stop();
                    Assert.IsNotNull(currentEvent);
                    switch (testResponse)
                    {
                        case TestResponse.Default:
                            Assert.AreEqual(1, callbacks[EXEC], "Expecting Caller to match ");
                            Assert.IsTrue(currentEvent?.Args is Dictionary<string, object>, "Expecting Args redirect to dict.");
                            break;
                        case TestResponse.HelloWorldError:
                            Assert.AreEqual(1, callbacks[ERROR], "Expecting this call produces Caller error.");
                            Assert.AreEqual(currentEvent?.Args, HELLO_WORLD);
                            break;
                        case TestResponse.HelloWorldArgs:
                            Assert.AreEqual(2, callbacks[EXEC], "Expecting Caller to match ");
                            Assert.AreEqual(currentEvent?.Args, HELLO_WORLD);
                            break;
                        case TestResponse.CollectionInitializer:
                            Assert.AreEqual(3, callbacks[EXEC], "Expecting Caller to match ");
                            Assert.AreEqual(3, currentEvent.Count, "Expecting dictionary contains 3 KVPs");
                            Assert.AreEqual(HELLO_WORLD, currentEvent["stringKey"], "Expecting dictionary value to match.");
                            Assert.AreEqual(42, currentEvent["intKey"], "Expecting dictionary value to match.");
                            Assert.AreEqual(TestResponse.CollectionInitializer, currentEvent["enumKey"], "Expecting dictionary value to match.");
                            break;
                        default: throw new NotImplementedException();
                    }
                }
                Assert.IsFalse(callbacks.ContainsKey(TYPE_NAME_ERROR), "Type name errors are categorically unexpected.");
            }
            finally
            {
                Awaited -= localOnAwaited;
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



        /// <summary>
        /// This test is a demonstration of "awaiting the unawaitable" async void.
        /// </summary>
        [TestMethod]
        public void SynchronousEventCounting()
        {
            var mockUT = new MockClassUnderTest { TestMode = TestMode.Synchronous };
            var callbacks = new Dictionary<string, int>();
            var stopwatch = new Stopwatch();
            AwaitedEventArgs? currentEvent = null!;
            try
            {
                Extensions.Awaited += localOnAwaited;

                foreach (var testResponse in Enum.GetValues<TestResponse>())
                {
                    mockUT.TestResponse = testResponse; // Setup.
                    mockUT.ButtonClickMe.PerformClick();
                    stopwatch.Stop();
                    Assert.IsNotNull(currentEvent);
                    switch (testResponse)
                    {
                        case TestResponse.Default:
                            Assert.AreEqual(1, callbacks[EXEC], "Expecting Caller to match ");
                            Assert.IsTrue(currentEvent?.Args is Dictionary<string, object>, "Expecting Args redirect to dict.");
                            break;
                        case TestResponse.HelloWorldError:
                            Assert.AreEqual(1, callbacks[ERROR], "Expecting this call produces Caller error.");
                            Assert.AreEqual(currentEvent?.Args, HELLO_WORLD);
                            break;
                        case TestResponse.HelloWorldArgs:
                            Assert.AreEqual(2, callbacks[EXEC], "Expecting Caller to match ");
                            Assert.AreEqual(currentEvent?.Args, HELLO_WORLD);
                            break;
                        case TestResponse.CollectionInitializer:
                            Assert.AreEqual(3, callbacks[EXEC], "Expecting Caller to match ");
                            Assert.AreEqual(3, currentEvent.Count, "Expecting dictionary contains 3 KVPs");
                            Assert.AreEqual(HELLO_WORLD, currentEvent["stringKey"], "Expecting dictionary value to match.");
                            Assert.AreEqual(42, currentEvent["intKey"], "Expecting dictionary value to match.");
                            Assert.AreEqual(TestResponse.CollectionInitializer, currentEvent["enumKey"], "Expecting dictionary value to match.");
                            break;
                        default: throw new NotImplementedException();
                    }
                }
                Assert.IsFalse(callbacks.ContainsKey(TYPE_NAME_ERROR), "Type name errors are categorically unexpected.");
            }
            finally
            {
                Extensions.Awaited -= localOnAwaited;
            }

            void localOnAwaited(object? sender, AwaitedEventArgs e)
            {
                currentEvent = e;
                callbacks.Increment(e.Args.GetType().FullName ?? TYPE_NAME_ERROR);
                switch (e.Caller)
                {
                    case string s when s.StartsWith(ERROR):
                        callbacks.Increment(ERROR);
                        break;
                    default:
                        callbacks.Increment(e.Caller);
                        break;
                }
            }
        }
        enum TestResponse
        {
            Default,
            HelloWorldError,
            HelloWorldArgs,
            CollectionInitializer,
        }
        enum TestMode 
        {
            Asynchronous,
            Synchronous,
        }
        class MockClassUnderTest
        {
            public TestResponse TestResponse { get; set; }
            public TestMode TestMode { get; set; }
            
            public MockButton ButtonClickMe { get; } = new MockButton
            {
                Text = "Click Me",
            };
            public MockClassUnderTest() => ButtonClickMe.Clicked += ExecClick;

            protected virtual void ExecClick(object? sender, EventArgs e)
            {
                switch (TestMode)
                {
                    case TestMode.Asynchronous:
                        Task.Run(async () =>
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1.1));
                            localExecClick();
                        });
                        break;
                    case TestMode.Synchronous:
                        localExecClick();
                        break;
                    default: throw new NotImplementedException();
                }

                void localExecClick()
                {
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
                                { "stringKey", HELLO_WORLD },                       // String value
                                { "intKey", 42 },                                   // Integer value
                                { "enumKey", TestResponse.CollectionInitializer }   // Enum Value 
                            });
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
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
