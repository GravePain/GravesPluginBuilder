using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Graves.Core.Saving
{
    /// <summary>
    /// Where saves live on disk, and the safe way to write one.
    /// </summary>
    /// <remarks>
    /// Files go under <c>Application.persistentDataPath/Saves</c>, which is the only location Unity
    /// guarantees is writable on every platform. Slot names are sanitised rather than trusted, because
    /// a slot name is usually something a player typed.
    /// </remarks>
    public static class SaveSlots
    {
        /// <summary>Folder name under the persistent data path.</summary>
        public const string FolderName = "Saves";

        /// <summary>File extension, including the dot.</summary>
        public const string Extension = ".json";

        /// <summary>The folder saves are written to, created on demand.</summary>
        public static string Folder
        {
            get
            {
                var path = Path.Combine(Application.persistentDataPath, FolderName);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                return path;
            }
        }

        /// <summary>The full path for a slot.</summary>
        public static string PathFor(string _slot) => Path.Combine(Folder, Sanitise(_slot) + Extension);

        /// <summary>True when that slot has a file.</summary>
        public static bool Exists(string _slot) => File.Exists(PathFor(_slot));

        /// <summary>Every slot name present on disk, newest first.</summary>
        public static List<string> List()
        {
            var names = new List<string>();
            // "*.json" also matches nothing else here, but the backup files written by WriteAtomic
            // end in .json.bak and must not be offered as slots.
            var files = Directory.GetFiles(Folder, "*" + Extension, SearchOption.TopDirectoryOnly);

            Array.Sort(files, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
            foreach (var file in files) names.Add(Path.GetFileNameWithoutExtension(file));

            return names;
        }

        /// <summary>Deletes a slot. Returns false when there was nothing to delete.</summary>
        public static bool Delete(string _slot)
        {
            var path = PathFor(_slot);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        /// <summary>
        /// Writes text to a slot without risking the existing save.
        /// </summary>
        /// <remarks>
        /// Writes to a temporary file first and replaces the real one only once that has succeeded. A
        /// direct write that fails halfway - the disk fills, the game is killed, the power goes -
        /// leaves a truncated file where the save used to be, and the player has lost the run rather
        /// than lost the last few minutes. The temporary file is in the same folder deliberately, so
        /// the replace stays on one volume.
        ///
        /// <b>The replace uses <c>File.Replace</c>, not delete-then-move.</b> The first version of
        /// this method did the latter, which leaves a window with no save file at all: crash between
        /// the delete and the move and the previous save is gone - the exact outcome the method is
        /// named for preventing. Caught by review on 2026-09-05 (finding F-002); the description had
        /// been stronger than the code.
        ///
        /// A backup file is written beside the save and left in place. It costs one extra file per
        /// slot and it is the only thing standing between a failure during the replace itself and a
        /// lost run.
        /// </remarks>
        public static void WriteAtomic(string _slot, string _contents)
        {
            var path = PathFor(_slot);
            var temporary = path + ".tmp";

            File.WriteAllText(temporary, _contents);

            if (File.Exists(path))
            {
                // Replace swaps the files in one operation and keeps the old contents as the backup,
                // so there is no moment where the slot has no file.
                File.Replace(temporary, path, path + ".bak", true);
                return;
            }

            File.Move(temporary, path);
        }

        /// <summary>Reads a slot, or returns null when it does not exist.</summary>
        public static string Read(string _slot)
        {
            var path = PathFor(_slot);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        /// <summary>Strips anything that cannot safely be a filename.</summary>
        private static string Sanitise(string _slot)
        {
            if (string.IsNullOrWhiteSpace(_slot)) return "save";

            var invalid = Path.GetInvalidFileNameChars();
            var builder = new System.Text.StringBuilder(_slot.Length);

            foreach (var character in _slot)
            {
                builder.Append(Array.IndexOf(invalid, character) >= 0 ? '_' : character);
            }

            return builder.ToString().Trim();
        }
    }
}
