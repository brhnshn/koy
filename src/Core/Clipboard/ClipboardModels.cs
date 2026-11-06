namespace Koy.Core.Clipboard;

/// <summary>Pano öğesinin türü (sınıflandırma: <see cref="ClipboardClassifier"/>).</summary>
public enum ClipboardKind
{
    Text,
    Link,
    Color,
    Code
}

/// <summary>Arayüzdeki süzgeç sekmeleri: Tümü / Sabit / Bağlantı / Renk / Kod.</summary>
public enum ClipboardFilter
{
    All,
    Pinned,
    Link,
    Color,
    Code
}

/// <summary>Tek pano kaydı. Değişmez; sabitleme ve taşıma yeni kayıt üretir.</summary>
public sealed record ClipboardItem(Guid Id, string Text, ClipboardKind Kind, bool Pinned, DateTimeOffset CopiedAt);
