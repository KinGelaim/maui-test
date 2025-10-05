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
            var lang = radio.Value.ToString();
            var newCulterInfo = new CultureInfo(lang);
            Translator.Instance.SetCultureInfo(newCulterInfo);
        }
    }
}