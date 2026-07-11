using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Sparroh.UI;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency("sparroh.uilibrary")]
[MycoMod(null, ModFlags.IsClientSide)]
public class SparrohPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.upgradesorting";
    public const string PluginName = "UpgradeSorting";
    public const string PluginVersion = "1.0.0";

    private ConfigEntry<bool> EnableStatReformat;
    internal static ManualLogSource Logger;
    public static SparrohPlugin Instance;

    private bool _barRegistered;

    private void Awake()
    {
        try
        {
            Logger = base.Logger;
            Instance = this;

            var harmony = new Harmony(PluginGUID);

            try
            {
                EnableStatReformat = Config.Bind(
                    "General",
                    "Reformat Statistics",
                    true,
                    "Force Key: Value stat format");
                FormatHandling.enableStatReformat = EnableStatReformat.Value;
                EnableStatReformat.SettingChanged += (_, _) =>
                {
                    FormatHandling.enableStatReformat = EnableStatReformat.Value;
                };
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Failed to setup configuration bindings: {ex.Message}");
            }

            try { FormatHandling.Initialize(); }
            catch (System.Exception ex) { Logger.LogError($"Failed to initialize FormatHandling: {ex.Message}"); }

            try { SortHandling.AddPriorityGUI(); }
            catch (System.Exception ex) { Logger.LogError($"Failed to initialize PriorityGUI: {ex.Message}"); }

            try { PriorityPatches.Patch(harmony); }
            catch (System.Exception ex) { Logger.LogError($"Failed to apply PriorityPatches: {ex.Message}"); }

            try { harmony.PatchAll(); }
            catch (System.Exception ex) { Logger.LogError($"Failed to apply Harmony patches: {ex.Message}"); }
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"Critical error during mod initialization: {ex.Message}\n{ex.StackTrace}");
        }

        Logger.LogInfo($"{PluginName} loaded successfully.");
    }

    private void Update()
    {
        GearActionBar.Tick();

        if (!GearActionBar.IsGearMenuOpen())
            return;

        if (!_barRegistered)
        {
            GearActionBar.Register("filter", "Filter", GearActionBar.OrderFilter, () =>
            {
                UpgradeSortingPlugin.FilterPanel?.Toggle();
            }, UIButtonStyle.Default);
            _barRegistered = true;
        }
    }

    private void OnDestroy()
    {
        GearActionBar.Unregister("filter");
        _barRegistered = false;
    }
}
