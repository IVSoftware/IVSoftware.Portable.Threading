using IVSoftware.Portable.Threading;
using System.Diagnostics;
using IVSoftware.Portable.Disposable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using IVSoftware.WinOS.MSTest.Extensions;
using MSTest.Async.Demo;

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
        /// Thest the combo of 'explicit args:' + 'collection initializer'.
        /// </summary>
        /// <remarks>
        /// Threading the needle, where Args is initialized as a string (thus
        /// making the AwaitedEventArgs.Args property 'not a dictionary` but 
        /// still using the initializer list to append the underlying _dict 
        /// because AwaitedEventArgs *itself* is 'still a dictionary` always.
        /// </remarks>
        [TestMethod]
        public void HybridCollectionInitializer()
        {
            object @this = new();
            try
            {
                #region L o c a l F x 
                void localOnAwaited(object? sender, AwaitedEventArgs e)
                {
                    Assert.AreEqual(e.Args, "Hello World!");
                    Assert.IsTrue(e.ContainsKey("Key"));
                    Assert.AreEqual(e["Key"], "Value");
                }
                #endregion L o c a l F x
                using (this.WithOnDispose(
                    onInit: (sender, e) =>
                        {
                            AwaitedEventArgs.Awaited += localOnAwaited;
                        },
                    onDispose: (sender, e) =>
                        {
                            AwaitedEventArgs.Awaited -= localOnAwaited;
                        }))
                {
                    @this.OnAwaited(new AwaitedEventArgs(args: "Hello World!")
                    {
                        { "Key", "Value" }
                    });
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Demonstrates "awaiting the unawaitable" — validates awaited events raised from an <c>async void</c> handler.
        /// </summary>
        /// <remarks>
        /// The mock button runs in <see cref="TestMode.Asynchronous"/>.  
        /// <see cref="MockClassUnderTest.ExecClick(object, EventArgs)"/> starts a background task,
        /// awaits a short delay, then raises <see cref="AwaitedEventArgs"/> on a thread-pool thread.  
        /// Confirms that async timing does not alter Args or Caller classification and produces
        /// the same event signature as the synchronous baseline.
        /// </remarks>
        [TestMethod]
        public async Task Test_AwaitAsyncVoid()
        {
            string actual, expected;

            var mockUT = new MockClassUnderTest { TestMode = TestMode.Asynchronous };
            var callbacks = new Dictionary<string, int>();
            var stopwatch = new Stopwatch();
            AwaitedEventArgs? currentEvent = null!;
            SemaphoreSlim awaiter = new SemaphoreSlim(0, 1);

            #region L o c a l F x 
            using var local = this.WithOnDispose(
                onInit: (sender, e) =>
                {
                    IVSoftware.Portable.Threading.Extensions.Awaited += localOnAwaited;
                },
                onDispose: (sender, e) =>
                {
                    IVSoftware.Portable.Threading.Extensions.Awaited -= localOnAwaited;
                    awaiter.Wait(0);
                    awaiter.Release();
                });
            void localOnAwaited(object? sender, AwaitedEventArgs e)
            {
                currentEvent = e;
                callbacks.Increment(e.Args.GetType().ToFormattedTypeName(FormattedTypeNameOptionFlag.UseShortTypeName) ?? TYPE_NAME_ERROR);
                switch (e.Caller)
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
            #endregion L o c a l F x

            foreach (var testResponse in Enum.GetValues<TestResponse>())
            {
                mockUT.TestResponse = testResponse; // Setup.
                mockUT.ButtonClickMe.PerformClick();
                await awaiter.WaitAsync();
                stopwatch.Stop();
                Assert.IsNotNull(currentEvent);
            }

            actual = JsonConvert.SerializeObject(callbacks, Formatting.Indented);
            actual.ToClipboardExpected();
            { }

            expected = @" 
{
  ""Dictionary<String, Object>"": 2,
  ""ExecClick"": 3,
  ""String"": 2,
  ""Hello World!"": 1
}"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting json serialization to match."
            );
        }


        /// <summary>
        /// Verifies awaited-event behavior when raised synchronously on the calling thread.
        /// </summary>
        /// <remarks>
        /// The mock button runs in <see cref="TestMode.Synchronous"/>.  
        /// <see cref="MockClassUnderTest.ExecClick(object, EventArgs)"/> invokes 
        /// <c>localExecClick()</c> directly, raising <see cref="AwaitedEventArgs"/> inline.  
        /// Establishes the deterministic reference signature used by the async counterpart.
        /// </remarks>
        [TestMethod]
        public void Test_SynchronousEventCounting()
        {
            string actual, expected;

            var mockUT = new MockClassUnderTest { TestMode = TestMode.Synchronous };
            var callbacks = new Dictionary<string, int>();
            var stopwatch = new Stopwatch();
            AwaitedEventArgs? currentEvent = null!;

            #region L o c a l F x 
            using var local = this.WithOnDispose(
                onInit: (sender, e) =>
                    {
                        IVSoftware.Portable.Threading.Extensions.Awaited += localOnAwaited;
                    },
                onDispose: (sender, e) =>
                    {
                        IVSoftware.Portable.Threading.Extensions.Awaited -= localOnAwaited;
                    });
            void localOnAwaited(object? sender, AwaitedEventArgs e)
            {
                currentEvent = e;
                callbacks.Increment(e.Args.GetType().ToFormattedTypeName(FormattedTypeNameOptionFlag.UseShortTypeName) ?? TYPE_NAME_ERROR);
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
            #endregion L o c a l F x


            foreach (var testResponse in Enum.GetValues<TestResponse>())
            {
                mockUT.TestResponse = testResponse; // Setup.
                mockUT.ButtonClickMe.PerformClick();
                stopwatch.Stop();
                Assert.IsNotNull(currentEvent);
            }

            actual = JsonConvert.SerializeObject(callbacks, Formatting.Indented);
            actual.ToClipboardExpected();
            { }
            expected = @" 
{
  ""Dictionary<String, Object>"": 2,
  ""ExecClick"": 3,
  ""String"": 2,
  ""Hello World!"": 1
}"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting json serialization to match."
            );
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
                            // [Careful] 
                            // These are string consts. To wir:
                            // HELLO_WORLD = "Hello World!",
                            // As of 251115 this should be routed as an Arg not a Caller
                            this.OnAwaited(new AwaitedEventArgs(HELLO_WORLD));
                            break;
                        case TestResponse.HelloWorldArgs:
                            // [Careful] ibid.
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
