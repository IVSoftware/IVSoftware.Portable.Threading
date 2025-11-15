using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.Threading;
using OnAwaited.MSTest.WinApplication;
using System.Runtime.CompilerServices;
using System.Windows;

namespace OnAwaited.MSTest
{
    [TestClass]
    public class TestClass_ReadMe_1_4
    {      

        class TstCon : TaskCompletionSource
        {
            public Form Runner { get; set; } = null!;
            public async Task Run()
            {
                string actual = string.Empty;
                var awaiter = new SemaphoreSlim(0, 1);

                #region L o c a l F x 
                void localOnAwaited(object? sender, AwaitedEventArgs e)
                {
                    switch (e.Caller)
                    {
                        case nameof(Run):
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

                    System.Windows.Forms.Button btn = new();
                    _ = btn.Handle;
                    btn.Click += localOnButtonClicked;

                    // This is an EventHandler delegate, and returning Task is not an option.
                    async void localOnButtonClicked(object? sender, EventArgs e)
                    {
                        // We can only estimate how long this will take.
                        actual = "Clicked!";
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        { }
                        this.OnAwaited();
                    }
                    btn.PerformClick();

                    await awaiter.WaitAsync();
                }
            }

            public TstCon()
            {
                thread = new Thread(() =>
                {
                    // Verify
                    Assert.IsTrue(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA);

                    // Log a message to the Unit Test
                    Console.WriteLine($"Thread State is {Thread.CurrentThread.GetApartmentState()}.");

                    Runner = new Form()
                    {
                        StartPosition = FormStartPosition.CenterScreen,
                    };
                    Runner.HandleCreated += async (sender, e) =>
                    {
                        //await Run();
                        //Runner.BeginInvoke(() =>
                        //{
                        //    Runner.Close();
                        //});
                    };
                    System.Windows.Forms.Application.Run(Runner);
                    SetResult();
                });
                // Just make sure to set the apartment state BEFORE starting the thread:
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();

            }
            Thread thread;

            internal async Task RunAsync(Func<Task> action)
            {
                var tcs = new TaskCompletionSource();
                Runner.BeginInvoke(async () =>
                {
                    await action();
                    tcs.SetResult();
                });
                await tcs.Task;
            }
        }

        [TestMethod]
        public async Task Test_TITGSA()
        {
            var tstcon = new TstCon();
            { }
            await Task.Delay(TimeSpan.FromSeconds(1));
            await tstcon.RunAsync(async()=> await Task.Delay(TimeSpan.FromSeconds(1)));
            tstcon.SetResult();
            await Task.Delay(TimeSpan.FromSeconds(1));

            await tstcon.Task;
        }

        [TestMethod]
        public async Task Test_TITGSB()
        {
            await new TstCon().Task;
            { }
        }

        [TestMethod]
        public async Task Test_HM()
        {
            TaskCompletionSource ss = new ();
            Thread thread = new Thread(() =>
            {
                // Verify
                Assert.IsTrue(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA);

                // Log a message to the Unit Test
                Console.WriteLine($"Thread State is {Thread.CurrentThread.GetApartmentState()}.");

                // I personally needed to test a Winforms UI and
                // the DragDrop COM wouldn't register without STA.
                var myUI = new System.Windows.Forms.Form();
                myUI.HandleCreated += async (sender, e) =>
                {
                    await AutomateMyUI(myUI);
                };
                System.Windows.Forms.Application.Run(myUI);

                // Signal that the [TestMethod] can return now.
                ss.SetResult(); //.Release();
            });
            // Just make sure to set the apartment state BEFORE starting the thread:
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            await ss.Task;

            Console.WriteLine("All done!");
            async Task AutomateMyUI(Form myUI)
            {

                string actual, expected;

                await Task.Delay(10);


                System.Windows.Forms.Button btn = new();
                _ = btn.Handle;
                btn.Click += localOnButtonClicked;

                // This is an EventHandler delegate, and returning Task is not an option.
                async void localOnButtonClicked(object? sender, EventArgs e)
                {
                    // We can only estimate how long this will take.
                    actual = "Clicked!";
                    await Task.Delay(10);
                    { }
                }
                btn.PerformClick();
                myUI.BeginInvoke(() =>
                {
                    myUI.Close();
                });
            }
        }


        [TestMethod]
        public async Task Test_UnawaitableBefore()
        {
#if false
            string actual = string.Empty;
            await this.RunOnSTAThread(async () =>
            {
                Random rando = new Random(); // An unseeded random.
                await Task.Delay(TimeSpan.FromSeconds(0.5 + rando.NextDouble()));
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
            });

#endif
        }

        [TestMethod]
        public async Task Test_AwaitableDelay()
        {
            using (var tstCon = new TstConPrev00())
            {
                _ = tstCon.Handle;
                await tstCon.Run(async () =>
                {
                    await Task.Delay(1);
                });
                tstCon.Close();
                await tstCon;
            }
        }

        [TestMethod]
        public async Task Test_Before()
        {
#if false
            string actual = string.Empty;
            var stopwatch = Stopwatch.StartNew();
            await this.RunOnSTAThread(out _, async () =>
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
#endif
        }

        [TestMethod]
        public async Task Test_After()
        {
#if false
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
                await this.RunOnSTAThread(out Form container, async () =>
                {
                    //System.Windows.Forms.Button btn = new();
                    //_ = btn.Handle;
                    //btn.Click += localOnButtonClicked;

                    //// REAL handler don't have a Task return
                    //async void localOnButtonClicked(object? sender, EventArgs e)
                    //{
                    //    await Task.Delay(TimeSpan.FromSeconds(1));
                    //    actual = "Clicked!";
                    //    this.OnAwaited();
                    //}
                    //btn.PerformClick();
                });
            }

            await awaiter.WaitAsync();


            Assert.AreEqual(
                "Clicked!",
                actual,
                $"The semaphore slim is now awaiting the next Awaited event.");
#endif
        }
    }

    namespace WinApplication
    {
        using Application = System.Windows.Forms.Application;
        public class TstConPrev00 : Form
        {
            public TstConPrev00()
            {
                HandleCreated += (sender, e) =>
                {
                    BeginInvoke(() =>
                    {
                        _tcsReady.SetResult();
                    });
                };
            }

            private readonly TaskCompletionSource
                _tcsReady = new(),
                _tcsDone = new();
            //protected override void SetVisibleCore(bool value)
            //{
            //    base.SetVisibleCore(value && false);
            //}
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
            }
            public TaskAwaiter GetAwaiter() => _tcsDone.Task.GetAwaiter();

            internal async Task Run(Func<Task> action)
            {
                while(!IsHandleCreated)
                {
                    await Task.Delay(100);
                }
                await action();
            }
        }
        static class STAExtensions
        {
            public static async Task GetTstCon(this TstConPrev00 mainWnd)
            {
                var tcs = new TaskCompletionSource();
                var thread = new Thread(() =>
                {
                    // Client must close the container when done
                    Application.Run(mainWnd);
                    tcs.SetResult();
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                await tcs.Task;
            }
        }
    }
}
