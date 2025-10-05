using CommunityToolkit.Mvvm.Input;
using MauiXaml.Models;

namespace MauiXaml.PageModels;

public interface IProjectTaskPageModel
{
    IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
    bool IsBusy { get; }
}