namespace Koy.Core.Tray;

/// <summary>
/// <see cref="TrayIconStore.Apply"/> sonucu. <see cref="ReleasedIcons"/> artık kullanılmayan simge tanıtıcılarıdır;
/// sahibi (Platform) bunları yok etmelidir.
/// </summary>
public sealed record TrayApplyResult(bool Success, IReadOnlyList<nint> ReleasedIcons)
{
    public static TrayApplyResult Ok(params nint[] released) => new(true, Filter(released));
    public static TrayApplyResult Fail(params nint[] released) => new(false, Filter(released));

    private static nint[] Filter(nint[] handles) => Array.FindAll(handles, h => h != 0);
}

/// <summary>
/// Tepsi simgelerinin listesi. Shell_NotifyIcon kurallarını izler; simgeler ekleniş sırasını korur.
/// Simge tanıtıcılarının sahibi çağırandır: depodan çıkan her tanıtıcı sonuçta geri verilir.
/// </summary>
public sealed class TrayIconStore
{
    /// <summary>
    /// Aynı anda tutulan en çok simge. Her kayıt bir simge kopyası (GDI nesnesi) tutar; hatalı ya da kötü niyetli
    /// bir uygulama sınırsız kayıt ekleyip belleği büyütemesin. Gerçek kullanımda simge sayısı onlarla ölçülür.
    /// </summary>
    public const int MaxIcons = 256;

    private const uint StateHidden = 0x1;
    private const uint DefaultVersion = 0;

    private readonly List<TrayIcon> _icons = [];

    public IReadOnlyList<TrayIcon> All => _icons;

    /// <summary>Gösterilecek simgeler, ekleniş sırasıyla.</summary>
    public IReadOnlyList<TrayIcon> Visible => _icons.FindAll(i => i.IsVisible);

    public TrayApplyResult Apply(TrayNotifyData data)
    {
        var key = TrayIconKey.From(data);
        var index = _icons.FindIndex(i => i.Key == key);
        var incomingIcon = (data.Fields & TrayIconFields.Icon) != 0 ? data.IconHandle : 0;

        switch (data.Message)
        {
            case TrayMessage.Add:
                if (index >= 0)
                {
                    // Koy tepsiyi devraldığında "TaskbarCreated" yayınıyla uygulamalar simgelerini yeniden ekler;
                    // zaten bilinen simgeyi güncelleme gibi ele almak, uygulamaya boşuna hata döndürmekten iyidir.
                    return Update(index, data, incomingIcon);
                }

                if (_icons.Count >= MaxIcons)
                    return TrayApplyResult.Fail(incomingIcon);

                _icons.Add(new TrayIcon(
                    Key: key,
                    OwnerHwnd: data.OwnerHwnd,
                    Id: data.Id,
                    CallbackMessage: (data.Fields & TrayIconFields.Message) != 0 ? data.CallbackMessage : 0,
                    IconHandle: incomingIcon,
                    Tooltip: (data.Fields & TrayIconFields.Tip) != 0 ? data.Tooltip : string.Empty,
                    IsHidden: ApplyHidden(false, data),
                    Version: DefaultVersion));
                return TrayApplyResult.Ok();

            case TrayMessage.Modify:
                return index >= 0 ? Update(index, data, incomingIcon) : TrayApplyResult.Fail(incomingIcon);

            case TrayMessage.Delete:
                if (index < 0)
                    return TrayApplyResult.Fail(incomingIcon);

                var removed = _icons[index];
                _icons.RemoveAt(index);
                return TrayApplyResult.Ok(removed.IconHandle, incomingIcon);

            case TrayMessage.SetVersion:
                if (index < 0)
                    return TrayApplyResult.Fail(incomingIcon);

                _icons[index] = _icons[index] with { Version = data.Version };
                return TrayApplyResult.Ok(incomingIcon);

            case TrayMessage.SetFocus:
                return new TrayApplyResult(index >= 0, incomingIcon != 0 ? [incomingIcon] : []);

            default:
                return TrayApplyResult.Fail(incomingIcon);
        }
    }

    /// <summary>
    /// Sahibi kapanmış (NIM_DELETE göndermeden çöken) uygulamaların simgelerini temizler.
    /// </summary>
    public IReadOnlyList<nint> RemoveDead(Func<nint, bool> isWindowAlive)
    {
        var released = new List<nint>();
        _icons.RemoveAll(icon =>
        {
            if (isWindowAlive(icon.OwnerHwnd))
                return false;

            if (icon.IconHandle != 0)
                released.Add(icon.IconHandle);
            return true;
        });
        return released;
    }

    /// <summary>Bütün simgeleri bırakır (tepsi kapanırken).</summary>
    public IReadOnlyList<nint> Clear()
    {
        var released = _icons.Where(i => i.IconHandle != 0).Select(i => i.IconHandle).ToList();
        _icons.Clear();
        return released;
    }

    private TrayApplyResult Update(int index, TrayNotifyData data, nint incomingIcon)
    {
        var current = _icons[index];
        nint released = 0;

        var iconHandle = current.IconHandle;
        if ((data.Fields & TrayIconFields.Icon) != 0)
        {
            released = current.IconHandle;
            iconHandle = incomingIcon;
        }

        _icons[index] = current with
        {
            // Pencere değişebilir (GUID'li simgede uygulama yeni pencereyle kaydolur); tıklamalar yeni pencereye gitmeli.
            OwnerHwnd = data.OwnerHwnd != 0 ? data.OwnerHwnd : current.OwnerHwnd,
            Id = data.Id,
            CallbackMessage = (data.Fields & TrayIconFields.Message) != 0 ? data.CallbackMessage : current.CallbackMessage,
            IconHandle = iconHandle,
            Tooltip = (data.Fields & TrayIconFields.Tip) != 0 ? data.Tooltip : current.Tooltip,
            IsHidden = ApplyHidden(current.IsHidden, data)
        };

        return TrayApplyResult.Ok(released == iconHandle ? 0 : released);
    }

    private static bool ApplyHidden(bool current, TrayNotifyData data)
    {
        if ((data.Fields & TrayIconFields.State) == 0 || (data.StateMask & StateHidden) == 0)
            return current;

        return (data.State & StateHidden) != 0;
    }
}
