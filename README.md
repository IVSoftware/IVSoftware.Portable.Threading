
## IVSoftware.Portable.Threading [[GitHub](https://github.com/IVSoftware/IVSoftware.Portable.Threading.git)]

This NuGet package offers a minimalist Design for Testability (DFT) solution. Here’s how it works: sprinkle this expression - like a pinch of dust - anywhere you anticipate needing to test for or monitor state.

```
this.OnAwaited(); // The no-frills edition: raise a static event from any object.
```

And what does this do? Simply stated, it raises a static event that (by default) has no subscribers. If that sounds unremarkable, that's fine - we'll get to why it matters. For now, two takeaways:

1. The overhead is next to nothing.  
2. You already know everything required to use it.

---

### When and How to Subscribe to the Event

The `AwaitedEventArgs.Awaited` event is meant to be used for ephemeral subscriptions in limited blocks. Here's a canonical example showing a `using` block that manages the subscription lifetime.

```
[TestMethod]
public void Test_Awaited101()
{
    // Handler
    void localOnAwaited(object? sender, AwaitedEventArgs e)
    {
        Assert.AreEqual(
            actual: e.Caller, 
            expected: nameof(Test_Awaited101),
            message: $"Expecting the calling method to be identified 'for free'.");

        Assert.ReferenceEquals(objA: this, objB: sender);
    }

    // <PackageReference Include="IVSoftware.Portable.Disposable" Version="2.0.0" />
    using (this.WithOnDispose(
        onInit: (sender, e) => AwaitedEventArgs.Awaited += localOnAwaited,
        onDispose: (sender, e) => AwaitedEventArgs.Awaited -= localOnAwaited))
    {
        // You can do this anywhere. In this case, someone *is* listening.
        this.OnAwaited(); 
    }
}
```

Recap of the call sequence:

1. Subscribes to the Awaited event for the duration of the using block.
2. Calls OnAwaited() with no special ceremony.
3. Receives a "ping" identified by the sender and calling method (i.e. [CallerMemberName]`).
4. Automatically unsubscribes when leaving scope.

___

### Awaiting the Unawaitable

Most .NET developers are familiar with a long-standing friction and pain point: event handlers that cannot return `Task`. This makes asynchronous work inside UI events fundamentally opaque, and unit tests tend to fall back on timing guesses and magic delays. Before this ever had a name, before it was a package, that was the spark. 

___
_In short: "Suppose a UI button is going to retrieve something from a server. How does one determine — rather than guess — when the result has actually returned?" That scenario, and many others like it, became the spark._ 
___





___








