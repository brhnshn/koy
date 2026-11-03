namespace Koy.Core.Visibility;

public enum VisibilityMode
{
    /// <summary>Her zaman görünür. Dock ekranda yer ayırır.</summary>
    Fixed,

    /// <summary>Üstüne bir pencere gelince gizlenir, fare gelince çıkar.</summary>
    Smart,

    /// <summary>Fare ekran kenarına gelene ya da bir medya olayı olana kadar gizlidir.</summary>
    Peek,
}
