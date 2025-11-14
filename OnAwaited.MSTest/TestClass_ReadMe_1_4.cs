using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.Threading;
using System.Diagnostics;

namespace OnAwaited.MSTest;

[TestClass]
public class TestClass_ReadMe_1_4
{
    [TestMethod]
    public async Task Test_UnawaitableBefore()
    {
        string actual = string.Empty;
        await this.RunOnSTAThread(async () =>
        {
            Random rando = new Random(); // An unseeded random.
            await Task.Delay(TimeSpan.FromSeconds(0.5 + rando.NextDouble()));
#if false
            await Task.CompletedTask;

            System.Windows.Forms.Button btn = new();
            _ = btn.Handle;
            btn.Click += localOnButtonClicked;

            // This is an EventHandler delegate, and returning Task is not an option.
            async void localOnButtonClicked(object? sender, EventArgs e)
            {
                var delay = 0.5 + rando.NextDouble();
                Debug.WriteLine($"{delay}:F2");
                // We can only estimate how long this will take.
                actual = "Clicked!";
            }
            btn.PerformClick();

#endif
        });
    }

    [TestMethod]
    public async Task Test_AwaitableDelay()
    {
        await this.RunOnSTAThread(async () => await Task.CompletedTask);
        { }
    }

    [TestMethod]
    public async Task Test_Before()
    {
        string actual = string.Empty;
        var stopwatch = Stopwatch.StartNew();
        await this.RunOnSTAThread(async () =>
        {
            System.Windows.Forms.Button btn = new();
            _ = btn.Handle;
            btn.Click += localOnButtonClicked;

            // REAL handler don't have a Task return
            async void localOnButtonClicked(object? sender, EventArgs e)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                actual = "Clicked!";
            }
            btn.PerformClick();
            await Task.CompletedTask;
        });

        // Option 1:
        // - Guess how long to wait.
        // - Put in a 'magic delay'
        // - Wait (for too long or not long enough) and hope.

        Assert.AreNotEqual(
            "Clicked!", 
            actual,
            $"Unfortunately these will NEVER be equal without some kind of 'magic delay' here.");
    }

    [TestMethod]
    public async Task Test_After()
    {
        string actual = string.Empty;
        SemaphoreSlim awaiter = new SemaphoreSlim(0, 1);

        #region L o c a l F x 
        void localOnAwaited(object? sender, AwaitedEventArgs e)
        {
            switch (e.Caller)
            {
                case nameof(Test_After):
                    awaiter.Release();
                    break;
            }
        }
        #endregion L o c a l F x

        using (this.WithOnDispose(
            onInit: (sender, e) =>
                {
                    IVSoftware.Portable.Threading.Extensions.Awaited += localOnAwaited;
                },
            onDispose: (sender, e) =>
                {
                    IVSoftware.Portable.Threading.Extensions.Awaited -= localOnAwaited;
                }))
        {
            await this.RunOnSTAThread(async () =>
            {
                System.Windows.Forms.Button btn = new();
                _ = btn.Handle;
                btn.Click += localOnButtonClicked;

                // REAL handler don't have a Task return
                async void localOnButtonClicked(object? sender, EventArgs e)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    actual = "Clicked!";
                    this.OnAwaited();
                }
                btn.PerformClick();
            });
        }

        await awaiter.WaitAsync();


        Assert.AreEqual(
            "Clicked!",
            actual,
            $"The semaphore slim is now awaiting the next Awaited event.");
    }
}

static class STAExtensions
{
    private class SilentRunner : Form
    {
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
            if(!IsHandleCreated)
            {
                _ = Handle;
            }
        }
    }
    public static Task RunOnSTAThread(this object _, Func<Task> action)
    {
        var mainWnd = new SilentRunner();
        mainWnd.HandleCreated += async (sender, e) =>
        {
            mainWnd.BeginInvoke(async () =>
            {
                await action();
                mainWnd.Close();
            });
        };
        var _tcs = new TaskCompletionSource ();
        var thread = new Thread(() =>
        {
            try
            {
                System.Windows.Forms.Application.Run(mainWnd);
                mainWnd.Dispose();
                _tcs.SetResult();
            }
            catch (Exception ex)
            {
                _tcs.SetException(ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return _tcs.Task;
    }
}
