This NuGet package provides a minimalist solution Design for Testability (DFT). It provides the lightweight `OnAwaited(...)`extension for `object`. The core concept is that calls to `OnAwaited(..)` only occur when there is a listener for the `AwaitedEvent`. In the absence of any listener (e.g. in a production Release version) this hook does nothing. As such benign entities, these conditional calls are intended to be sprinkled throughout the application under test.

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

**First,** listeners for the `Awaited` event are ephemeral, typically existing only for the duration of a single test.

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

**Second,** the `sender` argument is `this`, which means that any public properties of the invoking class are available to MSTest for evaluation.

**Third,** supporting information, for example private data fields or threading syncrhonization contexts, can be transmitted by populating an `AwaitedEventArgs` instance. One way to initialize the dictionary capability of this event args class is to populate it using a collection initializer in the same way as any other dictionary could be:

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

**Finally,** even in a parallel test execution environment, the `AwaitedEventArgs` can be filtered for sender, caller, and identifying values in its dictionary payload to determine whether _this event_ is the specific _event we're awaiting_ (or not) and if so, release the awaited and proceed with the test.

___

**Awaiting the Unawaitable**

This concept pertains to the testing of asynchronous methods that do not return a `Task`, making them difficult to await using conventional asynchronous testing strategies. This package provides mechanisms to effectively handle and test these scenarios.

### Usage Example

Below is a simplified class that demonstrates how to use the NuGet package to test asynchronous and synchronous event-driven UI interactions:

```csharp
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
        if (TestMode == TestMode.Asynchronous)
        {
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1.1));
                this.OnAwaited();
            });
        }
        else
        {
            this.OnAwaited();
        }
    }
}

class MockButton
{
    public event EventHandler? Clicked;
    public void PerformClick() => Clicked?.Invoke(this, EventArgs.Empty);
}
```

### Test Implementation

In MSTest, a named local function is declared to safely subscribe and unsubscribe to the `Awaited` event for the duration of the test.

```csharp
using IVSoftware.Portable.Threading;
using static IVSoftware.Portable.Threading.Extensions;

[TestMethod]
public async Task AwaitAsynchronousVoid()
{
    var mockUT = new MockClassUnderTest { TestMode = TestMode.Asynchronous };
    var callbacks = new Dictionary<string, int>();
    var awaiter = new SemaphoreSlim(1, 1);

    void localOnAwaited(object? sender, AwaitedEventArgs e)
    {
        callbacks.Increment(e.Caller);
        awaiter.Release(); // Ensure to release after handling to continue the test execution.
    }

    try
    {
        Awaited += localOnAwaited;
        mockUT.ButtonClickMe.PerformClick();
        await awaiter.WaitAsync(TimeSpan.FromSeconds(2)); // Adjust as needed based on expected delay
        Assert.IsTrue(callbacks.ContainsKey("ExecClick"), "ExecClick was not called.");
    }
    finally
    {
        Awaited -= localOnAwaited; // Unsubscribe using the same instance of the delegate.
    }
}
```

_Where `Increment(key)` is an Extension Method for Dictionary<string, object>_

```csharp
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

This section show

