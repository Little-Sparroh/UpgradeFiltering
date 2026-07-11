using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using Pigeon;

public static class FormatHandling
{
    public static bool enableStatReformat = true;

    public static void Initialize()
    {
        try
        {
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Failed to initialize FormatHandling: {ex.Message}");
        }
    }

    public static void ReformatStats(Upgrade __instance, ref string text)
    {
        if (!enableStatReformat || string.IsNullOrEmpty(text))
            return;

        try
        {
            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    sb.AppendLine();
                    continue;
                }

                string tagStripped = Regex.Replace(trimmed, @"<[^>]*>", "");
                if (Regex.IsMatch(tagStripped, @"^[-+]?\d"))
                {
                    var match = Regex.Match(tagStripped, @"^([-+]?\d+(?:\.\d+)?[%s]?)\s*(.+)$");
                    if (match.Success)
                    {
                        string value = match.Groups[1].Value;
                        string key = match.Groups[2].Value.Trim();
                        if (!string.IsNullOrEmpty(key))
                        {
                            string formatted = $"{key}: <b>{value}</b>";
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

        string currentText = textComponent.text;
        if (string.IsNullOrEmpty(currentText))
            return;

        try
        {
            string[] lines = currentText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    sb.AppendLine();
                    continue;
                }

                string tagStripped = Regex.Replace(trimmed, @"<[^>]*>", "");
                if (Regex.IsMatch(tagStripped, @"^[-+]?\d"))
                {
                    var match = Regex.Match(tagStripped, @"^([-+]?\d+(?:\.\d+)?[%s]?)\s*(.+)$");
                    if (match.Success)
                    {
                        string value = match.Groups[1].Value;
                        string key = match.Groups[2].Value.Trim();
                        if (!string.IsNullOrEmpty(key))
                        {
                            string formatted = $"{key}: <b>{value}</b>";
                            sb.AppendLine(formatted);
                            continue;
                        }
                    }
                }
                sb.AppendLine(line);
            }

            string reformattedText = sb.ToString().TrimEnd();
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
    static void Postfix(Upgrade __instance, int seed, ref string __result)
    {
        FormatHandling.ReformatStats(__instance, ref __result);
    }
}

[HarmonyPatch(typeof(HoverInfoDisplay), "Activate")]
public static class HoverInfoDisplayReformatPatch
{
    static void Postfix(HoverInfoDisplay __instance, HoverInfo info, bool resetPosition)
    {
        if (!FormatHandling.enableStatReformat || info == null || info.GetType().Name == "DirectiveButton")
            return;


        try
        {
            FieldInfo textField = AccessTools.Field(typeof(HoverInfoDisplay), "text");
            if (textField != null)
            {
                TextMeshProUGUI textComponent = (TextMeshProUGUI)textField.GetValue(__instance);
                FormatHandling.ReformatUIText(textComponent, "main text");
            }

            FieldInfo statsField = AccessTools.Field(typeof(HoverInfoDisplay), "statsText");
            if (statsField != null)
            {
                TextMeshProUGUI statsComponent = (TextMeshProUGUI)statsField.GetValue(__instance);
                FormatHandling.ReformatUIText(statsComponent, "statsText");
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogWarning($"HoverInfoDisplayReformatPatch: Failed: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(HoverInfoDisplay), "Refresh")]
public static class HoverInfoDisplayRefreshReformatPatch
{
    static void Postfix(HoverInfoDisplay __instance)
    {

        FieldInfo selectedField = AccessTools.Field(typeof(HoverInfoDisplay), "selectedInfo");
        if (selectedField == null)
            return;
        var selected = selectedField.GetValue(__instance) as HoverInfo;
        if (!FormatHandling.enableStatReformat || selected == null || selected.GetType().Name == "DirectiveButton")
            return;

        try
        {
            FieldInfo textField = AccessTools.Field(typeof(HoverInfoDisplay), "text");
            if (textField != null)
            {
                TextMeshProUGUI textComponent = (TextMeshProUGUI)textField.GetValue(__instance);
                FormatHandling.ReformatUIText(textComponent, "main text (refresh)");
            }

            FieldInfo statsField = AccessTools.Field(typeof(HoverInfoDisplay), "statsText");
            if (statsField != null)
            {
                TextMeshProUGUI statsComponent = (TextMeshProUGUI)statsField.GetValue(__instance);
                FormatHandling.ReformatUIText(statsComponent, "statsText (refresh)");
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogWarning($"HoverInfoDisplayRefreshReformatPatch: Failed: {ex.Message}");
        }
    }
}
