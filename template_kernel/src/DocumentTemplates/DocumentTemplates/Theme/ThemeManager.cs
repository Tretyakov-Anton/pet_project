using Avalonia;
using Avalonia.Media;
using System;
using Avalonia.Controls;

namespace DocumentTemplates.Theme;

public static class ThemeManager
{
    // Values should match Styles.axaml palettes.
    private static readonly Color DarkBackground = Color.Parse("#0B1220");
    private static readonly Color DarkPanelBackground = Color.Parse("#0F172A");
    private static readonly Color DarkBorder = Color.Parse("#243244");
    private static readonly Color DarkForeground = Color.Parse("#E5E7EB");
    private static readonly Color DarkSecondary = Color.Parse("#9CA3AF");

    private static readonly Color LightBackground = Color.Parse("#FFFFFF");
    private static readonly Color LightPanelBackground = Color.Parse("#F7F9FC");
    private static readonly Color LightBorder = Color.Parse("#C9D6E8");
    private static readonly Color LightForeground = Color.Parse("#1F2A3D");
    private static readonly Color LightSecondary = Color.Parse("#4C5D78");

    public static void ApplyDarkTheme()
    {
        var resources = Application.Current?.Resources;
        if (resources is null) return;

        SetColorResource(resources, "BackgroundColor", DarkBackground);
        SetColorResource(resources, "PanelBackgroundColor", DarkPanelBackground);
        SetColorResource(resources, "BorderColor", DarkBorder);
        SetColorResource(resources, "ForegroundColor", DarkForeground);
        SetColorResource(resources, "SecondaryTextColor", DarkSecondary);
    }

    public static void ApplyLightTheme()
    {
        var resources = Application.Current?.Resources;
        if (resources is null) return;

        SetColorResource(resources, "BackgroundColor", LightBackground);
        SetColorResource(resources, "PanelBackgroundColor", LightPanelBackground);
        SetColorResource(resources, "BorderColor", LightBorder);
        SetColorResource(resources, "ForegroundColor", LightForeground);
        SetColorResource(resources, "SecondaryTextColor", LightSecondary);
    }

    private static void SetColorResource(IResourceDictionary resources, string key, Color color)
    {
        // These keys are referenced by brushes via {DynamicResource}.
        // We update the underlying Color so all brushes update reliably.
        resources[key] = color;
    }
}

