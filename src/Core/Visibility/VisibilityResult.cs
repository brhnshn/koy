namespace Koy.Core.Visibility;

/// <summary>
/// Görünürlük durumunun sonucu: görünürlük ve tıklama geçirgenlik durumu.
/// </summary>
public readonly record struct VisibilityResult(bool IsVisible, bool IsClickThrough);
