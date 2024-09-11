using IVSoftware.Portable.Threading;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using wpf_app_test_async_void_methods;
using static IVSoftware.Portable.Threading.Extensions;
using Color = System.Drawing.Color;

namespace MSTest.Async.Demo
{
    [TestClass]
    public class UnitTestWPF
    {
        private static MainWindow? WPFAppWindow;
        private static SemaphoreSlim _uiKeepAlive = new SemaphoreSlim(0, 1);

        [ClassInitialize]
        public static async Task InitUI(TestContext context)
        {
            Thread thread = new Thread(async () =>
            {
                Application app = new Application();
                WPFAppWindow = new MainWindow
                {
                    Title = "Test Window",
                    Width = 400,
                    Height = 300,
                    RuntimeMode = RuntimeMode.UnitTest
                };
                WPFAppWindow.Loaded += (sender, e) =>
                {
                    _uiKeepAlive.Release();
                };
                app.Run(WPFAppWindow);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            await _uiKeepAlive.WaitAsync();
        }

        /// <summary>
        /// In this test method, we will wait for user to click buttons. 
        /// Since the test routine doesn't actually initiate the button clicks 
        /// (the User does) we apply a strategy to await the user action.
        /// </summary>
        [TestMethod]
        public async Task TestUserInteractions()
        {
            SemaphoreSlim awaiter = new SemaphoreSlim(0, 1);
            string? actual = null;
            var TIME_OUT = TimeSpan.FromSeconds(10);
            var TIME_OUT_MESSAGE = $" Expecting response within {TIME_OUT}";
            try
            {
                Awaited += localOnAwaited;
                var stopwatch = Stopwatch.StartNew();

                // Fire and Forget method
                WPFAppWindow?.ShowTestButton(); 
                // App fires OnAwaited when Visibility property of button changes. 
                PromptInUI("Wait for button show...", newline: false);
                await awaiter.WaitAsync(timeout: TimeSpan.FromSeconds(10));
                localCompare("Visible", actual);

                PromptInUI("Click button [StartTest]", Color.Blue, newline: false);
                // When user clicks StartTest, it hides the StartTest button.
                // App fires OnAwaited when Visibility property of button changes. 
                Assert.IsTrue(await awaiter.WaitAsync(TIME_OUT), TIME_OUT_MESSAGE);
                localCompare("Collapsed", actual);

                // When user clicks A, B, C or D, the Click handler fires OnAwaited
                WPFAppWindow?.PromptInRichTextBox("Click button [A]", newline: false);
                await awaiter.WaitAsync(timeout: TimeSpan.FromSeconds(10));
                localCompare("A", actual);

                WPFAppWindow?.PromptInRichTextBox("Click button [B]", newline: false);
                await awaiter.WaitAsync(timeout: TimeSpan.FromSeconds(10));
                localCompare("B", actual);

                WPFAppWindow?.PromptInRichTextBox("Click button [C]", newline: false);
                await awaiter.WaitAsync(timeout: TimeSpan.FromSeconds(10));
                localCompare("C", actual);

                WPFAppWindow?.PromptInRichTextBox("Click button [D]", newline: false);
                await awaiter.WaitAsync(timeout: TimeSpan.FromSeconds(10));
                localCompare("D", actual);

                void localCompare(string? expected, string? actual)
                {
                    if (Equals(expected, actual))
                    {
                        WPFAppWindow?.PromptInRichTextBox(" PASS", Color.Green);
                    }
                    else
                    {
                        WPFAppWindow?.PromptInRichTextBox(" FAIL", Color.DarkRed);
                    }
                    Assert.AreEqual(expected, actual);
                }
            }
            catch(AssertFailedException ex)
            {
                PromptInUI($"{Environment.NewLine}{ex.Message}", Color.Red);
            }
            finally
            {
                // Ensure there's something to release, 
                // even if timed out or exception.
                awaiter.Wait(0);
                // This makes it safe to release without violating `maxCount` of awaiter.
                awaiter.Release();
                // CRITICAL to unconditionally unsubscribe
                // from the static method when done.
                Awaited -= localOnAwaited;
            }

            #region L o c a l M e t h o d s
            async Task NoException(Action action) { try { await Task.Run(action); } catch(AssertFailedException) { } };
            void localOnAwaited(object sender, AwaitedEventArgs e)
            {
                // Listen for OnAwaited events now invoked within the fire-and-forget tasks
                switch (e.Caller)
                {
                    case "OnStartButtonVisibilityChanged":
                        if (sender is System.Windows.Controls.Button vis)
                        {
                            actual = $"{vis.Visibility}";
                            awaiter.Release();
                        }
                        return;
                }
                if (sender is System.Windows.Controls.Button btn)
                {
                    actual = btn.Content as string;
                    switch (actual)
                    {
                        case 
                            string cmp 
                            when cmp
                            .Replace(" ", string.Empty)
                            .Contains("StartButton", StringComparison.OrdinalIgnoreCase) :
                            // 
                            break;
                        default:
                            awaiter.Release();
                            return;
                    }
                }
            }
            #endregion L o c a l M e t h o d s
        }
        void PromptInUI(string prompt, Color? color = null, bool newline = true) =>
            WPFAppWindow?.PromptInRichTextBox(prompt, color, newline);


        [TestMethod]
        public async Task MyTest_ReturnsData()
        {
            SemaphoreSlim awaiter = new SemaphoreSlim(0, 1);
            string? actual = null;
            try
            {
                // <PackageReference Include="IVSoftware.Portable.Threading" Version="1.1.0" />
                // using static IVSoftware.Portable.Threading.Extensions;
                Awaited += localOnAwaited;

                PromptInUI("Waiting for value... ", newline: false);
                WPFAppWindow?.CallSomeAsyncVoidMethod();

                // Wait for it to have a deterministic
                // effect after a non-deteministic time.
                Assert.IsTrue(
                    condition: await awaiter.WaitAsync(timeout: TimeSpan.FromSeconds(10)),
                    "Timed out waiting for property change.");
                PromptInUI("Value Received. ");

                Assert.AreEqual(
                    expected: "MyExpectedValue",
                    actual: actual,
                    $"An unexpected value was detected in {nameof(WPFAppWindow)}.OnPropertyChanged().");
                PromptInUI("PASS", Color.Green);
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
                            else if (args.TryGetValue(nameof(PropertyChangedEventArgs), out o) &&
                            o is PropertyChangedEventArgs winformsPropertyChanged)
                            {
                                switch (winformsPropertyChanged.PropertyName)
                                {
                                    case "MyTargetProperty":
                                        // The property we've been listening to has changed.

                                        if (args.TryGetValue("Value", out o))
                                        {
                                            actual = $"{o}";
                                        }
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
        [ClassCleanup]
        public static async Task ClassCleanup()
        {
            if (WPFAppWindow != null)
            {
                WPFAppWindow.PromptInRichTextBox($"{Environment.NewLine}ALL TESTS HAVE RUN");
                // Cosmetic wait
                for (int i = 10; i >= 0; i--)
                {
                    WPFAppWindow.PromptInRichTextBox(
                        $"Shutting down in {i}");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                await WPFAppWindow.Dispatcher.BeginInvoke(()=>WPFAppWindow.Close());
            }
        }
    }
}