using System.Collections.Generic;
using HarmonyLib;

namespace HotbarDeathKeeper
{
    /// <summary>
    /// Harmony patches that intercept a local player's death to snapshot their vanilla
    /// hotbar (row 1, slots 1-8) and restore it once the player respawns, instead of
    /// letting those items fall into the death tombstone.
    /// </summary>
    /// <remarks>
    /// Sequencing matters here and is the whole reason this is two patches instead of one:
    /// on this game version the local <see cref="Player"/> GameObject is destroyed and a
    /// new one is created on respawn ("Local player destroyed" appears in the client log
    /// right after "Starting respawn"). Restoring the items inside
    /// <see cref="Character.CheckDeath"/> itself - the naive approach - would hand them
    /// back to the OLD, about-to-be-destroyed instance, so they'd still be lost. Instead we
    /// hold the items in a static snapshot and only put them back once
    /// <see cref="Player.OnSpawned"/> fires on the NEW instance.
    /// </remarks>
    public static class DeathKeeperPatches
    {
        /// <summary>Number of hotbar slots covered (vanilla row 1, slots 1-8).</summary>
        private const int HotbarSlotCount = 8;

        /// <summary>Inventory grid row that holds the hotbar.</summary>
        private const int HotbarRow = 0;

        /// <summary>
        /// Immutable record of a single hotbar item captured at the moment of death,
        /// along with whether it was equipped so it can be re-equipped on respawn.
        /// </summary>
        private sealed class KeptSlot
        {
            public ItemDrop.ItemData Item { get; }
            public bool WasEquipped { get; }

            public KeptSlot(ItemDrop.ItemData item, bool wasEquipped)
            {
                Item = item;
                WasEquipped = wasEquipped;
            }
        }

        /// <summary>
        /// Items captured on death, pending restoration on the next <see cref="Player.OnSpawned"/>.
        /// Static by design - see the remarks on <see cref="DeathKeeperPatches"/> for why an
        /// instance field would not survive the respawn cycle.
        /// </summary>
        private static readonly List<KeptSlot> keptItems = new List<KeptSlot>();

        /// <summary>
        /// Runs before vanilla death handling. Snapshots and removes the hotbar's contents
        /// so they never reach the death tombstone, while the game still thinks they were
        /// dropped as usual.
        /// </summary>
        [HarmonyPatch(typeof(Character), "CheckDeath")]
        private static class Character_CheckDeath_Patch
        {
            /// <summary>
            /// Runs first among all patches on this method so our snapshot is taken before
            /// any other mod's death handling (e.g. inventory-altering mods) can run.
            /// </summary>
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Character __instance)
            {
                if (!HotbarDeathKeeperPlugin.modEnabled.Value)
                    return;

                if (__instance is not Player player || !player.IsOwner())
                    return;

                if (player.IsDead() || player.GetHealth() > 0f)
                    return;

                // A snapshot already pending (e.g. re-entrant CheckDeath call) - don't overwrite it.
                if (keptItems.Count != 0)
                    return;

                Inventory inventory = player.GetInventory();
                if (inventory == null)
                    return;

                for (int slot = 0; slot < HotbarSlotCount; slot++)
                {
                    ItemDrop.ItemData item = inventory.GetItemAt(slot, HotbarRow);
                    if (item == null)
                        continue;

                    keptItems.Add(new KeptSlot(item, wasEquipped: player.IsItemEquiped(item)));
                }

                if (keptItems.Count == 0)
                {
                    HotbarDeathKeeperPlugin.log.LogDebug("CheckDeath.Prefix: hotbar was empty, nothing to keep.");
                    return;
                }

                foreach (KeptSlot kept in keptItems)
                {
                    bool removed = inventory.RemoveItem(kept.Item);
                    HotbarDeathKeeperPlugin.log.LogDebug(
                        $"CheckDeath.Prefix: removed '{kept.Item.m_shared.m_name}' at {kept.Item.m_gridPos} " +
                        $"(equipped={kept.WasEquipped}) -> success={removed}. Held until respawn.");
                }
            }
        }

        /// <summary>
        /// Runs on every player spawn, including the initial world load - but restoration
        /// only has an effect right after a death, since <see cref="keptItems"/> is empty
        /// the rest of the time.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        private static class Player_OnSpawned_Patch
        {
            private static void Postfix(Player __instance)
            {
                if (__instance == null || !__instance.IsOwner())
                    return;

                if (keptItems.Count == 0)
                    return;

                Inventory inventory = __instance.GetInventory();

                try
                {
                    foreach (KeptSlot kept in keptItems)
                    {
                        bool added = inventory != null && inventory.AddItem(kept.Item, kept.Item.m_gridPos);

                        bool reequipped = false;
                        if (added && HotbarDeathKeeperPlugin.keepEquippedState.Value && kept.WasEquipped)
                            reequipped = __instance.EquipItem(kept.Item);

                        HotbarDeathKeeperPlugin.log.LogDebug(
                            $"OnSpawned: restored '{kept.Item.m_shared.m_name}' at {kept.Item.m_gridPos} " +
                            $"-> added={added}, equipped={reequipped} (wasEquipped={kept.WasEquipped})");
                    }
                }
                finally
                {
                    // Always clear, even on failure - stale entries must never leak into a
                    // future, unrelated spawn (e.g. a later world load).
                    keptItems.Clear();
                }
            }
        }
    }
}
