using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;

namespace Koy.Platform.Display;

/// <summary>Bir monitörün DisplayConfig'ten okunan kararlı kimliği ve dostça adı.</summary>
internal readonly record struct DisplayConfigInfo(string DevicePath, string FriendlyName);

/// <summary>
/// QueryDisplayConfig + DisplayConfigGetDeviceInfo ile her etkin ekranın <c>monitorDevicePath</c> (kararlı kimlik) ve
/// <c>monitorFriendlyDeviceName</c> (dostça ad) bilgisini okur. Sonuç Windows aygıt adına (<c>\\.\DISPLAY2</c>) göre
/// eşlenir; çünkü EnumDisplayMonitors yalnız o adı verir. Yalnız okur, hiçbir ayarı değiştirmez.
/// </summary>
internal static class DisplayConfigReader
{
    /// <summary>GDI aygıt adı (büyük/küçük harf duyarsız) → kimlik ve ad. API hata verirse boş sözlük döner.</summary>
    public static unsafe Dictionary<string, DisplayConfigInfo> Read()
    {
        var result = new Dictionary<string, DisplayConfigInfo>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Ekran listesi sorgu ile okuma arasında değişebilir (ERROR_INSUFFICIENT_BUFFER); birkaç kez dene.
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var status = PInvoke.GetDisplayConfigBufferSizes(
                    QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS, out var pathCount, out var modeCount);
                if (status != WIN32_ERROR.ERROR_SUCCESS)
                    return result;

                var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
                var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

                fixed (DISPLAYCONFIG_PATH_INFO* pathPtr = paths)
                fixed (DISPLAYCONFIG_MODE_INFO* modePtr = modes)
                {
                    status = PInvoke.QueryDisplayConfig(
                        QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS, &pathCount, pathPtr, &modeCount, modePtr, null);
                }
                if (status == WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
                    continue;
                if (status != WIN32_ERROR.ERROR_SUCCESS)
                    return result;

                for (var i = 0; i < pathCount; i++)
                {
                    var path = paths[i];

                    var source = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
                    source.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
                    source.header.size = (uint)sizeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME);
                    source.header.adapterId = path.sourceInfo.adapterId;
                    source.header.id = path.sourceInfo.id;
                    if (PInvoke.DisplayConfigGetDeviceInfo(&source.header) != 0)
                        continue;

                    var target = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
                    target.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
                    target.header.size = (uint)sizeof(DISPLAYCONFIG_TARGET_DEVICE_NAME);
                    target.header.adapterId = path.targetInfo.adapterId;
                    target.header.id = path.targetInfo.id;
                    if (PInvoke.DisplayConfigGetDeviceInfo(&target.header) != 0)
                        continue;

                    var gdiName = source.viewGdiDeviceName.ToString().TrimEnd('\0');
                    var devicePath = target.monitorDevicePath.ToString().TrimEnd('\0');
                    var friendly = target.monitorFriendlyDeviceName.ToString().TrimEnd('\0');
                    if (gdiName.Length == 0)
                        continue;

                    result[gdiName] = new DisplayConfigInfo(devicePath, friendly);
                }

                break;
            }
        }
        catch
        {
            // Sürücü ya da API hatası: çağıran GDI adına döner (kimlik daha az kararlı olur ama uygulama çalışır).
        }

        return result;
    }
}
