This NuGet package offers a minimalist Design for Testability (DFT) solution. It features the lightweight `OnAwaited(...)` extension for `object`. A key feature of this design is that calls to `OnAwaited(..)` only activate when there are active subscribers to the static `AwaitedEvent`. These subscribers are typically ephemeral, existing ad hoc only for the duration of a single test. In environments like production releases where no subscribers are present, these hooks effectively do nothing, ensuring they remain benign. This allows for these conditional calls to be sprinkled throughout the application under test without impacting performance or behavior in production.

```csharp
using IVSoftware.Portable.Threading;

public void MethodUnderTest()
{
    // Raises `Awaited` with sender=this and e.Caller="MethodUnderTest".
    this.OnAwaited();
}
```
___

The power of this deceptively simple approach stems from features of the design:

**First:** Listeners for the `Awaited` event are ephemeral, typically existing only for the duration of a single test.

```csharp
using IVSoftware.Portable.Threading;
using static IVSoftware.Portable.Threading.Extensions;

[TestMethod]
public async Task AwaitAsyncVoid()
{
    var awaiter = new SemaphoreSlim(1, 1);
    try
    {
        Awaited += localOnAwaited;

        // The "Fire and Forget" stumulus of the app under test goes here.

        await awaiter.WaitAsync(TimeSpan.FromSeconds(2)); // Adjust as needed based on expected delay
    }
    finally
    {
        Awaited -= localOnAwaited; // Unsubscribe using the same instance of the delegate.
    }

    void localOnAwaited(object? sender, AwaitedEventArgs e)
    {
        // If conditions are met, the awaiter is released and the
        // unit test is evaluated by inspecting the event payload. 
        awaiter.Release(); // Ensure to release after handling to continue the test execution.
    }
}
```

**Second:** The `sender` argument is `this`, which means that any public properties of the invoking class are available to MSTest for evaluation.

**Third:** Supporting information, for example private data fields or threading syncrhonization contexts, can be transmitted by populating an `AwaitedEventArgs` instance. One way to initialize the dictionary capability of this event args class is to populate it using a collection initializer in the same way as any other dictionary could be:

##### Using String Keys

This example demonstrates how to populate `AwaitedEventArgs` with string keys:

```csharp
using IVSoftware.Portable.Threading;

public void MethodUnderTest()
{
    this.OnAwaited(new AwaitedEventArgs
    {
        {"Key1", "Value1"},
        {"Key2", 100}
    });
}
```
___
##### Using a User-Defined Enumeration as a Standard Key

Enumeration values used as keys will be converted to string keys. This approach enhances code readability and consistency:

```csharp
using IVSoftware.Portable.Threading;

public void MethodUnderTest()
{
    this.OnAwaited(new AwaitedEventArgs
    {
        {"StdKey.Key1", "Value1"},
        {"Std.Key2", 100}
    });
}
```

**Finally:** Even in a parallel test execution environment, the `AwaitedEventArgs` can be filtered for sender, caller, and identifying values in its dictionary payload to determine whether _this event_ is the specific _event we're awaiting_ (or not) and if so, release the awaited and proceed with the test.

___

