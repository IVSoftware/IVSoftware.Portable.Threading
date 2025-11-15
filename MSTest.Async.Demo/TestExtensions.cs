using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MSTest.Async.Demo
{
    internal static class TestExtensions
    {
        // From Collections...

        [Flags]
        public enum FormattedTypeNameOptionFlag
        {
            UseShortTypeName = 1 << 0,
        }

        /// <summary>
        /// Produces a readable name for the specified type, expanding generic type
        /// definitions into a formatted string with argument type names included.
        /// Supports optional flags such as <see cref="FormattedTypeNameOptionFlag">
        /// </summary>
        public static string ToFormattedTypeName(
            this Type @this,
            FormattedTypeNameOptionFlag options = 0)
            => ToFormattedTypeName(@this, out _, options);

        /// <summary>
        /// Produces a readable name for the specified type, expanding generic type
        /// definitions into a formatted string with argument type names included.
        /// Supports optional flags such as <see cref="FormattedTypeNameOptionFlag">
        /// </summary>
        [NotCanonical(reason: "Copied from Collections, which is the canonical source")]
        public static string ToFormattedTypeName(
            this Type @this,
            out Type[] types,
            FormattedTypeNameOptionFlag options = 0)
        {
            bool nameOnly = options.HasFlag(FormattedTypeNameOptionFlag.UseShortTypeName);

            // Handle generic case
            if (@this.IsGenericType)
            {
                var genericType = @this.GetGenericTypeDefinition();
                var genericName = nameOnly
                    ? genericType.Name
                    : genericType.FullName ?? genericType.Name;

                // Strip the arity backtick
                var unmangled = genericName.Contains('`')
                    ? genericName[..genericName.IndexOf('`')]
                    : genericName;

                types = @this.GetGenericArguments();
                var args = types.Select(t => t.ToFormattedTypeName(options));

                return $"{unmangled}<{string.Join(", ", args)}>";
            }

            // Non-generic
            types = Type.EmptyTypes;
            return nameOnly
                ? @this.Name
                : @this.FullName ?? @this.Name;
        }
    }

    internal class NotCanonicalAttribute : Attribute
    {
        public NotCanonicalAttribute(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; }
    }
}
