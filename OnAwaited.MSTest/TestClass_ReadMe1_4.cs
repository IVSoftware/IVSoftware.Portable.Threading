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
            // Make a disposable STA thread to run the form
            using var sta = this.CreateSTAThread<JsonApiViewer>(isVisible: true);

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
                    // Dwell long enough to view result
                    await Task.Delay(TimeSpan.FromSeconds(2.5));
                }
            });

            // Test result

            actual = string.Join(Environment.NewLine, builder);

            actual.ToClipboardAssert("Expecting builder content to match.");
            { }
            expected = @" 
OnHandleCreated Button=API Query
Loading...
delectus aut autem";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting fake JSON retrieved from real API."
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
                HandleCreated +=(sender, e)
                    => btnApiQuery.OnAwaited(caller: nameof(OnHandleCreated));

                // DFT: Notify when text changes on button by adding new text to event dictionary.
                txtFact.TextChanged += (sender, e) 
                    => txtFact.OnAwaited(new AwaitedEventArgs(caller: nameof(OnTextChanged)){ { nameof(Text), txtFact.Text} });
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
