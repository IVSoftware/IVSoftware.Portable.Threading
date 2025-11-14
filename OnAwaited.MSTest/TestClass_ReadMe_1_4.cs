using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OnAwaited.MSTest;

[TestClass]
public class TestClass_ReadMe_1_4
{
    [TestMethod]
    public async Task Test_UnawaitableBefore()
    {
        await this.RunOnSTAThread(async () =>
        {
            string actual = string.Empty;
            Random rando = new Random(); // An unseeded random.

            System.Windows.Forms.Button btn = new();
            btn.Click += localOnButtonClicked;

            // This is an EventHandler delegate, and returning Task is not an option.
            async void localOnButtonClicked(object? sender, EventArgs e)
            {
                // We can only estimate how long this will take.
                await Task.Delay(TimeSpan.FromSeconds(0.5 + rando.NextDouble()));
                actual = "Clicked!";
            }
            btn.PerformClick();

            // So we put in a magic delay and hope.
            await Task.Delay(TimeSpan.FromSeconds(1));
            Assert.AreEqual("Clicked", actual);
        });
        { }
    }
}
static class STAExtensions
{
    public static async Task RunOnSTAThread(this object _,  Func<Task> action)
    {
        TaskCompletionSource _tcs = new();
        var thread = new Thread(() =>
        {
            action();
            _tcs.SetResult();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        await _tcs.Task;
    }
}
