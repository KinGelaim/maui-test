using MauiXaml.Models;
using MauiXaml.PageModels;

namespace MauiXaml.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}