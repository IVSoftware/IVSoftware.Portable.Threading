This package addresses a need that is crucial and common in a test (e.g. MSTest) environment where evaluating asynchronous UI interactions in something like a WPF or Winforms app is often complex and fraught with challenges. These tests might involve stimuli that are either test-driven or interactively user-driven. They may also require monitoring for changes in typically synchronous methods like OnPropertyChanged, or tracking updates in a continuously running polling loop.

I saw the question recently worded as _How can `async void` methods be tested?_ Or, to put a finer point on it, how can we await the unawaitable?

This is a tried and true approach that I've used extensively for testing my UI application in "just the basic" MSTest environment. My early attempts always seemed to pile additional timing uncertainties on top of the ones I was trying to test. This solution is dirt simple. I made a helper class that exposes an extension for `object` that fires a custom static event automatically tagged with the caller method name. There's also an args property that can carry a Dictionary<string, object> or a json payload (for example), and this is to provide context to the MSTest method that is listening to it, plus you have the sender object itself. Taken together, this provides a rich context in which to evaluate this moment in the app's asynchronous life. 

One of the simplest examples I can think of would be the ability to await an expected property change from within the synchronous `System.Windows.Window.OnPropertyChanged()` method in the app under test. You can do this by adding one line to call the `OnAwaited` extension:

#### App under test

```
// <PackageReference Include="IVSoftware.Portable.Threading" Version="*" />
// using IVSoftware.Portable.Threading;
// The synchronous method you want to observe but can't (or shouldn't) call directly. 
protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
{
    base.OnPropertyChanged(e);
    this.OnAwaited(new AwaitedEventArgs(args: new Dictionary<string, object>
    {
        { nameof(DependencyPropertyChangedEventArgs), e }
    }));
}
```

#### MSTest

In the test method, the static `Awaited` event is subscribed just for the duration of this particular test. Once its raised, it can be inspected to see who the sending object is, to see what method actually called it, and to examine whatever rich treasures have been pushed into the args object for detailed analysis.

```
// using static IVSoftware.Portable.Threading.Extensions;
[TestMethod]
public async Task MyTest_ReturnsData()
{
    SemaphoreSlim awaiter = new SemaphoreSlim(0, 1);
    string? actual = null;
    try
    {
        Awaited += localOnAwaited;

        WPFAppWindow?.CallSomeAsyncVoidMethod();
        // Wait for it to have a deterministic
        // effect after a non-deteministic time.
        Assert.IsTrue(
            condition: await awaiter.WaitAsync(timeout: TimeSpan.FromSeconds(10)),
            "Timed out waiting for property change.");
        Assert.AreEqual(
            expected: "MyExpectedValue",
            actual: actual,
            $"An unexpected value was detected in {nameof(WPFAppWindow)}.OnPropertyChanged().");
    }
    finally
    {
        // CRITICAL to unconditionally unsubscribe
        // from the static method when done.
        Awaited -= localOnAwaited;
    }
    #region L o c a l M e t h o d s
    void localOnAwaited(object? sender, AwaitedEventArgs e)
    {
                object? o;
                switch (e.Caller)
                {
                    // Very common scenario of listening for a
                    // property to change after a UI stimulus.
                    case "OnPropertyChanged":
                        if (e.Args is Dictionary<string, object> args)
                        {
                            if (args.TryGetValue(nameof(DependencyPropertyChangedEventArgs), out o) &&
                            o is DependencyPropertyChangedEventArgs wpfPropertyChanged)
                            {
                                switch (wpfPropertyChanged.Property.Name)
                                {
                                    case "MyTargetProperty":
                                        // The property we've been listening to has changed.
                                        actual = $"{wpfPropertyChanged.NewValue}";
                                        awaiter.Release();
                                        break;
                                }
                            }
                        }
                        break;
                }
    }
    #endregion L o c a l M e t h o d s
}
```
