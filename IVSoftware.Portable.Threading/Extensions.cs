using System;
using System.Runtime.CompilerServices;

namespace IVSoftware.Portable.Threading
{
    public static partial class Extensions
    {
        public static void OnAwaited(this object sender, AwaitedEventArgs e)
        {
            Awaited?.Invoke(sender, e);
        }
        public static event EventHandler<AwaitedEventArgs> Awaited;
    }
    public class AwaitedEventArgs : EventArgs
    {
        public AwaitedEventArgs([CallerMemberName] string caller = null)
        {
            Caller = caller;
        }
        public AwaitedEventArgs(object args, [CallerMemberName] string caller = null)
        {
            Caller = caller;
            Args = args;
        }
        public string Caller { get; }
        public object Args { get; }
    }
}
