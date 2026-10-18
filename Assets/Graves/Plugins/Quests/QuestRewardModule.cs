using Graves.Core;

namespace Graves.Plugins.Quests
{
    /// <summary>
    /// Something a quest pays out. Added to a quest definition like any other module.
    /// </summary>
    /// <remarks>
    /// <b>This is why the quest plugin does not know what an inventory is.</b> A reward that hands
    /// over items needs the inventory plugin, one that hands over experience needs progression, and
    /// a quest system that named either would drag it into every game that wanted quests. Each
    /// reward lives in the assembly that owns the thing being given, and the quest plugin only knows
    /// that rewards exist and can be asked to pay.
    ///
    /// <b>It is a module, so it needed no editor work at all.</b> The authoring window already lists,
    /// adds, orders and removes modules on any definition, and a quest is a definition. The reward is
    /// authored in the same place as everything else on the first try.
    ///
    /// <b><see cref="DefinitionModule.AttachTo"/> is deliberately not overridden.</b> A quest is never
    /// spawned as an entity, so nothing ever calls it, and the base is already a no-op.
    ///
    /// <b>Paying must be safe to call once and never again.</b> The log records that a quest was paid
    /// and will not ask twice, but a reward that cannot be given - no bag, no room, a deleted item -
    /// must say so by returning false, so the log leaves it unclaimed and the player can come back for
    /// it rather than losing it silently.
    /// </remarks>
    public abstract class QuestRewardModule : DefinitionModule
    {
        /// <summary>
        /// Hands the reward to an entity. Returns false when it could not be given, which leaves the
        /// quest unclaimed rather than paid.
        /// </summary>
        public abstract bool Pay(GameEntity _recipient);

        /// <summary>A short line for a log or a quest screen, e.g. "100 gold".</summary>
        public abstract string Describe();
    }
}
