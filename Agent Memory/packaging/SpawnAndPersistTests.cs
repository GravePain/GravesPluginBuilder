using System.Collections;
using Graves.Core;
using Graves.Plugins.Save;
using Graves.Plugins.Vitals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Graves.Proof.Tests
{
    /// <summary>
    /// The runtime half of the install proof, in play mode because that is the only place the core
    /// does anything.
    /// </summary>
    /// <remarks>
    /// <b>This assembly touches no editor code at all.</b> That is not tidiness - a test assembly
    /// restricted to the Editor platform produces EditMode tests, and EditMode never calls
    /// <c>Awake</c>, which is where <c>GameEntity</c> does its entire job. So the content is authored
    /// by the edit-mode half of the proof, saved as a prefab under Resources, and this loads it the
    /// way a shipped game would.
    /// </remarks>
    public class SpawnAndPersistTests
    {
        private GameObject _host;

        [TearDown]
        public void Clean()
        {
            if (_host != null) Object.Destroy(_host);
        }

        [UnityTest]
        public IEnumerator An_entity_spawns_takes_damage_and_survives_a_save()
        {
            var prefab = Resources.Load<GameObject>("Proof/ProofDummy");
            Assert.NotNull(prefab, "The authored prefab was not found under Resources.");

            // The prefab is saved inactive, because Awake runs the instant the component exists and
            // an entity that wakes without a definition disables itself for good.
            _host = Object.Instantiate(prefab);
            _host.SetActive(true);
            yield return null;

            var entity = _host.GetComponent<GameEntity>();
            Assert.NotNull(entity, "The prefab has no entity component.");
            Assert.NotNull(entity.Definition, "The entity lost its definition crossing the prefab.");
            Assert.NotNull(entity.Instance, "The entity woke without minting an instance.");
            Assert.NotNull(entity.Representation, "The entity woke without a body.");

            var health = _host.GetComponent<Health>();
            Assert.NotNull(health, "The vitals plugin did not attach its component.");
            Assert.Greater(health.Max, 0, "Maximum health came out at nothing.");

            int lost = health.TakeDamage(35);
            Assert.AreEqual(35, lost, "Damage was not applied in full.");

            var state = entity.Instance.GetState<HealthState>();
            Assert.NotNull(state, "Health has no instance state, so it can never be saved.");
            Assert.AreEqual(health.Current, state.Current,
                            "The component and its state disagree, which means one is a copy.");

            int wounded = health.Current;

            Assert.IsTrue(SaveSystem.Save("proof"), "The save was not written.");

            health.RestoreTo(health.Max);
            Assert.AreNotEqual(wounded, health.Current, "Health did not change after saving.");

            var loaded = SaveSystem.Load("proof");
            Assert.IsTrue(loaded.Loaded, "The save could not be read back.");
            Assert.AreEqual(1, loaded.EntitiesRestored, "The saved entity was not matched.");
            Assert.AreEqual(0, loaded.EntitiesMissing, "The load reported a missing entity.");
            Assert.AreEqual(0, loaded.StateUnresolved, "The load could not resolve some state.");
            CollectionAssert.IsEmpty(loaded.Problems, "The load reported problems.");

            Assert.AreEqual(wounded, health.Current,
                            "Health did not come back to the value that was saved.");
        }
    }
}
