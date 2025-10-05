using MauiXaml.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace MauiXaml.Data;

/// <summary>
/// Класс репозитория для управления проектами в базе данных
/// </summary>
public class ProjectRepository
{
    private bool _hasBeenInitialized = false;

    private readonly ILogger _logger;
    private readonly TaskRepository _taskRepository;
    private readonly TagRepository _tagRepository;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ProjectRepository"/>
    /// </summary>
    /// <param name="taskRepository">Репозиторий задач</param>
    /// <param name="tagRepository">Репозиторий тэгов</param>
    /// <param name="logger">Логгер</param>
    public ProjectRepository(TaskRepository taskRepository, TagRepository tagRepository, ILogger<ProjectRepository> logger)
    {
        _taskRepository = taskRepository;
        _tagRepository = tagRepository;
        _logger = logger;
    }

    /// <summary>
    /// Инициализирует подключение к базе данных и создает таблицу проекта, если она не существует
    /// </summary>
    private async Task Init()
    {
        if (_hasBeenInitialized)
        {
            return;
        }

        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        try
        {
            var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Project (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Description TEXT NOT NULL,
                        Icon TEXT NOT NULL,
                        CategoryID INTEGER NOT NULL
                    );";
            await createTableCmd.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ошибка при создании таблицы проектов");
            throw;
        }

        _hasBeenInitialized = true;
    }

    /// <summary>
    /// Извлекает список всех проектов из базы данных
    /// </summary>
    /// <returns>Список объектов класса <see cref="Project"/></returns>
    public async Task<List<Project>> ListAsync()
    {
        await Init();

        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT * FROM Project";
        var projects = new List<Project>();

        await using var reader = await selectCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            projects.Add(new Project
            {
                ID = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                Icon = reader.GetString(3),
                CategoryID = reader.GetInt32(4)
            });
        }

        foreach (var project in projects)
        {
            project.Tags = await _tagRepository.ListAsync(project.ID);
            project.Tasks = await _taskRepository.ListAsync(project.ID);
        }

        return projects;
    }

    /// <summary>
    /// Извлекает конкретный проект по его идентификатору
    /// </summary>
    /// <param name="id">Идентификатор проекта</param>
    /// <returns>Возвращает проект класса <see cref="Project"/>, если не найден, то null</returns>
    public async Task<Project?> GetAsync(int id)
    {
        await Init();

        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT * FROM Project WHERE ID = @id";
        selectCmd.Parameters.AddWithValue("@id", id);

        await using var reader = await selectCmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var project = new Project
            {
                ID = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                Icon = reader.GetString(3),
                CategoryID = reader.GetInt32(4)
            };

            project.Tags = await _tagRepository.ListAsync(project.ID);
            project.Tasks = await _taskRepository.ListAsync(project.ID);

            return project;
        }

        return null;
    }

    /// <summary>
    /// Сохраняет проект в базе данных. Если идентификатор проекта равен 0, создается новый проект; в противном случае обновляется существующий проект
    /// </summary>
    /// <param name="item">Проект для сохранения</param>
    /// <returns>Идентификатор сохранённого проекта</returns>
    public async Task<int> SaveItemAsync(Project item)
    {
        await Init();

        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var saveCmd = connection.CreateCommand();
        if (item.ID == 0)
        {
            saveCmd.CommandText = @"
                    INSERT INTO Project (Name, Description, Icon, CategoryID)
                    VALUES (@Name, @Description, @Icon, @CategoryID);
                    SELECT last_insert_rowid();";
        }
        else
        {
            saveCmd.CommandText = @"
                    UPDATE Project
                    SET Name = @Name, Description = @Description, Icon = @Icon, CategoryID = @CategoryID
                    WHERE ID = @ID";
            saveCmd.Parameters.AddWithValue("@ID", item.ID);
        }

        saveCmd.Parameters.AddWithValue("@Name", item.Name);
        saveCmd.Parameters.AddWithValue("@Description", item.Description);
        saveCmd.Parameters.AddWithValue("@Icon", item.Icon);
        saveCmd.Parameters.AddWithValue("@CategoryID", item.CategoryID);

        var result = await saveCmd.ExecuteScalarAsync();
        if (item.ID == 0)
        {
            item.ID = Convert.ToInt32(result);
        }

        return item.ID;
    }

    /// <summary>
    /// Удаляет проект из базы данных
    /// </summary>
    /// <param name="item">Проект для удаления</param>
    /// <returns>Кол-во строк задетых скриптом удаления</returns>
    public async Task<int> DeleteItemAsync(Project item)
    {
        await Init();

        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var deleteCmd = connection.CreateCommand();
        deleteCmd.CommandText = "DELETE FROM Project WHERE ID = @ID";
        deleteCmd.Parameters.AddWithValue("@ID", item.ID);

        return await deleteCmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Удаляет таблицу проекта из базы данных
    /// </summary>
    public async Task DropTableAsync()
    {
        await Init();

        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var dropCmd = connection.CreateCommand();
        dropCmd.CommandText = "DROP TABLE IF EXISTS Project";
        await dropCmd.ExecuteNonQueryAsync();

        await _taskRepository.DropTableAsync();
        await _tagRepository.DropTableAsync();
        _hasBeenInitialized = false;
    }
}