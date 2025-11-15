using Microsoft.VisualStudio.TestTools.UnitTesting;
using Size = System.Drawing.Size;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using Label = System.Windows.Forms.Label;

namespace IVSoftware.WinOS.MSTest.Extensions.STA
{
    [TestClass]
    public sealed class TestClass_STA
    {
        /// <summary>
        /// Confirms that execution enters the STARunner STA thread.
        /// </summary>
        /// <remarks>
        /// Verifies that code invoked through RunAsync executes directly on the
        /// STA thread and does not require UI marshalling. This establishes the
        /// baseline contract for working inside the STARunner UI context.
        /// </remarks>
        [TestMethod]
        public async Task Test_CanonicalPOC()
        {
            using var sta = new STARunner(isVisible: false);
            await sta.RunAsync(localStaTest);

            #region L o c a l F x 
            async Task localStaTest()
            {
                Assert.IsFalse(
                    sta.MainForm.InvokeRequired,
                    $"Expecting confirmation of UI thread context. No marshal is needed.");

                // Manipulate the UI
                sta.MainForm.Text = "Hello";
                Assert.IsInstanceOfType<SilentRunner>(sta.MainForm);
                Assert.IsTrue(sta.MainForm.IsHandleCreated);
                Assert.AreEqual("Hello", sta.MainForm.Text);

                await Task.CompletedTask;
            }
            #endregion L o c a l F x
        }

        /// <summary>
        /// Exercises visible STA execution in a single RunAsync block.
        /// </summary>
        /// <remarks>
        /// The entire test runs on the STA thread, enabling direct manipulation
        /// of MainForm without additional marshalling. Validates that UI updates
        /// remain stable across timed delays when the window is visible.
        /// </remarks>
        [TestMethod]
        public async Task Test_MonolithicVisible()
        {
            using var sta = new STARunner(isVisible: true);

            await sta.RunAsync(localTestOnStaThread);

            #region L o c a l F x 
            async Task localTestOnStaThread()
            {
                Assert.IsFalse(
                    sta.MainForm.InvokeRequired,
                    $"Expecting confirmation of UI thread context. No marshal is needed.");

                sta.MainForm.Text = "Main Form";

                for (int countdown = 5; countdown >= 0; countdown--)
                {
                    sta.MainForm.Text = $"Main Form - Shutdown in {countdown}";
                    await Task.Delay(1000);
                }
            }
            #endregion L o c a l F x
        }

        /// <summary>
        /// Exercises silent STA execution in a single RunAsync block.
        /// </summary>
        /// <remarks>
        /// Builds a temporary popup form inside the STA universe, stabilizes its
        /// layout, and updates both the popup and MainForm over timed intervals.
        /// Confirms correct behavior when the STA environment is not visible.
        /// </remarks>
        [TestMethod]
        public async Task Test_MonolithicSilent()
        {
            using var sta = new STARunner(isVisible: false);

            await sta.RunAsync(localTestOnStaThread);

            #region L o c a l F x 
            async Task localTestOnStaThread()
            {
                Assert.IsFalse(sta.MainForm.InvokeRequired);

                sta.MainForm.Text = "Main Form";

                // Build UI in place
                var popup = new Form
                {
                    Size = new Size(300, 100),
                    FormBorderStyle = FormBorderStyle.None,
                    StartPosition = FormStartPosition.Manual,
                    BackColor = Color.Black
                };

                // Determine which display has the cursor
                var screen = Screen.FromPoint(Cursor.Position);
                popup.Location = screen.WorkingArea.Location;

                var label = new Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Consolas", 16, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = ColorTranslator.FromHtml("#444444")
                };

                popup.Controls.Add(label);
                popup.Show();
                popup.CycleTopmost();

                // Stabilize layout + paint before we ever await
                popup.PerformLayout();
                popup.Update();

                // Mutate UI normally
                for (int countdown = 5; countdown >= 0; countdown--)
                {
                    label.Text = $"Shutdown in {countdown}";
                    sta.MainForm.Text = $"Main Form - Shutdown in {countdown}";
                    await Task.Delay(1000); // allowed inside UI callback
                }
            };
            #endregion L o c a l F x
        }

        /// <summary>
        /// Validates reentrant UI execution and sequential STARunner lifetimes.
        /// </summary>
        /// <remarks>
        /// Confirms that multiple RunAsync calls within a single STARunner instance
        /// reliably reenter the STA thread and update the UI without marshalling.
        /// After disposal, a second STARunner is created to verify clean teardown,
        /// correct foreground activation, and full isolation between lifetimes.
        /// This test exercises both reentrant UI behavior and sequential runner
        /// lifecycles.
        /// </remarks>
        [TestMethod]
        public async Task Test_ReentrantAndSequentialRunners()
        {
            STARunner sta;
            using (sta = new STARunner(isVisible: true))
            {
                await sta.RunAsync(async () =>
                {
                    sta.MainForm.Text = "Main Form #1";
                    await Task.CompletedTask;
                });
                for (int countdown = 5; countdown >= 0; countdown--)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    await sta.RunAsync(async () =>
                    {
                        sta.MainForm.Text = $"Main Form #1 - Shutdown in {countdown}";
                        await Task.CompletedTask;
                    });
                }
            }

            // This tests not only the robustness of the dispose, but also
            // the ability of the second instance to bring itself to front.
            using (sta = new STARunner(isVisible: true))
            {
                await sta.RunAsync(async () =>
                {
                    sta.MainForm.Text = "Main Form #2";
                    await Task.CompletedTask;
                });
                for (int countdown = 5; countdown >= 0; countdown--)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    await sta.RunAsync(async () =>
                    {
                        sta.MainForm.Text = $"Main Form #2 - Shutdown in {countdown}";
                        await Task.CompletedTask;
                    });
                }
            }
        }

        /// <summary>
        /// Verifies that STARunner can activate a user-supplied Form type
        /// and execute UI code on the STA thread without marshaling.
        /// </summary>
        /// <remarks>
        /// This test confirms that custom Form classes can serve as the main
        /// window for a STARunner instance. The callback runs on the UI thread,
        /// allowing direct property manipulation, handle verification, and
        /// timing behavior consistent with WinForms.
        /// </remarks>
        [TestMethod]
        public async Task Test_CustomUserForm()
        {
            using var sta = new STARunner(isVisible: false, typeof(UserForm));
            await sta.RunAsync(localStaTest);

            #region L o c a l F x 
            async Task localStaTest()
            {
                Assert.IsFalse(
                    sta.MainForm.InvokeRequired,
                    $"Expecting confirmation of UI thread context. No marshal is needed.");

                // Manipulate the UI
                sta.MainForm.Text = "Hello";
                Assert.IsInstanceOfType<UserForm>(sta.MainForm);
                Assert.IsTrue(sta.MainForm.IsHandleCreated);
                Assert.AreEqual("Hello", sta.MainForm.Text);

                await Task.Delay(TimeSpan.FromSeconds(2.5));
            }
            #endregion L o c a l F x
        }

        /// <summary>
        /// Simple test form used to validate STARunner activation
        /// with a user-provided Form type.
        /// </summary>
        /// <remarks>
        /// Provides a visible identity and stable layout for tests that
        /// rely on form instantiation, handle creation, or UI mutation
        /// within STARunner's STA thread.
        /// </remarks>

        class UserForm : Form 
        {
            public UserForm()
            {
                Text = nameof(UserForm);
                BackColor = Color.AliceBlue;
                StartPosition = FormStartPosition.CenterScreen;
            }
        }
    }
}
