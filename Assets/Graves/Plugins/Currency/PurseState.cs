namespace Graves.Plugins.Currency
{
    /// <summary>
    /// Where money lives: an inventory in every respect except that it is not a bag.
    /// </summary>
    /// <remarks>
    /// <b>Currency is an item, and the only thing that makes it feel otherwise is the container.</b>
    /// The usual argument against coins-as-items is that ten thousand of them would fill your bag -
    /// but a capacity limit is a rule of the container, not a property of the coin. A purse has no
    /// limit, so the objection disappears and nothing about the coin had to change.
    ///
    /// <b>What that buys is mostly invisible.</b> A crafting cost stops being a special case and
    /// becomes another ingredient. Trading is moving items between containers. Loot, stacking,
    /// saving, and the rule that an improved item is never taken as material all apply unchanged.
    /// The reference read for this took the other road and carries a parallel system - typed
    /// currencies with their own accessors, their own totals and their own screen - sitting beside
    /// the item calls doing structurally identical work.
    ///
    /// <b>Where this model genuinely stops.</b> A stack count is an integer, so about two billion is
    /// the ceiling. And a currency you do not carry - a bank balance, faction standing, a season
    /// level - is not in a container at all, and calling one an inventory would be a bag in name
    /// only. Those want something else, and this plugin deliberately does not pretend to cover them.
    /// </remarks>
    public class PurseState : Inventory.InventoryState
    {
        /// <summary>The key a purse is stored under.</summary>
        public new const string Key = "graves.purse";

        /// <inheritdoc />
        public override string StateKey => Key;
    }
}
