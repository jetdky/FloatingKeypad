namespace FloatingKeypad.Models;

public sealed class AppearanceConfig
{
    public const double DefaultWidth = 120;

    public const double DefaultHeight = 48;

    public const double DefaultOpacity = 0.88;

    public const string DefaultBackground = "#CC2D7FF9";

    public const string DefaultForeground = "#FFFFFFFF";

    public double Width { get; set; } = DefaultWidth;

    public double Height { get; set; } = DefaultHeight;

    public double Opacity { get; set; } = DefaultOpacity;

    public string Background { get; set; } = DefaultBackground;

    public string Foreground { get; set; } = DefaultForeground;

    public void Reset()
    {
        Width = DefaultWidth;
        Height = DefaultHeight;
        Opacity = DefaultOpacity;
        Background = DefaultBackground;
        Foreground = DefaultForeground;
    }
}
