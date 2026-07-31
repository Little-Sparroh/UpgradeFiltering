using System;
using System.Text.RegularExpressions;
using HarmonyLib;
using Pigeon;
using TMPro;
using StringBuilder = System.Text.StringBuilder;

public static class StatFormatHandling
{
    public static bool enableStatReformat = true;

    public static void Initialize()
    {
        try
        {
        }
        catch (Exception ex)
        {
            UpgradeFilteringPlugin.Logger.LogError($"Failed to initialize StatFormatHandling: {ex.Message}");
        }
    }

    public static void ReformatStats(Upgrade __instance, ref string text)
    {
        if (!enableStatReformat || string.IsNullOrEmpty(text))
            return;

        try
        {
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    sb.AppendLine();
                    continue;
                }

                var tagStripped = Regex.Replace(trimmed, @"<[^>]*>", "");
                if (Regex.IsMatch(tagStripped, @"^[-+]?\d"))
                {
                    var match = Regex.Match(tagStripped, @"^([-+]?\d+(?:\.\d+)?[%s]?)\s*(.+)$");
                    if (match.Success)
                    {
                        var value = match.Groups[1].Value;
                        var key = match.Groups[2].Value.Trim();
                        if (!string.IsNullOrEmpty(key))
                        {
                            var formatted = $"{key}: <b>{value}</b>";
                            sb.AppendLine(formatted);
                            continue;
                        }
                    }
                }

                sb.AppendLine(line);
            }

            text = sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
        }
    }

    public static void ReformatUIText(TextMeshProUGUI textComponent, string fieldName = "text")
    {
        if (!enableStatReformat || textComponent == null)
            return;

        var currentText = textComponent.text;
        if (string.IsNullOrEmpty(currentText))
            return;

        try
        {
            var lines = currentText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    sb.AppendLine();
                    continue;
                }

                var tagStripped = Regex.Replace(trimmed, @"<[^>]*>", "");
                if (Regex.IsMatch(tagStripped, @"^[-+]?\d"))
                {
                    var match = Regex.Match(tagStripped, @"^([-+]?\d+(?:\.\d+)?[%s]?)\s*(.+)$");
                    if (match.Success)
                    {
                        var value = match.Groups[1].Value;
                        var key = match.Groups[2].Value.Trim();
                        if (!string.IsNullOrEmpty(key))
                        {
                            var formatted = $"{key}: <b>{value}</b>";
                            sb.AppendLine(formatted);
                            continue;
                        }
                    }
                }

                sb.AppendLine(line);
            }

            var reformattedText = sb.ToString().TrimEnd();
            textComponent.text = reformattedText;
            textComponent.ForceMeshUpdate();
        }
        catch (Exception ex)
        {
        }
    }
}

[HarmonyPatch(typeof(Upgrade), nameof(Upgrade.GetStatList))]
public static class StatListReformatPatch
{
    private static void Postfix(Upgrade __instance, int seed, ref string __result)
    {
        StatFormatHandling.ReformatStats(__instance, ref __result);
    }
}

[HarmonyPatch(typeof(HoverInfoDisplay), "Activate")]
public static class HoverInfoDisplayReformatPatch
{
    private static void Postfix(HoverInfoDisplay __instance, HoverInfo info, bool resetPosition)
    {
        if (!StatFormatHandling.enableStatReformat || info == null || info.GetType().Name == "DirectiveButton")
            return;


        try
        {
            var textField = AccessTools.Field(typeof(HoverInfoDisplay), "text");
            if (textField != null)
            {
                var textComponent = (TextMeshProUGUI)textField.GetValue(__instance);
                StatFormatHandling.ReformatUIText(textComponent, "main text");
            }

            var statsField = AccessTools.Field(typeof(HoverInfoDisplay), "statsText");
            if (statsField != null)
            {
                var statsComponent = (TextMeshProUGUI)statsField.GetValue(__instance);
                StatFormatHandling.ReformatUIText(statsComponent, "statsText");
            }
        }
        catch (Exception ex)
        {
            UpgradeFilteringPlugin.Logger.LogWarning($"HoverInfoDisplayReformatPatch: Failed: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(HoverInfoDisplay), "Refresh")]
public static class HoverInfoDisplayRefreshReformatPatch
{
    private static void Postfix(HoverInfoDisplay __instance)
    {
        var selectedField = AccessTools.Field(typeof(HoverInfoDisplay), "selectedInfo");
        if (selectedField == null)
            return;
        var selected = selectedField.GetValue(__instance) as HoverInfo;
        if (!StatFormatHandling.enableStatReformat || selected == null || selected.GetType().Name == "DirectiveButton")
            return;

        try
        {
            var textField = AccessTools.Field(typeof(HoverInfoDisplay), "text");
            if (textField != null)
            {
                var textComponent = (TextMeshProUGUI)textField.GetValue(__instance);
                StatFormatHandling.ReformatUIText(textComponent, "main text (refresh)");
            }

            var statsField = AccessTools.Field(typeof(HoverInfoDisplay), "statsText");
            if (statsField != null)
            {
                var statsComponent = (TextMeshProUGUI)statsField.GetValue(__instance);
                StatFormatHandling.ReformatUIText(statsComponent, "statsText (refresh)");
            }
        }
        catch (Exception ex)
        {
            UpgradeFilteringPlugin.Logger.LogWarning($"HoverInfoDisplayRefreshReformatPatch: Failed: {ex.Message}");
        }
    }
}