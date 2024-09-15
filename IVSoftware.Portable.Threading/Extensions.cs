using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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
            Awaited?.Invoke(
                sender, 
                e ??                            // ...from the block that instantiates AwaitedEventArgs
                new AwaitedEventArgs(caller));  // ...from the block that calls OnAwaited() 
        }
        public static event EventHandler<AwaitedEventArgs> Awaited;
    }

    /// <summary>
    /// Represents event data that, by default, behaves like a dictionary but can be overridden by the user with any type of object.
    /// </summary>
    public class AwaitedEventArgs : EventArgs, IEnumerable
    {
        /// <summary>
        /// By default, this object behaves like a dictionary, storing key-value pairs.
        /// </summary>
        public object Args { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the AwaitedEventArgs class with the calling method's name as the caller.
        /// </summary>
        /// <param name="caller">The name of the method that instantiated the object, automatically captured.</param>
        public string Caller { get; }

        /// <summary>
        /// Constructs a new instance of AwaitedEventArgs, defaulting to use a dictionary for storing event data.
        /// </summary>
        /// <param name="caller">The name of the method that instantiated the object, automatically captured.</param>
        public AwaitedEventArgs([CallerMemberName] string caller = null)
        {
            Caller = caller;
        }

        /// <summary>
        /// Constructs a new instance of AwaitedEventArgs, allowing for the Args property to be set with a custom object.
        /// </summary>
        /// <param name="args">The object to use for the Args property. Throws an exception if null.</param>
        /// <param name="caller">The name of the method that instantiated the object, automatically captured.</param>
        public AwaitedEventArgs(object args, [CallerMemberName] string caller = null)
            : this(caller)
        {
            if (args != null)
            {
                Args = args;
            }
            else
            {
                throw new ArgumentNullException(nameof(args), "Args cannot be null; provide a valid object.");
            }
        }

        /// <summary>
        /// Adds a key-value pair to the Args dictionary, enabling the use of collection initializer syntax.
        /// Throws an exception if Args is not a dictionary.
        /// </summary>
        /// <param name="key">The key for the value to be stored.</param>
        /// <param name="value">The value to be stored.</param>
        /// <remarks>
        /// This method allows the AwaitedEventArgs to be initialized or modified using collection initializer syntax
        /// as long as Args is in its default dictionary form, enhancing ease of use when setting multiple key-value pairs.
        /// </remarks>
        public void Add(string key, object value)
        {
            if (Args is Dictionary<string, object> dict)
            {
                dict[key] = value; // Using indexer to allow overwriting existing keys.
            }
            else
            {
                throw new InvalidOperationException("Cannot add key-value pairs when Args is not a dictionary.");
            }
        }

        /// <summary>
        /// Retrieves or sets a value by key when Args is a dictionary, which is the default configuration.
        /// Provides the option to throw if the key is not found or if Args is no longer a compatible dictionary type.
        /// For setting values, adds or updates the key with the provided value if Args is a dictionary.
        /// </summary>
        /// <param name="key">The key of the value to retrieve or set.</param>
        /// <param name="throw">If true, throws a KeyNotFoundException when attempting to retrieve a key that is not found in the dictionary, 
        /// or an InvalidOperationException if Args is not a dictionary during retrieval or setting.</param>
        /// <returns>The value associated with the specified key or null if the key is not found and 'throw' is false when retrieving.</returns>
        public object this[string key, bool @throw = false]
        {
            get
            {
                if (Args is Dictionary<string, object> dict)
                {
                    if (dict.TryGetValue(key, out var value))
                    {
                        return value;
                    }
                    else if (@throw)
                    {
                        throw new KeyNotFoundException("The given key was not present in the dictionary.");
                    }
                }
                else if (@throw)
                {
                    throw new InvalidOperationException("Args object is not a dictionary. Please check your Constructor.");
                }
                return default;
            }
            set
            {
                if (Args is Dictionary<string, object> dict)
                {
                    dict[key] = value;  // Adds or updates the key with the provided value.
                }
                else if (@throw)
                {
                    throw new InvalidOperationException("Args object is not a dictionary. Unable to set the value.");
                }
            }
        }


        /// <summary>
        /// Provides an enumerator for the Args property if it is an IEnumerable.
        /// </summary>
        /// <returns>An IEnumerator for the Args if it is enumerable, or an empty enumerator if it is not.</returns>
        public IEnumerator GetEnumerator()
        {
            if (Args is IEnumerable enumerable)
            {
                return enumerable.GetEnumerator();
            }
            return Enumerable.Empty<object>().GetEnumerator();
        }

        /// <summary>
        /// Tries to retrieve a value by key from the Args property when it is a dictionary, casting it to the specified type.
        /// This method provides a way to attempt to retrieve values without throwing exceptions,
        /// returning a boolean indicating success or failure.
        /// </summary>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <param name="value">The value associated with the specified key, if found and successfully cast; otherwise, default value of type T.</param>
        /// <returns>True if the key is found, the value exists and is of type T; otherwise, false.</returns>
        /// <typeparam name="T">The type to which the retrieved value should be cast.</typeparam>
        public bool TryGetValue<T>(string key, out T value)
        {
            if (Args is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue(key, out var o) && o is T t)
                {
                    value = t;
                    return true;
                }
            }
            value = default;
            return false;
        }
    }
}
