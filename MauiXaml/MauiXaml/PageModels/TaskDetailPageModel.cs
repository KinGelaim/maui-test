using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiXaml.Data.Language;
using MauiXaml.Models;

namespace MauiXaml.PageModels;

public partial class TaskDetailPageModel : ObservableObject, IQueryAttributable
{
    public const string ProjectQueryKey = "project";
    private ProjectTask? _task;
    private bool _canDelete;
    private readonly ProjectRepository _projectRepository;
    private readonly TaskRepository _taskRepository;
    private readonly ModalErrorHandler _errorHandler;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private List<Project> _projects = [];

    [ObservableProperty]
    private Project? _project;

    [ObservableProperty]
    private int _selectedProjectIndex = -1;


    [ObservableProperty]
    private bool _isExistingProject;

    public TaskDetailPageModel(
        ProjectRepository projectRepository,
        TaskRepository taskRepository,
        ModalErrorHandler errorHandler)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _errorHandler = errorHandler;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        LoadTaskAsync(query).FireAndForgetSafeAsync(_errorHandler);
    }

    private async Task LoadTaskAsync(IDictionary<string, object> query)
    {
        if (query.TryGetValue(ProjectQueryKey, out var project))
            Project = (Project)project;

        int taskId = 0;

        if (query.ContainsKey("id"))
        {
            taskId = Convert.ToInt32(query["id"]);
            _task = await _taskRepository.GetAsync(taskId);

            if (_task is null)
            {
                _errorHandler.HandleError(new Exception($"Задача с идентификатором {taskId} не валидна"));
                return;
            }

            Project = await _projectRepository.GetAsync(_task.ProjectID);
        }
        else
        {
            _task = new ProjectTask();
        }

        // Если проект новый, нам не нужно загружать выпадающий список проекта
        if (Project?.ID == 0)
        {
            IsExistingProject = false;
        }
        else
        {
            Projects = await _projectRepository.ListAsync();
            IsExistingProject = true;
        }

        if (Project is not null)
        {
            SelectedProjectIndex = Projects.FindIndex(p => p.ID == Project.ID);
        }
        else if (_task?.ProjectID > 0)
        {
            SelectedProjectIndex = Projects.FindIndex(p => p.ID == _task.ProjectID);
        }

        if (taskId > 0)
        {
            if (_task is null)
            {
                _errorHandler.HandleError(new Exception($"Задача с идентификатором {taskId} не может быть найдена"));
                return;
            }

            Title = _task.Title;
            IsCompleted = _task.IsCompleted;
            CanDelete = true;
        }
        else
        {
            _task = new ProjectTask()
            {
                ProjectID = Project?.ID ?? 0
            };
        }
    }

    public bool CanDelete
    {
        get => _canDelete;
        set
        {
            _canDelete = value;
            DeleteCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (_task is null)
        {
            _errorHandler.HandleError(new Exception("Задача не может быть сохранена"));
            return;
        }

        _task.Title = Title;

        int projectId = Project?.ID ?? 0;

        if (Projects.Count > SelectedProjectIndex && SelectedProjectIndex >= 0)
        {
            _task.ProjectID = projectId = Projects[SelectedProjectIndex].ID;
        }

        _task.IsCompleted = IsCompleted;

        if (Project?.ID == projectId && !Project.Tasks.Contains(_task))
        {
            Project.Tasks.Add(_task);
        }

        if (_task.ProjectID > 0)
        {
            _taskRepository.SaveItemAsync(_task).FireAndForgetSafeAsync(_errorHandler);
        }

        await Shell.Current.GoToAsync("..?refresh=true");

        if (_task.ID > 0)
        {
            var message = Translator.Instance["TaskSavedMessage"] ?? string.Empty;
            await AppShell.DisplayToastAsync(message);
        }    
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task Delete()
    {
        if (_task is null || Project is null)
        {
            _errorHandler.HandleError(new Exception("Задача не может быть удалена"));
            return;
        }

        if (Project.Tasks.Contains(_task))
        {
            Project.Tasks.Remove(_task);
        }

        if (_task.ID > 0)
        {
            await _taskRepository.DeleteItemAsync(_task);
        }

        await Shell.Current.GoToAsync("..?refresh=true");

        var message = Translator.Instance["TaskDeletedMessage"] ?? string.Empty;
        await AppShell.DisplayToastAsync(message);
    }
}