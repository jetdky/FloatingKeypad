using System.Text.Json.Serialization;

namespace FloatingKeypad.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(KeyEvent), "key")]
[JsonDerivedType(typeof(MouseEvent), "mouse")]
public abstract class InputEvent
{
}

public sealed class KeyEvent : InputEvent
{
    public ushort Vk { get; set; }
    public bool Extended { get; set; }
    public bool Down { get; set; }
}

public enum MouseButton
{
    Left,
    Right,
    Middle,
    X1,
    X2
}

public enum MouseAction
{
    Down,
    Up,
    Click,
    WheelUp,
    WheelDown,
    WheelLeft,
    WheelRight
}

public sealed class MouseEvent : InputEvent
{
    public MouseButton Button { get; set; }
    public MouseAction Action { get; set; } = MouseAction.Click;
}
