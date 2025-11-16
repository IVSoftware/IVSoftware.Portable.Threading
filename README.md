
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
        // You can do this anywhere. In this case, someone *is* listening (ephemerally).
        this.OnAwaited(); 
    }
}
```

Recap of the call sequence:

1. Subscribes to the Awaited event for the duration of the using block.
2. Calls OnAwaited() with no special ceremony.
3. Receives a "ping" identified by the sender and calling method (i.e. `[CallerMemberName]`).
4. Automatically unsubscribes when leaving scope.

___

### Awaiting the Unawaitable

Most .NET developers are familiar with a long-standing friction and pain point: event handlers that cannot return `Task`. This makes asynchronous work inside UI events fundamentally opaque, and unit tests tend to fall back on timing guesses and magic delays. Before this ever had a name, before it was a package, that was the spark. 

___
_In short: "Suppose a UI button is going to retrieve something from a server. How does one determine — rather than guess — when the result has actually returned?" That scenario, and many others like it, became the spark._ 
___

To demonstrate, let's make a real form with a real API call (not mocked). Now ask, what would it take to drive this UI in test and reliably evaluate the API response.

```
public partial class JsonApiViewer : Form
{
    public JsonApiViewer() => InitializeComponent();

    private void InitializeComponent()
    {
        Size = new System.Drawing.Size(500, 300);
        btnApiQuery = new Button { Text = "API Query", Left = 10, Top = 10, Width = 100 };
        txtFact = new TextBox { Left = 10, Top = 50, Width = ClientSize.Width-20, Height = 120, Multiline = true };
        btnApiQuery.Click += btnApiQuery_Click;
        Controls.Add(btnApiQuery);
        Controls.Add(txtFact);
        StartPosition = FormStartPosition.CenterScreen;
    }

    // https://jsonplaceholder.typicode.com/
    private async void btnApiQuery_Click(object? sender, EventArgs e)
    {
        txtFact.Text = "Loading...";
        using var http = new HttpClient();
        var json = await http.GetStringAsync("https://jsonplaceholder.typicode.com/todos/1");
        // Parse and show something interesting
        var doc = JsonDocument.Parse(json);
        txtFact.Text = doc.RootElement.GetProperty("title").GetString();
    }
    private Button btnApiQuery;
    private TextBox txtFact;
}
```
At first glance this looks straightforward: a simple async handler fetching JSON. But for a test trying to drive this form, there is no built-in signal that the awaited work has actually completed. The UI thread stays alive, the async state machine runs in the background, and the test is left to guess when the result is ready. This is exactly the visibility gap the `Awaited` signal was designed to close.

___

### Design for Test (DFT)

Now let's retrofit the same class for testability, explaining as we go.

1. "Let me know when the UI is available, and at the same time give me the Button handle."

For this two-in-one effect, the timing can trigger on the form's `HandleCreated` event. And if the button handle is used to call `OnAwaited()` then it will show up as the sender. This will allow us to call the button's native`PerformClick()` method.

```
    public JsonApiViewer()
    {
        InitializeComponent();
                
        // DFT: Provide button handle when ready in order to PerformClick on it.
        HandleCreated +=(sender, e) => btnApiQuery.OnAwaited(caller: nameof(OnHandleCreated));
    }
```

#### Setting up the Listener in MSTest

Remember, the `Awaited` event has no listeners, and doesn't need to have any in the class being tested. To set up an ephemeral listener, set up the basic MSTest method with the necessary scaffolding.

```
[TestMethod]
public async Task Test_JsonPlaceholderAPI()
{
    // A reusable completion source to await things
    var awaiter = new SemaphoreSlim(0, 1); // A blocked semaphore

    // But only "certain" things
    string armed = "OnHandleCreated";

    // A list we can use to track responses
    var builder = new List<string>();

    // The handle we need to obtain.
    System.Windows.Forms.Button? btn = null;
}
```
___

Then, like the first example, add an ephemeral event handler for the duration of the test.

```
    // Subscribe to AwaitedEventArgs.Awaited for the duration of this test.
    using var local = this.WithOnDispose(
        onInit: (sender, e) => AwaitedEventArgs.Awaited += localOnAwaited,
        onDispose: (sender, e) => AwaitedEventArgs.Awaited -= localOnAwaited);

