using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.Threading;
using IVSoftware.WinOS.MSTest.Extensions;
using IVSoftware.WinOS.MSTest.Extensions.STA;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using OnAwaited.MSTest.WinTest;
using System.Diagnostics;
using System.Net.Http;
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
                Debug.WriteLine($"R{stopwatch.Elapsed}");
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
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    //await Task.Delay(TimeSpan.FromSeconds(5 + 0.5 + (2 * rando.NextDouble())));
                    sender.OnAwaited(new AwaitedEventArgs
                    {
                        { nameof(Stopwatch), $@"{stopwatch.Elapsed:mm\:ss\.ff}" }
                    });
                };
                for (int i = 0; i < 5; i++)
                {
                    // Space these out a little but run concurrently.
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    Debug.WriteLine(stopwatch.Elapsed);
                    btnQueryCloud.PerformClick();
                }
            });
            await tcs.Task;


            actual = string.Join(Environment.NewLine, builder);


            actual.ToClipboardExpected();
            { }
            expected = @" 
QueryCloud Sent 00:06.09 Returned 00:06:09
QueryCloud Sent 00:07.10 Returned 00:07:11
QueryCloud Sent 00:08.11 Returned 00:08:12
QueryCloud Sent 00:09.12 Returned 00:09:13
QueryCloud Sent 00:10.13 Returned 00:10:14"
            ;
        }

        [TestMethod]
        public async Task Test_CatFact()
        {
            string actual = null!, expected = null!, armed = "OnHandleCreated";
            var builder = new List<string>();
            var awaiter = new SemaphoreSlim(0, 1); // A blocked semaphore
            System.Windows.Forms.Button? btn = null;

            // Subscribe to AwaitedEventArgs.Awaited for the duration of this test.
            using var local = this.WithOnDispose(
                onInit: (sender, e) => AwaitedEventArgs.Awaited += localOnAwaited,
                onDispose: (sender, e) => AwaitedEventArgs.Awaited -= localOnAwaited);

            void localOnAwaited(object? sender, AwaitedEventArgs e)
            {
                switch (e.Caller)
                {
                    case "OnHandleCreated" when armed == "OnHandleCreated":
                        btn = sender as System.Windows.Forms.Button;
                        builder.Add($"OnHandleCreated Button={btn?.Text}");
                        awaiter.Release();
                        break;
                    case "OnTextChanged" when armed == "OnTextChanged":
                        actual = e["Text"] as string ?? string.Empty;
                        builder.Add(actual);
                        awaiter.Release();
                        break;
                }
            }

            // <PackageReference Include="IVSoftware.WinOS.MSTest.Extensions.STA" Version="1.0.0-alpha" />
            // Make a disposable STA thread to run the form
            using var sta = this.CreateSTAThread<CatFactForm>(isVisible: true);

            await sta.RunAsync(async () =>
            {
                // Wait for OnHandleCreated
                await awaiter.WaitAsync();

                armed = "OnTextChanged";
                btn?.PerformClick();

                // Wait for loading message if present.
                await awaiter.WaitAsync();
                if(actual.StartsWith("Loading"))
                {
                    // Wait for fact or error.
                    await awaiter.WaitAsync();
                }
            });

            // Test result

            actual = string.Join(Environment.NewLine, builder);

            actual.ToClipboardAssert("Expecting builder content to match.");
            { }
            expected = @" 
OnHandleCreated Button=Cat Fact
Loading...
A cats field of vision is about 185 degrees.";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder contains cat fact 0."
            );
        }
    }
    namespace WinTest
    {
        using System.Text.Json;
        using Button = System.Windows.Forms.Button;
        public partial class CatFactForm : Form
        {
            public CatFactForm()
            {
                InitializeComponent();
                
                // DFT: Provide button handle when ready in order to PerformClick on it.
                HandleCreated +=(sender, e)
                    => btnCatFact.OnAwaited(caller: nameof(OnHandleCreated));

                // DFT: Notify when text changes on button by adding new text to event dictinary.
                txtFact.TextChanged += (sender, e) 
                    => txtFact.OnAwaited(new AwaitedEventArgs(caller: nameof(OnTextChanged)){ { nameof(Text), txtFact.Text} });
            }

            private void InitializeComponent()
            {
                btnCatFact = new Button { Text = "Cat Fact", Left = 10, Top = 10, Width = 100 };
                txtFact = new TextBox { Left = 10, Top = 50, Width = 360, Height = 120, Multiline = true };
                btnCatFact.Click += btnCatFact_Click;

                Controls.Add(btnCatFact);
                Controls.Add(txtFact);

                StartPosition = FormStartPosition.CenterScreen;
            }

            private async void btnCatFact_Click(object? sender, EventArgs e)
            {
                txtFact.Text = "Loading...";
                using var http = new HttpClient();
                var json = await http.GetStringAsync("https://meowfacts.herokuapp.com/?id=0");                    
                var fact = JsonDocument.Parse(json).RootElement.GetProperty("data")[0].GetString();
                txtFact.Text = fact ?? "(no fact returned)";
            }
            private Button btnCatFact;
            private TextBox txtFact;
        }
    }
}
