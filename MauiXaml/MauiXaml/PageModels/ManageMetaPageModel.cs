using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiXaml.Data.Language;
using MauiXaml.Models;

namespace MauiXaml.PageModels
{
    public partial class ManageMetaPageModel : ObservableObject
    {
        private readonly CategoryRepository _categoryRepository;
        private readonly TagRepository _tagRepository;
        private readonly SeedDataService _seedDataService;

        [ObservableProperty]
        private ObservableCollection<Category> _categories = [];

        [ObservableProperty]
        private ObservableCollection<Tag> _tags = [];

        [ObservableProperty]
        private bool _ruCultureSelected = false;

        [ObservableProperty]
        private bool _enCultureSelected = false;

        [ObservableProperty]
        private bool _frCultureSelected = false;

        public ManageMetaPageModel(CategoryRepository categoryRepository, TagRepository tagRepository, SeedDataService seedDataService)
        {
            _categoryRepository = categoryRepository;
            _tagRepository = tagRepository;
            _seedDataService = seedDataService;
        }

        private async Task LoadData()
        {
            var categoriesList = await _categoryRepository.ListAsync();
            Categories = new ObservableCollection<Category>(categoriesList);

            var tagsList = await _tagRepository.ListAsync();
            Tags = new ObservableCollection<Tag>(tagsList);

            var currentCultureInfoName = Preferences.Default.Get(Translator.PreviousCultureInfoName, "ru-RU");
            RuCultureSelected = currentCultureInfoName == "ru-RU";
            EnCultureSelected = currentCultureInfoName == "en-US";
            FrCultureSelected = currentCultureInfoName == "fr-FR";
        }

        [RelayCommand]
        private Task Appearing() => LoadData();

        [RelayCommand]
        private async Task SaveCategories()
        {
            foreach (var category in Categories)
            {
                await _categoryRepository.SaveItemAsync(category);
            }

            await AppShell.DisplayToastAsync("Сохранены категории");
        }

        [RelayCommand]
        private async Task DeleteCategory(Category category)
        {
            Categories.Remove(category);
            await _categoryRepository.DeleteItemAsync(category);
            await AppShell.DisplayToastAsync("Удалена категория");
        }

        [RelayCommand]
        private async Task AddCategory()
        {
            var category = new Category();
            Categories.Add(category);
            await _categoryRepository.SaveItemAsync(category);
            await AppShell.DisplayToastAsync("Добавлена категория");
        }

        [RelayCommand]
        private async Task SaveTags()
        {
            foreach (var tag in Tags)
            {
                await _tagRepository.SaveItemAsync(tag);
            }

            await AppShell.DisplayToastAsync("Сохранены тэги");
        }

        [RelayCommand]
        private async Task DeleteTag(Tag tag)
        {
            Tags.Remove(tag);
            await _tagRepository.DeleteItemAsync(tag);
            await AppShell.DisplayToastAsync("Удалён тэг");
        }

        [RelayCommand]
        private async Task AddTag()
        {
            var tag = new Tag();
            Tags.Add(tag);
            await _tagRepository.SaveItemAsync(tag);
            await AppShell.DisplayToastAsync("Добавлен тэг");
        }

        [RelayCommand]
        private async Task Reset()
        {
            Preferences.Default.Remove("is_seeded");
            await _seedDataService.LoadSeedDataAsync();
            Preferences.Default.Set("is_seeded", true);
            await Shell.Current.GoToAsync("//main");
        }
    }
}
