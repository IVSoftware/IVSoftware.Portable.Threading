
## IVSoftware.Portable.Threading [[GitHub](https://github.com/IVSoftware/IVSoftware.Portable.Threading.git)]

This package began as a minimalist Design for Testability (DFT) tool: an extension on `object` that raises a static `Awaited` event whenever an awaited continuation completes. It was originally meant to help "await the unawaitable," but it proved flexible enough to drop anywhere you might ever want a discreet test hook. One call, no ceremony, no dependencies.
___

### Awaiting the Unawaitable

The original use case comes from a long-standing friction in .NET: event handlers cannot return `Task`. This makes asynchronous work inside UI events fundamentally opaque, and unit tests tend to fall back on timing guesses and magic delays.

___

#### Before - Magic Delays and Guesswork

This version is tame only because we can at least estimate the worst-case delay. In a real application you are rarely this lucky; you might be waiting on a server, network hop, or database call, and the duration is unknowable in advance.


```csharp
[TestMethod]
public async Task Test_UnawaitableBefore()
{
    string actual = string.Empty;
    Random rando = new Random(); // An unseeded random.

    System.Windows.Forms.Button btn = new();
    btn.Click += localOnButtonClicked;

    // This is an EventHandler delegate, and returning Task is not an option.
    async void localOnButtonClicked(object? sender, EventArgs e)
    {
        // We can only estimate how long this will take.
        await Task.Delay(TimeSpan.FromSeconds(0.5 + rando.NextDouble()));
        actual = "Clicked!";
    }

    btn.PerformClick();

    // So we put in a magic delay and hope.
    await Task.Delay(TimeSpan.FromSeconds(1));
    Assert.AreEqual("Clicked", actual); // Expected to fail sporadically because timing is guesswork.
}
```
___

#### After - Deterministic Ephemeral Hook on Awaited






