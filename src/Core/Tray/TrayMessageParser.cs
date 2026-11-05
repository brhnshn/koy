using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Koy.Core.Tray;

/// <summary>
/// Shell_NotifyIcon'un <c>Shell_TrayWnd</c> penceresine WM_COPYDATA ile gönderdiği SHELLTRAYDATA bloğunu çözer.
/// shell32 çağıranın yapısını her zaman sabit, 32 bit tanıtıcılı tam NOTIFYICONDATA düzenine çevirir;
/// bu yüzden alanlar 32 ve 64 bit uygulamalar için aynı konumdadır.
/// </summary>
public static class TrayMessageParser
{
    /// <summary>WM_COPYDATA'daki dwData değeri: tepsi mesajı.</summary>
    public const int CopyDataTrayMessage = 1;

    /// <summary>SHELLTRAYDATA başındaki sabit imza.</summary>
    public const int Signature = 0x34753423;

    // Konumlar SHELLTRAYDATA başına göredir (imza 4 + mesaj 4 = 8 bayt başlık).
    private const int OffMessage = 4;
    private const int OffHwnd = 12;
    private const int OffId = 16;
    private const int OffFlags = 20;
    private const int OffCallback = 24;
    private const int OffIcon = 28;
    private const int OffTip = 32;
    private const int TipChars = 128;
    private const int OffState = 288;
    private const int OffStateMask = 292;
    private const int OffVersion = 808;
    private const int OffGuid = 944;

    /// <summary>İpucu alanının sonuna kadar olan en küçük geçerli blok.</summary>
    public const int MinimumLength = OffTip + TipChars * 2;

    public static bool TryParse(ReadOnlySpan<byte> data, out TrayNotifyData result)
    {
        result = null!;
        if (data.Length < MinimumLength)
            return false;

        if (BinaryPrimitives.ReadInt32LittleEndian(data) != Signature)
            return false;

        var message = BinaryPrimitives.ReadUInt32LittleEndian(data[OffMessage..]);
        if (message > (uint)TrayMessage.SetVersion)
            return false;

        result = new TrayNotifyData(
            Message: (TrayMessage)message,
            OwnerHwnd: ToHandle(BinaryPrimitives.ReadUInt32LittleEndian(data[OffHwnd..])),
            Id: BinaryPrimitives.ReadUInt32LittleEndian(data[OffId..]),
            Fields: (TrayIconFields)BinaryPrimitives.ReadUInt32LittleEndian(data[OffFlags..]),
            CallbackMessage: BinaryPrimitives.ReadUInt32LittleEndian(data[OffCallback..]),
            IconHandle: ToHandle(BinaryPrimitives.ReadUInt32LittleEndian(data[OffIcon..])),
            Tooltip: ReadString(data.Slice(OffTip, TipChars * 2)),
            State: ReadUInt32OrZero(data, OffState),
            StateMask: ReadUInt32OrZero(data, OffStateMask),
            Version: ReadUInt32OrZero(data, OffVersion),
            Guid: data.Length >= OffGuid + 16 ? new Guid(data.Slice(OffGuid, 16)) : Guid.Empty);
        return true;
    }

    // 64 bit Windows'ta tanıtıcılar 32 bite sığar ve işaretli genişletilir (LongToHandle).
    private static nint ToHandle(uint value) => (nint)(int)value;

    private static uint ReadUInt32OrZero(ReadOnlySpan<byte> data, int offset) =>
        data.Length >= offset + 4 ? BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]) : 0;

    private static string ReadString(ReadOnlySpan<byte> utf16)
    {
        var chars = MemoryMarshal.Cast<byte, char>(utf16);
        var end = chars.IndexOf('\0');
        return (end >= 0 ? chars[..end] : chars).ToString();
    }
}