_COMPLETE REFERENCE EXAMPLES ARE SHOWN BELOW, AFTER THE QUICK-START SECTION. [Skip to Examples](#examples-for-reference)_
___

**Quick Start Guide for AwaitedEventArgs**

This guide offers a concise overview of how to effectively utilize `AwaitedEventArgs` in your projects. When invoking the static `OnAwaited()` method without specific arguments, an instance of `AwaitedEventArgs` is automatically created. This instance captures the calling method's name, which, along with the sender argument of the event, facilitates preliminary filtering in the `localOnAwaited` handler used in MSTest scenarios.

```csharp
using IVSoftware.Portable.Threading;

public void MethodUnderTest()
{
    // Raises `Awaited` with sender=this and e.Caller="MethodUnderTest".
    this.OnAwaited();
}
```
___

#### Customizing Event Data

You can configure the AwaitedEventArgs by using the collection initializer syntax, just as you would with any dictionary. Populate these key-value pairs using either string keys or enumeration values.
___
##### Using String Keys

This example demonstrates how to populate AwaitedEventArgs with string keys:

```csharp
using IVSoftware.Portable.Threading;

public void MethodUnderTest()
{
    this.OnAwaited(new AwaitedEventArgs
    {
        {"Key1", "Value1"},
        {"Key2", 100}
    });
}
```
___
##### Using a User-Defined Enumeration as a Standard Key

Enumeration values used as keys will be converted to string keys. This approach enhances code readability and consistency:

```csharp
using IVSoftware.Portable.Threading;

public void MethodUnderTest()
{
    this.OnAwaited(new AwaitedEventArgs
    {
        {"StdKey.Key1", "Value1"},
        {"Std.Key2", 100}
    });
}
```
___

##### Setting Args as an Independent Object Instance

When the args parameter is explicitly set, e.Args becomes an independent object instance that can be used either in place of or alongside the dictionary. This feature offers a convenient shortcut, potentially eliminating the need for setting or retrieving dictionary values altogether.

```csharp
using IVSoftware.Portable.Threading;

public void MethodUnderTest()
{
    this.OnAwaited(new AwaitedEventArgs(args: SelectedItems);
}
```
___
**Two-Way Interaction with Dictionary Values**

The dictionary in AwaitedEventArgs supports two-way interactions, crucial for dynamic test setups. Here’s how it typically works in a testing scenario:

1. **Initial Notification:** The method under test first fires an OnAwaited event with default parameters to notify MSTest of its initialization:

```csharp
public void MethodUnderTest()
{
    this.OnAwaited(new AwaitedEventArgs());
}
```

2. **Test Context Adjustment:** Upon receiving the initial notification, MSTest may adjust the test context by setting values such as StdKey.RunContext to RunContext.Test and potentially supplying a custom filter parameter. To ensure that the localOnAwaited function responds only when the caller is specifically "MethodUnderTest", incorporate a check for the caller within the function. Here's how you can refine your function to include this selective response:

```csharp
void localOnAwaited(object sender, AwaitedEventArgs e)
{
    switch(e.Caller)
    {
        case nameof(ClassUnderTest.MethodUnderTest):
            e.Add(StdKey.RunContext, RunContext.Test);
            e.Add(StdKey.SelectedItemsFilterParameter, "SpecificFilter");
            break;
        default:
            // Handle other cases or do nothing
            break;
    }
}
```
3. **Responding to Adjustments:** Back in the method under test, after the initial event, it may check these settings and apply the custom filter parameter to refine its operations before pushing the results back onto the dictionary stack or proceeding with further logic:

```csharp
// Continuing within the MethodUnderTest
if (e.ContainsKey(StdKey.RunContext) && e[StdKey.RunContext] == RunContext.Test)
{
    // Apply filter parameter if provided
    if (e.ContainsKey(StdKey.SelectedItemsFilterParameter))
    {
        var filter = e[StdKey.SelectedItemsFilterParameter];
        // Apply filter logic here
    }

    // Potentially fire another event or continue with modified behavior
    this.OnAwaited(new AwaitedEventArgs { { "FilteredResults", FilteredItems } });
}
```
___

### Addressing the Suitability of AwaitedEventArgs for Parallel Testing

**Parallel testing** in modern software development requires robust and thread-safe components capable of handling multiple operations concurrently. `AwaitedEventArgs`, integral to the NuGet package, is expressly designed for such environments. It features a flexible architecture that supports the inclusion of thread-specific data and unique identifiers like GUIDs alongside standard `Caller` and `Sender` information. This design allows for a clear and isolated context for each event, crucial for accurate and reliable parallel testing.

#### Key Benefits:

- **Context-Rich Events**: Each `AwaitedEventArgs` instance can encapsulate detailed execution contexts, including thread IDs or GUIDs, to uniquely identify and trace the source and state of each event. This makes it easier to debug and analyze test results in a parallel execution scenario.

- **Concurrency-Optimized**: By allowing testers to attach specific, thread-bound data to events, `AwaitedEventArgs` helps maintain data integrity and prevent state bleed across concurrent tests. This is essential for achieving accurate and deterministic test outcomes in multithreaded applications.

- **Skillful Implementation**: Users engaged in parallel testing are often well-versed in the complexities of such environments. `AwaitedEventArgs` leverages this expertise by offering a flexible yet structured way to manage event data, aligning with advanced testing practices that require meticulous context management and thread safety.

#### Usage Recommendations:

To maximize the benefits of `AwaitedEventArgs` in parallel testing:
- **Incorporate Unique Identifiers**: Enhance traceability and isolation by including unique identifiers for each test execution within the event arguments.
- **Manage Subscriptions Carefully**: Ensure that event subscriptions and unsubscriptions are handled in a thread-safe manner to avoid cross-test interference.
- **Employ Proper Synchronization**: Utilize appropriate synchronization techniques when accessing shared resources from event handlers to prevent race conditions.

`AwaitedEventArgs` is not just compatible with parallel testing—it is optimized for it, providing a solid foundation for building reliable, scalable, and effective test suites.

___

## Examples for Reference

The examples below cover two common use cases.

1. **Awaiting the Unawaitable** shows how to use a test hook to await an `async void` method for testing.
2. **Synchonous event counting** shows a non-awaited evaluation of expected received events when the timing is deterministic.

___

#### Mock Class Under Test

```
[TestClass]
public sealed class TestClass
{
    const string 
        EXEC = "ExecClick",
        ERROR = "ERROR", 
        HELLO_WORLD = "Hello World!",
        TYPE_NAME_ERROR = "UNEXPECTED: Type Name Error.";
        
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
```
___
**Awaiting the Unawaitable**

This concept pertains to the testing of asynchronous methods that do not return a `Task`, making them difficult to await using conventional asynchronous testing strategies. 


```
/// <summary>
/// This test is a demonstration of "awaiting the unawaitable" async void.
/// </summary>
[TestMethod]
public async Task AwaitAsyncVoid()
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
```
___

**Synchronous event counting**

This concept pertains to discrete event counting, for example the number of times a clling method has been invoked in a given test flow. 

```
/// <summary>
/// This test is a demonstration of counting synchronous events.
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
        Awaited += localOnAwaited;

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
        Awaited -= localOnAwaited;
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
```

___

**Utility Dictionary Incrementing Extension Method**

Safely increments a key, whether it already exists or not.

```
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
```

