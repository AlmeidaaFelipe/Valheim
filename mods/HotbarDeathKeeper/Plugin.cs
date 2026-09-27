using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace HotbarDeathKeeper
{
    /// <summary>
    /// Entry point for the HotbarDeathKeeper BepInEx plugin. Wires up configuration and
    /// applies the Harmony patches defined in <see cref="DeathKeeperPatches"/>.
    /// </summary>
    /// <remarks>
    /// Client-side only: the character owner is the client, not the server, so installing
    /// this on a dedicated server has no effect. Declared as a soft dependency on ExtraSlots
    /// purely for load-order purposes (see <see cref="DependencyGuids.ExtraSlots"/>) - this
    /// plugin does not call into ExtraSlots' API and works fine without it.
    /// </remarks>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(DependencyGuids.ExtraSlots, BepInDependency.DependencyFlags.SoftDependency)]
    public class HotbarDeathKeeperPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "hdk.valheim.hotbardeathkeeper";
        public const string PluginName = "Hotbar Death Keeper";
        public const string PluginVersion = "1.0.0";

        /// <summary>BepInEx GUIDs of mods this plugin interacts with by load order only.</summary>
        private static class DependencyGuids
        {
            public const string ExtraSlots = "shudnal.ExtraSlots";
        }

        private static class ConfigSection
        {
            public const string General = "General";
        }

        internal static ConfigEntry<bool> modEnabled;
        internal static ConfigEntry<bool> keepEquippedState;
        internal static ManualLogSource log;

        private Harmony harmony;

        private void Awake()
        {
            log = Logger;

            modEnabled = Config.Bind(
                ConfigSection.General,
                "Enabled",
                defaultValue: true,
                "Keep vanilla hotbar (row 1, slots 1-8) items on death instead of losing them to the tombstone.");

            keepEquippedState = Config.Bind(
                ConfigSection.General,
                "Keep equipped state",
                defaultValue: true,
                "Re-equip the weapon/tool/shield that was in hand when you died.");

            harmony = new Harmony(PluginGUID);
            harmony.PatchAll();

            log.LogInfo($"{PluginName} {PluginVersion} loaded. Patched methods: {harmony.GetPatchedMethods().Count()}");
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }
    }
}
