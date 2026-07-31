using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Sparroh.UI;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency("sparroh.uilibrary")]
[MycoMod(null, ModFlags.IsClientSide)]
public class UpgradeFilteringPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.upgradefiltering";
    public const string PluginName = "UpgradeFiltering";
    public const string PluginVersion = "1.1.2";

    internal static ManualLogSource Logger;
    public static UpgradeFilteringPlugin Instance;

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
                ConfigManager.Initialize(Config, Logger);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to setup configuration bindings: {ex.Message}");
            }

            try
            {
                StatFormatHandling.Initialize();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize StatFormatHandling: {ex.Message}");
            }

            try
            {
                PriorityGUI.EnsureExists();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize PriorityGUI: {ex.Message}");
            }


            try
            {
                PriorityPatches.Patch(harmony);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to apply PriorityPatches: {ex.Message}");
            }


            foreach (var type in new[]
                     {
                         typeof(GearDetailsWindowPatches),
                         typeof(SetupUpgradesPatch),
                         typeof(SortUpgradesMethodPatch),
                         typeof(PriorityPatches)
                     })

                try
                {
                    harmony.PatchAll(type);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to patch {type.Name}: {ex.Message}");
                }


            try
            {
                foreach (var type in typeof(UpgradeFilteringPlugin).Assembly.GetTypes())
                {
                    if (type == typeof(GearDetailsWindowPatches) ||
                        type == typeof(SetupUpgradesPatch) ||
                        type == typeof(SortUpgradesMethodPatch) ||
                        type == typeof(PriorityPatches) ||
                        type == typeof(ListRebuildReapply))
                        continue;


                    if (!type.IsClass)
                        continue;

                    var attrs = type.GetCustomAttributes(typeof(HarmonyPatch), true);
                    if (attrs == null || attrs.Length == 0)
                        continue;

                    try
                    {
                        harmony.PatchAll(type);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to patch {type.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed scanning assembly patches: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Critical error during mod initialization: {ex.Message}\n{ex.StackTrace}");
        }

        Logger.LogInfo($"{PluginName} loaded successfully.");
    }

    private void Update()
    {
        ConfigManager.Tick();
        GearActionBar.Tick();

        if (!GearActionBar.IsGearMenuOpen())
            return;

        if (!_barRegistered)
        {
            GearActionBar.Register("filter", "Filter", GearActionBar.OrderFilter,
                () => { FilterState.FilterPanel?.Toggle(); });
            GearActionBar.Register("priority", "Upgr. Sort", GearActionBar.OrderPriority,
                PriorityGUI.ToggleWindowStatic, UIButtonStyle.Primary);
            _barRegistered = true;
        }
    }

    private void OnDestroy()
    {
        ConfigManager.Dispose();
        GearActionBar.Unregister("filter");
        GearActionBar.Unregister("priority");
        _barRegistered = false;
    }
}