using MauiXaml.Resources.Strings;
using System.ComponentModel;
using System.Globalization;

namespace MauiXaml.Data.Language;

public sealed class Translator : INotifyPropertyChanged
{
    public const string PreviousCultureInfoName = "previous_culture_info_name";

    private static Translator? _instance;
    public static Translator Instance
    {
        get
        {
            if (_instance is null)
            {
                var previousCultureName = Preferences.Default.Get(PreviousCultureInfoName, string.Empty);
                var cultureInfo = previousCultureName != string.Empty
                    ? new CultureInfo(previousCultureName)
                    : CultureInfo.InvariantCulture;

                _instance = new Translator
                {
                    CultureInfo = cultureInfo
                };
            }

            return _instance;
        }
    }

    public CultureInfo CultureInfo { get; private set; } = CultureInfo.InvariantCulture;

    public string? this[string key] => AppResources.ResourceManager.GetString(key, CultureInfo);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetCultureInfo(CultureInfo cultureInfo)
    {
        CultureInfo = cultureInfo;
        Preferences.Default.Set(PreviousCultureInfoName, cultureInfo.Name);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}