```

Keep in mind that `Awaited` is a static event, and there's some chance that tests are running parallel. This means that the local handler will need to be selective. The "armed" filter provides a simple way to take action on an event in this method while ignoring it in a concurrent test, and it's already initialised to `"OnHandleCreated"`.

Now look at the `awaiter` semaphore, which is blocked in creation. If we were to `await awaiter.WaitAsync()` then it would sit forever. That's where the handler steps in, releasing the awaiter so that the test can advance to the next asynchronous phase.

```
    void localOnAwaited(object? sender, AwaitedEventArgs e)
    {
        switch (e.Caller)
        {
            case "OnHandleCreated" when armed == "OnHandleCreated":
                btn = sender as System.Windows.Forms.Button;
                builder.Add($"OnHandleCreated Button={btn?.Text}");
                awaiter.SafeRelease();
                break;
            case "OnTextChanged" when armed == "OnTextChanged":
                actual = e["Text"] as string ?? string.Empty;
                builder.Add(actual);
                awaiter.SafeRelease();
                break;
        }
    }
```

---
#### Running the `JsonApiViewer` Form in MSTest

This part requires an STA thread. How you obtain one doesn't matter; for this working example the thread comes from:


```
    // <PackageReference Include="IVSoftware.WinOS.MSTest.Extensions.STA" Version="1.0.0-alpha" />
    // Make a disposable STA thread to run the form for the duration of this method.
    using var sta = this.CreateSTAThread<JsonApiViewer>(isVisible: true);
