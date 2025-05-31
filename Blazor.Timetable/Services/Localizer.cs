using Blazor.Timetable.Models.Configuration;
using System.Resources;

namespace Blazor.Timetable.Services;

internal sealed class Localizer
{
    private readonly ResourceManager _resourceManager;

    public Localizer(string baseName, System.Reflection.Assembly assembly)
    {
        _resourceManager = new ResourceManager(baseName, assembly);
    }

    public string this[string key]
    {
        get => _resourceManager.GetString(key, CultureConfig.CultureInfo) ?? key;
    }

    public string GetLocalizedString(string key, params object[] args)
    {
        return string.Format(this[key], args);
    }
}
