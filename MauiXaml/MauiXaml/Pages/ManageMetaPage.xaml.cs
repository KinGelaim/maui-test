using MauiXaml.Data.Language;
using System.Globalization;

namespace MauiXaml.Pages;

public partial class ManageMetaPage : ContentPage
{
    public ManageMetaPage(ManageMetaPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }

    private void LangChanged(object sender, CheckedChangedEventArgs e)
    {
        var radio = (RadioButton)sender;
        if (radio.IsChecked)
        {
            var lang = radio.Value;
            var newCulterInfo = lang switch
            {
                "Ru" => new CultureInfo("ru-RU"),
                "En" => new CultureInfo("en-US"),
                "Fr" => new CultureInfo("fr-FR"),
                _ => CultureInfo.InvariantCulture
            };
            Translator.Instance.SetCultureInfo(newCulterInfo);
        }
    }
}