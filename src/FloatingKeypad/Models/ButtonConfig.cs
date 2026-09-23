using System.Text.Json.Serialization;
using FloatingKeypad.Services;

namespace FloatingKeypad.Models;

public sealed class ButtonConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Label { get; set; } = "按钮";

    public List<InputEvent> Events { get; set; } = new();

    public double Left { get; set; } = 300;

    public double Top { get; set; } = 300;

    [JsonIgnore]
    public string Display => Describe();

    public string Describe()
    {
        if (Events.Count == 0)
        {
            return Localization.T("NotSet");
        }

        var mods = new List<string>();
        var actions = new List<string>();
        foreach (var e in Events)
        {
            switch (e)
            {
                case KeyEvent k when k.Down:
                    if (KeyNames.IsModifier(k.Vk))
                    {
                        mods.Add(KeyNames.Get(k.Vk));
                    }
                    else
                    {
                        actions.Add(KeyNames.Get(k.Vk));
                    }
                    break;
                case MouseEvent m:
                    actions.Add(KeyNames.Describe(m));
                    break;
            }
        }

        var parts = mods.Concat(actions).ToList();
        return parts.Count == 0 ? Localization.T("NotSet") : string.Join(" + ", parts);
    }
}
