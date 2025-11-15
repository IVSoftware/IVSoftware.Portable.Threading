using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.Threading;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OnAwaited.MSTest
{
    [TestClass]
    public class TestClass_ReadMe1_4
    {
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

        [TestMethod, Ignore]
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
