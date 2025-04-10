using System;

namespace OpenShell.Desktop;

public class FontSettings
{
    public string DefaultFontFamily = "fonts:cc#Cascadia Code";
    public Uri Key { get; set; } = new Uri("fonts:cc", UriKind.Absolute);
    public Uri Source { get; set; } = new Uri("avares://OpenShell.Desktop/Assets/Font/CascadiaCode.ttf", UriKind.Absolute);
}