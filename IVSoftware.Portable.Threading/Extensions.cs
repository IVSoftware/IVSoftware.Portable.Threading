using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace IVSoftware.Portable.Threading
{
    public static partial class Extensions
    {
        /// <summary>
        /// Raises the Awaited event, automatically passing the caller's name as part of the event arguments.
        /// If the event arguments are not provided, a new instance of AwaitedEventArgs is created with the caller's name.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data. If null, a new instance with the caller's name is created.</param>
        /// <param name="caller">Automatically captured name of the method or property that calls this method.</param>

        public static void OnAwaited(
            this object sender, 
            AwaitedEventArgs e = null,
            [CallerMemberName] string caller = null)
        {
            // Caller is inferred...
            AwaitedEventArgs.RaiseSelf(
                sender, 
                e ??                            // ...from the block that instantiates AwaitedEventArgs
                new AwaitedEventArgs(caller));  // ...from the block that calls OnAwaited() 
        }
        public static event EventHandler<AwaitedEventArgs> Awaited
        {
            add => AwaitedEventArgs.Awaited += value;
            remove => AwaitedEventArgs.Awaited -= value;
        }

        /// <summary>
        /// Reports whether the semaphore was already signaled, then releases it.
        /// </summary>
        /// <remarks>
        /// Wait(0) performs a non-blocking probe. A false result means the semaphore
        /// was signaled. The method returns that information and always calls Release
        /// to restore the semaphore to a known state.
        /// </remarks>
        public static bool SafeRelease(this SemaphoreSlim @this)
        {
            bool wasSignaled = !@this.Wait(0);
            @this.Release();
            return wasSignaled;
        }
    }
}
