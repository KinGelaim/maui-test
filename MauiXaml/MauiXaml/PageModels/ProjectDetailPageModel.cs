using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiXaml.Data.Language;
using MauiXaml.Models;

namespace MauiXaml.PageModels;

public partial class ProjectDetailPageModel : ObservableObject, IQueryAttributable, IProjectTaskPageModel
{
    private Project? _project;
    private readonly ProjectRepository _projectRepository;
    private readonly TaskRepository _taskRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly TagRepository _tagRepository;
    private readonly ModalErrorHandler _errorHandler;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private List<ProjectTask> _tasks = [];

    [ObservableProperty]
    private List<Category> _categories = [];

    [ObservableProperty]
    private Category? _category;

    [ObservableProperty]
    private int _categoryIndex = -1;

    [ObservableProperty]
    private List<Tag> _allTags = [];

    [ObservableProperty]
    private IconData _icon;

    [ObservableProperty]
    bool _isBusy;

    [ObservableProperty]
    private List<IconData> _icons = new List<IconData>
    {
        new IconData { Icon = FluentUI.ribbon_24_regular, Description = "Ribbon Icon" },
        new IconData { Icon = FluentUI.ribbon_star_24_regular, Description = "Ribbon Star Icon" },
        new IconData { Icon = FluentUI.trophy_24_regular, Description = "Trophy Icon" },
        new IconData { Icon = FluentUI.badge_24_regular, Description = "Badge Icon" },
        new IconData { Icon = FluentUI.book_24_regular, Description = "Book Icon" },
        new IconData { Icon = FluentUI.people_24_regular, Description = "People Icon" },
        new IconData { Icon = FluentUI.bot_24_regular, Description = "Bot Icon" }
    };

    public bool HasCompletedTasks
        => _project?.Tasks.Any(t => t.IsCompleted) ?? false;

    public ProjectDetailPageModel(
        ProjectRepository projectRepository,
        TaskRepository taskRepository,
        CategoryRepository categoryRepository,
        TagRepository tagRepository,
        ModalErrorHandler errorHandler)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
        _errorHandler = errorHandler;

        var firstIcon = _icons.First();
        _icon = new IconData()
        {
            Icon = firstIcon.Icon,
            Description = firstIcon.Description
        };
        Tasks = [];
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("id"))
        {
            int id = Convert.ToInt32(query["id"]);
            LoadData(id).FireAndForgetSafeAsync(_errorHandler);
        }
        else if (query.ContainsKey("refresh"))
        {
            RefreshData().FireAndForgetSafeAsync(_errorHandler);
        }
        else
        {
            Task.WhenAll(LoadCategories(), LoadTags()).FireAndForgetSafeAsync(_errorHandler);
            _project = new()
            {
                Tags = [],
                Tasks = []
            };
            Tasks = _project.Tasks;
        }
    }

    private async Task LoadCategories() =>
        Categories = await _categoryRepository.ListAsync();

    private async Task LoadTags() =>
        AllTags = await _tagRepository.ListAsync();

    private async Task RefreshData()
    {
        if (_project.IsNullOrNew())
        {
            if (_project is not null)
            {
                Tasks = [.. _project.Tasks];
            }

            return;
        }

        Tasks = await _taskRepository.ListAsync(_project.ID);
        _project.Tasks = Tasks;
    }

    private async Task LoadData(int id)
    {
        try
        {
            IsBusy = true;

            _project = await _projectRepository.GetAsync(id);

            if (_project.IsNullOrNew())
            {
                _errorHandler.HandleError(new Exception($"Проект с идентификатором {id} не найден"));
                return;
            }

            Name = _project.Name;
            Description = _project.Description;
            Tasks = _project.Tasks;

            Icon.Icon = _project.Icon;

            Categories = await _categoryRepository.ListAsync();
            Category = Categories?.FirstOrDefault(c => c.ID == _project.CategoryID);
            CategoryIndex = Categories?.FindIndex(c => c.ID == _project.CategoryID) ?? -1;

            var allTags = await _tagRepository.ListAsync();
            foreach (var tag in allTags)
            {
                tag.IsSelected = _project.Tags.Any(t => t.ID == tag.ID);
            }
            AllTags = [.. allTags];
        }
        catch (Exception e)
        {
            _errorHandler.HandleError(e);
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(HasCompletedTasks));
        }
    }

    [RelayCommand]
    private async Task TaskCompleted(ProjectTask task)
    {
        await _taskRepository.SaveItemAsync(task);
        OnPropertyChanged(nameof(HasCompletedTasks));
    }


    [RelayCommand]
    private async Task Save()
    {
        if (_project is null)
        {
            _errorHandler.HandleError(new Exception("Проект не может быть сохранён"));
            return;
        }

        _project.Name = Name;
        _project.Description = Description;
        _project.CategoryID = Category?.ID ?? 0;
        _project.Icon = Icon.Icon ?? FluentUI.ribbon_24_regular;
        await _projectRepository.SaveItemAsync(_project);

        if (_project.IsNullOrNew())
        {
            foreach (var tag in AllTags)
            {
                if (tag.IsSelected)
                {
                    await _tagRepository.SaveItemAsync(tag, _project.ID);
                }
            }
        }

        foreach (var task in _project.Tasks)
        {
            if (task.ID == 0)
            {
                task.ProjectID = _project.ID;
                await _taskRepository.SaveItemAsync(task);
            }
        }

        await Shell.Current.GoToAsync("..");

        var message = Translator.Instance["ProjectSavedMessage"] ?? string.Empty;
        await AppShell.DisplayToastAsync(message);
    }

    [RelayCommand]
    private async Task AddTask()
    {
        if (_project is null)
        {
            _errorHandler.HandleError(new Exception("Нельзя создать задачу, т.к. не найден проект"));
            return;
        }

        // Передаём проект, чтобы, если это новый проект
        // можно было просто добавить задачи в проект, а затем сохранить их все отсюда
        await Shell.Current.GoToAsync($"task",
            new ShellNavigationQueryParameters(){
                {TaskDetailPageModel.ProjectQueryKey, _project}
            });
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (_project.IsNullOrNew())
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        await _projectRepository.DeleteItemAsync(_project);
        await Shell.Current.GoToAsync("..");

        var message = Translator.Instance["ProjectDeletedMessage"] ?? string.Empty;
        await AppShell.DisplayToastAsync(message);
    }

    [RelayCommand]
    private Task NavigateToTask(ProjectTask task) =>
        Shell.Current.GoToAsync($"task?id={task.ID}");

    [RelayCommand]
    private async Task ToggleTag(Tag tag)
    {
        tag.IsSelected = !tag.IsSelected;

        if (!_project.IsNullOrNew())
        {
            if (tag.IsSelected)
            {
                await _tagRepository.SaveItemAsync(tag, _project.ID);
            }
            else
            {
                await _tagRepository.DeleteItemAsync(tag, _project.ID);
            }
        }

        AllTags = [.. AllTags];
    }

    [RelayCommand]
    private async Task CleanTasks()
    {
        var completedTasks = Tasks.Where(t => t.IsCompleted).ToArray();
        foreach (var task in completedTasks)
        {
            await _taskRepository.DeleteItemAsync(task);
            Tasks.Remove(task);
        }

        Tasks = [.. Tasks];
        OnPropertyChanged(nameof(HasCompletedTasks));

        var message = Translator.Instance["EverythingIsClearedMessage"] ?? string.Empty;
        await AppShell.DisplayToastAsync(message);
    }
}
