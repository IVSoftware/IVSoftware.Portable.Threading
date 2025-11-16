using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.Threading;
using IVSoftware.WinOS.MSTest.Extensions;
using IVSoftware.WinOS.MSTest.Extensions.STA;
using Newtonsoft.Json;
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

        [TestMethod]
        public async Task Test_UnawaitableScenario()
        {
            string actual, expected;

            var rando = new Random(10);
            var awaiter = new SemaphoreSlim(0, 1);
            var builder = new List<string>();
            var stopwatch = Stopwatch.StartNew();
            var tcs = new TaskCompletionSource();
            var eventCount = 0;

            #region L o c a l F x 
            using var local = this.WithOnDispose(
                onInit: (sender, e) => AwaitedEventArgs.Awaited += localOnAwaited,
                onDispose: (sender, e) => AwaitedEventArgs.Awaited -= localOnAwaited);
            void localOnAwaited(object? sender, AwaitedEventArgs e)
            {
                eventCount++;
                builder.Add($@"{((Control?)sender).Name} Sent {e[nameof(Stopwatch)]} Returned {stopwatch.Elapsed:mm\:ss\:ff}");
                if (eventCount == 5)
                {
                    tcs.SetResult();
                }
            }
            #endregion L o c a l F x

            // <PackageReference Include="IVSoftware.WinOS.MSTest.Extensions.STA" Version="1.0.0-alpha" />
            using var sta = new STARunner(isVisible: false);

            await sta.RunAsync(async () =>
            {
                var btnQueryCloud = new System.Windows.Forms.Button() { Name = "QueryCloud" };

                btnQueryCloud.Click += async (sender, e) =>
                {
                    // Simlulate an indeterminate cloud retrieval.
                    await Task.Delay(TimeSpan.FromSeconds(5 + 0.5 + (2 * rando.NextDouble())));
                    sender.OnAwaited(new AwaitedEventArgs
                    {
                        { nameof(Stopwatch), $@"{stopwatch.Elapsed:mm\:ss\.ff}" }
                    });
                };
                for (int i = 0; i < 5; i++)
                {
                    // Space these out a little but run concurrently.
                    await Task.Delay(TimeSpan.FromSeconds(0.5));
                    btnQueryCloud.PerformClick();
                }
            });
            await tcs.Task;


            actual = string.Join(Environment.NewLine, builder);


            actual.ToClipboardExpected();
            { }
            // Approximate
            expected = @" 
QueryCloud Sent 00:07.97 Returned 00:07:98
QueryCloud Sent 00:08.09 Returned 00:08:09
QueryCloud Sent 00:08.62 Returned 00:08:62
QueryCloud Sent 00:09.00 Returned 00:09:00
QueryCloud Sent 00:09.57 Returned 00:09:57"
            ;
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
