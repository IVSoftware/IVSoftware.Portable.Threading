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
        public async Task Test_JsonPlaceholderAPI()
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
                        awaiter.SafeRelease();
                        break;
                    case "OnTextChanged" when armed == "OnTextChanged":
                        actual = e["Text"] as string ?? string.Empty;
                        builder.Add(actual);
                        awaiter.SafeRelease();
                        break;
                }
            }

            // <PackageReference Include="IVSoftware.WinOS.MSTest.Extensions.STA" Version="1.0.0-alpha" />
            // Make a disposable STA thread to run the form for the duration of this method.
            using var sta = this.CreateSTAThread<JsonApiViewer>(isVisible: true);

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

            // Test result

            actual = string.Join(Environment.NewLine, builder);

            actual.ToClipboardExpected();
            { }
            expected = @" 
OnHandleCreated Button=API Query
delectus aut autem";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting builder content to match."
            );
        }
    }
    namespace WinTest
    {
        using System.Text.Json;
        using Button = System.Windows.Forms.Button;
        public partial class JsonApiViewer : Form
        {
            public JsonApiViewer()
            {
                InitializeComponent();
                
                // DFT: Provide button handle when ready in order to PerformClick on it.
                HandleCreated +=(sender, e) => btnApiQuery.OnAwaited(caller: nameof(OnHandleCreated));
            }

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

                // DFT: Notify when text changes on button by adding new text to event dictionary.
                this.OnAwaited(new AwaitedEventArgs(caller: nameof(OnTextChanged)){ { nameof(Text), txtFact.Text} });
            }
            private Button btnApiQuery;
            private TextBox txtFact;
        }
#if false && SAVE

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
#endif
    }
}
