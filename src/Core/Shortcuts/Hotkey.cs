using System.Globalization;

namespace Koy.Core.Shortcuts;

/// <summary>Win32 MOD_* bayraklarıyla aynı değerler (RegisterHotKey doğrudan kullanır).</summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 0x1,
    Control = 0x2,
    Shift = 0x4,
    Win = 0x8
}

/// <summary>
/// Genel (sistem çapında) klavye kısayolu: değiştirici tuşlar + sanal tuş kodu (VK_*).
/// Metin biçimi "Ctrl+Alt+N": ayarlarda saklanır, arayüzde gösterilir.
/// </summary>
public readonly record struct Hotkey(HotkeyModifiers Modifiers, int VirtualKey)
{
    private static readonly (string Name, int Vk)[] NamedKeys =
    [
        ("Space", 0x20), ("Enter", 0x0D), ("Tab", 0x09), ("Esc", 0x1B), ("Backspace", 0x08),
        ("Insert", 0x2D), ("Delete", 0x2E), ("Home", 0x24), ("End", 0x23),
        ("PageUp", 0x21), ("PageDown", 0x22),
        ("Left", 0x25), ("Up", 0x26), ("Right", 0x27), ("Down", 0x28)
    ];

    /// <summary>Ctrl, Alt ya da Win'den en az biri gerekir; yalnız Shift ya da çıplak tuş yazı yazmayı bozar.</summary>
    public bool HasPrimaryModifier =>
        (Modifiers & (HotkeyModifiers.Alt | HotkeyModifiers.Control | HotkeyModifiers.Win)) != 0;

    /// <summary>Kaydedilebilir mi: ana değiştirici var ve tuş adlandırılabilir bir tuş.</summary>
    public bool IsValid => HasPrimaryModifier && NameOf(VirtualKey) is not null;

    /// <summary>Tuşun ayarlarda yazılan adı; desteklenmeyen tuş (değiştirici tuşlar dahil) için null.</summary>
    public static string? NameOf(int vk)
    {
        if (vk is >= 0x41 and <= 0x5A)
            return ((char)vk).ToString();
        if (vk is >= 0x30 and <= 0x39)
            return ((char)vk).ToString();
        if (vk is >= 0x70 and <= 0x87)
            return "F" + (vk - 0x70 + 1).ToString(CultureInfo.InvariantCulture);

        foreach (var (name, key) in NamedKeys)
        {
            if (key == vk)
                return name;
        }

        return null;
    }

    private static int? VirtualKeyOf(string name)
    {
        if (name.Length == 1)
        {
            var c = char.ToUpperInvariant(name[0]);
            if (c is >= 'A' and <= 'Z' or >= '0' and <= '9')
                return c;
        }

        if (name.Length is 2 or 3 && name[0] is 'F' or 'f' &&
            int.TryParse(name.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n is >= 1 and <= 24)
            return 0x70 + n - 1;

        foreach (var (key, vk) in NamedKeys)
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                return vk;
        }

        return name.ToLowerInvariant() switch
        {
            "escape" => 0x1B,
            "return" => 0x0D,
            "del" => 0x2E,
            "ins" => 0x2D,
            "pgup" => 0x21,
            "pgdn" => 0x22,
            _ => null
        };
    }

    /// <summary>"Ctrl+Alt+N" gibi metni çözer. Geçersiz ya da kaydedilemeyecek bileşimlerde false döner.</summary>
    public static bool TryParse(string? text, out Hotkey hotkey)
    {
        hotkey = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var parts = text.Split('+', StringSplitOptions.TrimEntries);
        var modifiers = HotkeyModifiers.None;
        int? key = null;

        foreach (var part in parts)
        {
            if (part.Length == 0)
                return false;

            switch (part.ToLowerInvariant())
            {
                case "ctrl" or "control":
                    modifiers |= HotkeyModifiers.Control;
                    break;
                case "alt":
                    modifiers |= HotkeyModifiers.Alt;
                    break;
                case "shift":
                    modifiers |= HotkeyModifiers.Shift;
                    break;
                case "win" or "windows":
                    modifiers |= HotkeyModifiers.Win;
                    break;
                default:
                    // Tek bir ana tuş olabilir.
                    if (key is not null)
                        return false;
                    key = VirtualKeyOf(part);
                    if (key is null)
                        return false;
                    break;
            }
        }

        if (key is null)
            return false;

        var candidate = new Hotkey(modifiers, key.Value);
        if (!candidate.IsValid)
            return false;

        hotkey = candidate;
        return true;
    }

    /// <summary>Kanonik metin: Ctrl+Alt+Shift+Win+Tuş sırası.</summary>
    public string Format()
    {
        var parts = new List<string>(5);
        if ((Modifiers & HotkeyModifiers.Control) != 0) parts.Add("Ctrl");
        if ((Modifiers & HotkeyModifiers.Alt) != 0) parts.Add("Alt");
        if ((Modifiers & HotkeyModifiers.Shift) != 0) parts.Add("Shift");
        if ((Modifiers & HotkeyModifiers.Win) != 0) parts.Add("Win");
        parts.Add(NameOf(VirtualKey) ?? "0x" + VirtualKey.ToString("X2", CultureInfo.InvariantCulture));
        return string.Join('+', parts);
    }

    public override string ToString() => Format();
}
