using MauiXaml.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace MauiXaml.Data;

/// <summary>
/// Класс репозитория для управления категориями в базе данных
/// </summary>
public class CategoryRepository
{
    private bool _hasBeenInitialized = false;

    private readonly ILogger _logger;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="CategoryRepository"/>
    /// </summary>
    /// <param name="logger">Логгер</param>
    public CategoryRepository(ILogger<CategoryRepository> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Инициализирует подключение к базе данных и создает таблицу категорий, если она не существует
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
                    CREATE TABLE IF NOT EXISTS Category (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Color TEXT NOT NULL
                    );";
            await createTableCmd.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ошибка создания таблицы категорий");
            throw;
        }

        _hasBeenInitialized = true;
    }

    /// <summary>
    /// Извлекает список всех категорий из базы данных
    /// </summary>
    /// <returns>Список объектов класса <see cref="Category"/></returns>
    public async Task<List<Category>> ListAsync()
    {
        await Init();

        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT * FROM Category";
        var categories = new List<Category>();

        await using var reader = await selectCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            categories.Add(new Category
            {
                ID = reader.GetInt32(0),
                Title = reader.GetString(1),
                Color = reader.GetString(2)
            });
        }

        return categories;
    }

    /// <summary>
    /// Извлекает определенную категорию по ее идентификатору
    /// </summary>
    /// <param name="id">Идентификатор категории</param>
    /// <returns>Возвращает категорию класса <see cref="Category"/>, если не найдена, то null</returns>
    public async Task<Category?> GetAsync(int id)
    {
        await Init();

        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT * FROM Category WHERE ID = @id";
        selectCmd.Parameters.AddWithValue("@id", id);

        await using var reader = await selectCmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Category
            {
                ID = reader.GetInt32(0),
                Title = reader.GetString(1),
                Color = reader.GetString(2)
            };
        }

        return null;
    }

    /// <summary>
    /// Сохраняет категорию в базе данных. Если идентификатор категории равен 0, создается новая категория; в противном случае существующая категория обновляется
    /// </summary>
    /// <param name="item">Категория для сохранения</param>
    /// <returns>Идентификатор сохранённой категории</returns>
    public async Task<int> SaveItemAsync(Category item)
    {
        await Init();

        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var saveCmd = connection.CreateCommand();
        if (item.ID == 0)
        {
            saveCmd.CommandText = @"
                    INSERT INTO Category (Title, Color)
                    VALUES (@Title, @Color);
                    SELECT last_insert_rowid();";
        }
        else
        {
            saveCmd.CommandText = @"
                    UPDATE Category SET Title = @Title, Color = @Color
                    WHERE ID = @ID";
            saveCmd.Parameters.AddWithValue("@ID", item.ID);
        }

        saveCmd.Parameters.AddWithValue("@Title", item.Title);
        saveCmd.Parameters.AddWithValue("@Color", item.Color);

        var result = await saveCmd.ExecuteScalarAsync();
        if (item.ID == 0)
        {
            item.ID = Convert.ToInt32(result);
        }

        return item.ID;
    }

    /// <summary>
    /// Удаляет категорию из базы данных
    /// </summary>
    /// <param name="item">Категория для удаления</param>
    /// <returns>Кол-во строк задетых скриптом удаления</returns>
    public async Task<int> DeleteItemAsync(Category item)
    {
        await Init();

        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var deleteCmd = connection.CreateCommand();
        deleteCmd.CommandText = "DELETE FROM Category WHERE ID = @id";
        deleteCmd.Parameters.AddWithValue("@id", item.ID);

        return await deleteCmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Удаляет таблицу категорий из базы данных
    /// </summary>
    public async Task DropTableAsync()
    {
        await Init();

        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();

        var dropTableCmd = connection.CreateCommand();
        dropTableCmd.CommandText = "DROP TABLE IF EXISTS Category";

        await dropTableCmd.ExecuteNonQueryAsync();
        _hasBeenInitialized = false;
    }
}