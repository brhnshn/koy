using CommunityToolkit.Mvvm.ComponentModel;
using Koy.Core.Notch;

namespace Koy.App.Features.Notch;

/// <summary>Açık çentiğin üstündeki sekme (Müzik, Kontrol, Durum, Takvim, Raf).</summary>
public sealed partial class NotchTabViewModel(NotchPanel panel, string label) : ObservableObject
{
    public NotchPanel Panel { get; } = panel;

    public string Label { get; } = label;

    [ObservableProperty]
    private bool _isSelected;
}
