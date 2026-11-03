using Koy.Core.Settings;

namespace Koy.App.Features.Notch;

public sealed class NotchFeature(AppSettings settings) : IFeature
{
    private NotchWindow? _window;

    public void Start()
    {
        if (_window is not null)
            return;

        _window = new NotchWindow { DataContext = new NotchViewModel(settings) };
        _window.Show();
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
    }
}
