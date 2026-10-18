using System;
using System.Collections.Generic;
using UnityEngine;

namespace Graves.Core.Saving
{
    /// <summary>
    /// The on-disk shape of a save. Plain serializable data with no behaviour, so what is written is
    /// exactly what is read.
    /// </summary>
    /// <remarks>
    /// <b>Everything here is a string or a list of them.</b> Nothing in a save file is an object
    /// reference, a scene path, or an index into a list. Object references are what broke the previous
    /// project - they resolved after a restart to a boot texture and an arm bone - and indices break
    /// the moment content is reordered. Only stable ids cross the disk boundary.
    /// </remarks>
    [Serializable]
    public class SaveFile
    {
        /// <summary>Format version, so an old save can be recognised and migrated rather than misread.</summary>
        public int Version;

        /// <summary>Framework version that wrote this, for diagnosing a report from elsewhere.</summary>
        public string FrameworkVersion;

        /// <summary>When it was written, UTC, round-trip format.</summary>
        public string SavedAtUtc;

        /// <summary>Free text the game may set - a location name, a chapter, a play time.</summary>
        public string Label;

        /// <summary>One entry per saved entity.</summary>
        public List<SavedEntity> Entities = new List<SavedEntity>();

        /// <summary>One entry per non-entity saveable.</summary>
        public List<SavedBlob> Blobs = new List<SavedBlob>();
    }

    /// <summary>One entity's identity and its per-instance state.</summary>
    [Serializable]
    public class SavedEntity
    {
        /// <summary>The entity's authored persistent id. How it is found again on load.</summary>
        public string InstanceId;

        /// <summary>What it is. Recorded so a load can detect that a definition was swapped or deleted.</summary>
        public string DefinitionUid;

        /// <summary>
        /// True when this entity was made at runtime rather than placed in a scene, so a load has to
        /// rebuild it rather than find it.
        /// </summary>
        public bool CreatedAtRuntime;

        /// <summary>Where it stood. Only meaningful when <see cref="CreatedAtRuntime"/>.</summary>
        public Vector3 Position;

        /// <summary>Which way it faced. Only meaningful when <see cref="CreatedAtRuntime"/>.</summary>
        public Quaternion Rotation;

        /// <summary>Its per-instance state, each independently serialized.</summary>
        public List<SavedState> State = new List<SavedState>();
    }

    /// <summary>
    /// One piece of per-instance state, with enough information to rebuild its concrete type.
    /// </summary>
    /// <remarks>
    /// <b>Why the type is stored as a string, when Unity would do it for us.</b> An earlier version of
    /// this comment said <c>JsonUtility</c> could not serialize a polymorphic field. That was wrong,
    /// and was corrected on 2026-09-05 by testing it: <c>JsonUtility</c> handles
    /// <c>[SerializeReference]</c> perfectly well, writing a <c>references.RefIds</c> block with each
    /// concrete type's class, namespace and assembly.
    ///
    /// The real reason to keep doing it by hand is narrower and more important. When the built-in
    /// mechanism meets a type it cannot resolve - because the plugin that wrote it is not installed -
    /// it drops the entry, and the next save writes that state out of existence. Keeping each state
    /// as an opaque string means the bytes survive a build that cannot read them, which is the whole
    /// point of <c>SaveSystem.CarriedForward</c>.
    ///
    /// The cost of being right for the wrong reason is that this only protects state at the TOP level
    /// of an instance. State nested deeper - an item inside an inventory, whose own state comes from
    /// an uninstalled plugin - goes through Unity's mechanism and is not protected. Known, recorded,
    /// not yet solved.
    ///
    /// <b>The type string deliberately omits the assembly version.</b> A version-qualified name breaks
    /// on every recompile. Namespace, class and assembly name are enough to find the type again and
    /// are stable across builds.
    ///
    /// <see cref="Key"/> is stored as well, and is not redundant: it survives a class being renamed,
    /// which the type string does not. When the type cannot be resolved the key is what makes the
    /// report say something useful instead of "unknown".
    /// </remarks>
    [Serializable]
    public class SavedState
    {
        /// <summary>The state's stable key, from <c>InstanceState.StateKey</c>.</summary>
        public string Key;

        /// <summary>Concrete type, as "Namespace.Class, AssemblyName".</summary>
        public string Type;

        /// <summary>The state's own fields, as JSON.</summary>
        public string Json;
    }

    /// <summary>State from an <c>ISaveable</c> that is not an entity - a quest log, world flags.</summary>
    [Serializable]
    public class SavedBlob
    {
        /// <summary>The saveable's stable key.</summary>
        public string Key;

        /// <summary>Concrete type of the captured object, as "Namespace.Class, AssemblyName".</summary>
        public string Type;

        /// <summary>The captured object's fields, as JSON.</summary>
        public string Json;
    }
}
