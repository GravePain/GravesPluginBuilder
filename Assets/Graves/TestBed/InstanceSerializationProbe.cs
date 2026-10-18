using System;
using Graves.Core;


namespace Graves.TestBed
{
    /// <summary>
    /// Stands in for a plugin's per-instance state, so the core's serialization can be tested without
    /// a plugin that needs any.
    /// </summary>
    /// <remarks>
    /// The point of the probe is the TYPE, not the value. <see cref="GameInstance"/> stores state in a
    /// <c>[SerializeReference]</c> list, and the whole instance design depends on a concrete type
    /// declared in another assembly surviving a write and a read. This class is that other assembly.
    /// </remarks>
    [Serializable]
    public class ProbeDurability : InstanceState
    {
        /// <summary>Arbitrary value, used to confirm fields survive alongside the type.</summary>
        public float Remaining = 42f;

        /// <summary>Deliberately a literal, not the type name - the behaviour a real plugin wants.</summary>
        public override string StateKey => "probe.durability";
    }

    /// <summary>A second type, so the test proves the list keeps them distinct rather than collapsing.</summary>
    [Serializable]
    public class ProbeCharges : InstanceState
    {
        /// <summary>Arbitrary value.</summary>
        public int Remaining = 3;

        /// <inheritdoc />
        public override string StateKey => "probe.charges";
    }

}
