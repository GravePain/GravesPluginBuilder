using Graves.Core;
using UnityEngine;

namespace Graves.TestBed
{
    /// <summary>
    /// An asset that holds a <see cref="GameInstance"/>, so a real serialization round trip can be
    /// run against a file on disk rather than against memory.
    /// </summary>
    /// <remarks>
    /// <b>This lives in its own file for a reason that cost a debugging cycle.</b> A ScriptableObject
    /// or MonoBehaviour must be declared in a file whose name matches the class, or Unity never
    /// creates a MonoScript for it. Everything appears to work - the asset is created, the data
    /// serializes correctly - but the file is written with <c>m_Script: {fileID: 0}</c> and can never
    /// be loaded again. It failed silently, and the data looked perfect in the file the whole time.
    ///
    /// Plain <c>[Serializable]</c> classes have no such rule and can share a file freely, which is why
    /// the probe state types still sit together in InstanceSerializationProbe.cs.
    /// </remarks>
    public class InstanceProbeAsset : ScriptableObject
    {
        /// <summary>The instance under test.</summary>
        public GameInstance Instance = new GameInstance();
    }
}
