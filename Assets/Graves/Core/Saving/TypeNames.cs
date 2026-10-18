using System;
using System.Collections.Generic;
using UnityEngine;

namespace Graves.Core.Saving
{
    /// <summary>
    /// Turns a type into a string that survives a save file, and back again.
    /// </summary>
    /// <remarks>
    /// <b>Not <c>AssemblyQualifiedName</c>.</b> That includes the assembly version, culture and public
    /// key token, all of which change under you - and a save written yesterday then fails to resolve a
    /// type that is sitting right there. Namespace, class and assembly name are enough to find a type
    /// and are stable across recompiles and builds.
    ///
    /// Resolution falls back to scanning loaded assemblies, because a type can legitimately move
    /// between assemblies when a plugin is reorganised. That is slower, so results are cached; it runs
    /// once per unknown type per session and only on the load path.
    /// </remarks>
    public static class TypeNames
    {
        private static readonly Dictionary<string, Type> Cache = new Dictionary<string, Type>();

        /// <summary>Writes a type as "Namespace.Class, AssemblyName". Never version-qualified.</summary>
        public static string Write(Type _type)
        {
            if (_type == null) return string.Empty;
            return _type.FullName + ", " + _type.Assembly.GetName().Name;
        }

        /// <summary>
        /// Resolves a written type name, or returns null when nothing in the domain matches.
        /// </summary>
        /// <remarks>
        /// A null return means the plugin that wrote this state is not installed. The caller must
        /// report that and leave the data alone - never drop it, because reinstalling the plugin
        /// should bring the state back, and a load that quietly discards what it cannot read turns a
        /// recoverable situation into a permanent one.
        /// </remarks>
        public static Type Read(string _written)
        {
            if (string.IsNullOrEmpty(_written)) return null;
            if (Cache.TryGetValue(_written, out var cached)) return cached;

            var found = Type.GetType(_written);

            if (found == null)
            {
                // The assembly moved, or the name was written by a build that laid things out
                // differently. Fall back to matching on the type's full name alone.
                var comma = _written.IndexOf(',');
                var fullName = comma > 0 ? _written.Substring(0, comma).Trim() : _written.Trim();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    found = assembly.GetType(fullName, false);
                    if (found != null) break;
                }

                if (found != null)
                {
                    Debug.LogWarning("[Save] '" + _written + "' was not found where the save said it " +
                                     "was, but a type of the same name exists in '" +
                                     found.Assembly.GetName().Name + "'. Using it.");
                }
            }

            Cache[_written] = found;
            return found;
        }

        /// <summary>Drops the resolution cache. For editor tooling after a recompile.</summary>
        public static void ClearCache() => Cache.Clear();
    }
}
