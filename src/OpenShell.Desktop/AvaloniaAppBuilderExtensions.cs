using Avalonia.Media;
using Avalonia;
using System.Diagnostics.CodeAnalysis;
using System;
using Avalonia.Media.Fonts;

namespace OpenShell.Desktop;

public static class AvaloniaAppBuilderExtensions
{
    public static AppBuilder UseFont([DisallowNull] this AppBuilder builder, Action<FontSettings>? configDelegate = default)
    {
        var setting = new FontSettings();
        configDelegate?.Invoke(setting);

        return builder.With(new FontManagerOptions
        {
            DefaultFamilyName = setting.DefaultFontFamily,
            FontFallbacks = new[]
            {
                new FontFallback
                {
                    FontFamily = new FontFamily(setting.DefaultFontFamily)
                }
            }
        }).ConfigureFonts(manager => manager.AddFontCollection(new EmbeddedFontCollection(setting.Key, setting.Source)));
    }
}