```

To post work to this thread, call its `RunAsync(func)` method with any `Func<Task>`. In this test, the entire sequence will run inside a single invocation:

```
    // The local test
    await sta.RunAsync(async () =>
    {
        // Wait for OnHandleCreated
        await awaiter.WaitAsync();

        // YOU ARE HERE - BUT WHY ?
    }
```

At first glance, it seems odd that execution has already passed a semaphore we know began in a blocked state. The explanation lies in what has already occurred:

1. Calling `CreateSTAThread<JsonApiViewer>()` had the dual effect of spawning the thread _and_ showing the form.
2. Showing the form raised `HandleCreated`, so the first branch of `localOnAwaited` has already fired, releasing the semaphore.
3. In that same branch, the `sender` was captured as the button instance and is now available for use.

___

_This completes the DFT setup described as step 1._

___

#### DFT Final Setup

The second step will complete the setup and the test will be finalized.

2. "I need to await the 'unawaitable' handler that runs when I call `PerformClick()` on the handle received in Step 1."

The solution is to place an `OnAwaited()` call immediately after the awaited operation resumes. 

___

_This is where the name originated: a simple signal marking an awaited boundary inside a method that cannot return a `Task`. It turned out to be useful in many of other scenarios, but this was the first and most obvious case._
___

```
    private async void btnApiQuery_Click(object? sender, EventArgs e)
    {
        txtFact.Text = "Loading...";
        using var http = new HttpClient();
        var json = await http.GetStringAsync("https://jsonplaceholder.typicode.com/todos/1");
        // Parse and show something interesting
        var doc = JsonDocument.Parse(json);
        txtFact.Text = doc.RootElement.GetProperty("title").GetString();

        // DFT: Signal that await has completed in a method that returns no Task.
        this.OnAwaited(
            new AwaitedEventArgs(caller: nameof(OnTextChanged))
            {
                { nameof(Text), txtFact.Text} 
            });
    }
```

#### Test Method Final Setup

From here, the test can advance to the second awaited phase:

1. Arm the semaphore for `"OnTextChanged"`.
2. Virtually click the button (the same way a UI user would) by calling `PerformClick()`.
3. Await the local semaphore that _can_ be awaited, acting as a proxy for the handler that _can't_.


```
    await sta.RunAsync(async () =>
    {
        // Wait for OnHandleCreated
        await awaiter.WaitAsync();
        // Wait for API response with timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var stopwatch = Stopwatch.StartNew();

        try
        {
            armed = "OnTextChanged";
            btn?.PerformClick();
            await awaiter.WaitAsync(cts.Token); 
        } 
        catch (OperationCanceledException) {
            Assert.Fail("Expecting the API to respond within the alotted maximum time.");
        }
        stopwatch.Stop();

        Assert.IsTrue(
            stopwatch.Elapsed < TimeSpan.FromSeconds(1),
            $"We're expecting typical values ~0.2 seconds with deviation."
        );
    });
```


#### Test Method Local Handler Advanced

In the code above, recall that a string result is appended to the `builder` each time it was raised. 

What this means in terms of general strategy is that instead of trying to pick things apart - counting events and such - tests like these where the result is idempotent can do things like joining the builder of JSON serialize an object to obtain a complex limit.

```
    // Test result
    actual = string.Join(Environment.NewLine, builder);
    expected = @" 
OnHandleCreated Button=API Query
delectus aut autem";

    Assert.AreEqual(
        expected.NormalizeResult(),
        actual.NormalizeResult(),
        "Expecting to see evidence of exactly two specific events."
    );
```

The string "delectus aut autem" is a _lorem ipsum_ returned by the API, but how did it come to be attached to`e`? The key - literally - is that `AwaitedEventArgs` functions as a `Dictionary<string,object>()`.

The call site in the click handler used a collection initializer to populate this dictionary.

```
    this.OnAwaited(
        new AwaitedEventArgs(caller: nameof(OnTextChanged))
        {
            { nameof(Text), txtFact.Text} 
        });
```

In the handler for the `Awaited` event, it's read back like any string dictionary key.

```
    actual = e["Text"] as string ?? string.Empty;

```

___

### Recap of Basic Features

Test points can be placed anywhere in an application's control flow with essentially no cost. It does not matter if they are never used. When a subscription is active, each test point becomes an awaited boundary simply by releasing a synchronization primitive on the test side. The observability comes from the test harness, not from the object under test.

In effect, any such point becomes awaitable on demand.

#### Notes on Visibility

Whether these test points are compiled into production builds is entirely up to you. They can be left in place for telemetry or forensic purposes, or excluded in a release configuration. Either way, the need for test adapters, special visibility modifiers, or `InternalsVisibleTo` disappears. As the earlier example showed, a private `Button` and a private `async void` handler were both fully testable from the outside without altering their access levels.

___


### Advanced Applications: Beyond Simple Checkpoints

The earlier examples treated `AwaitedEventArgs` as a simple marker carrying a single value. In practice, it is backed by a `Dictionary<string,object>` that can carry far more expressive data. The values placed in it are not limited to diagnostics. They can participate directly in the workflow and coordinate with the test harness.

Common examples include:

- Structured context objects for a given flow.
- Delegates that act as callbacks into the test harness.
- Arbitrary object instances with no schema constraints.
- JSON or XML documents representing intermediate state.
- Synchronization primitives such as TaskCompletionSource or CountdownEvent.
- Enums used for finite state transitions.
- Loop or retry instructions.
- Reference counters or tokens similar to a DisposableHost pattern.

___

#### Not a One-Way Conversation

The dictionary is not restricted to one-way traffic. Test code can write to it just as freely as the producer can. This allows a test to supply values, inject behavior, or shape the next phase of a workflow before the handler returns. The pattern is not limited to UI event lifecycles. Any scenario with asynchronous boundaries, state transitions, or cooperative steps can use the same mechanism.

Whether the test is coordinating multiple phases of a token-ring workflow, exchanging structured state with an embedded device, or simply capturing a sequence of transitions for forensic comparison, the same principles apply. Test points remain optional and low-impact, and the test harness decides how much information to exchange at each boundary.

___

#### Many Effective Concurrency Management Paths

A common reaction is: "A static event? This cannot be thread-safe."  
The concern is valid. The reality is more interesting.

Consider a test runner with twenty tests executing in parallel, each with its own semaphore. The worst-case failure mode is a deadlock on the test side. The application does not carry any of those synchronization objects; nothing in production becomes entangled. That is already a practical win.

Now imagine calling `OnAwaited` twice in succession. Suppose the first event populates the dictionary with an enum for phase management (for example, `Phase.ContextRequest`), and all twenty tests are listening. Each test now has an opportunity to attach its own context object keyed in whatever manner the test requires.

Because the event is fired synchronously, the second event (such as `Phase.Run`) will reach all listeners while the dictionary is still present. Each of the twenty tests is now able to inspect not only its own context, but the others as well.

At that point:

- The class under test can synchronously examine the available contexts and populate them appropriately.
- Each test can disambiguate cross-test chatter by locating its own context in the shared pile.
- A test can even maintain a visited list to determine which contexts have responded and which have not.

Armed with that information, concurrency becomes something explicit and navigable rather than opaque. The mechanism does not enforce a concurrency model; it simply gives you a clear view of what is happening so you can decide how to handle it.



