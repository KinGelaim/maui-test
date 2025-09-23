using MauiXaml.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace MauiXaml.Data
{
    /// <summary>
    /// Класс репозитория для управления задачами в базе данных
    /// </summary>
    public class TaskRepository
    {
        private bool _hasBeenInitialized = false;
        private readonly ILogger _logger;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="TaskRepository"/>
        /// </summary>
        /// <param name="logger">Логгер</param>
        public TaskRepository(ILogger<TaskRepository> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Инициализирует подключение к базе данных и создает таблицу задач, если она не существует
        /// </summary>
        private async Task Init()
        {
            if (_hasBeenInitialized)
                return;

            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            try
            {
                var createTableCmd = connection.CreateCommand();
                createTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Task (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        IsCompleted INTEGER NOT NULL,
                        ProjectID INTEGER NOT NULL
                    );";
                await createTableCmd.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Ошибка при создании таблицы задач");
                throw;
            }

            _hasBeenInitialized = true;
        }

        /// <summary>
        /// Извлекает список всех задач из базы данных
        /// </summary>
        /// <returns>Список объектов класса <see cref="ProjectTask"/></returns>
        public async Task<List<ProjectTask>> ListAsync()
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Task";
            var tasks = new List<ProjectTask>();

            await using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tasks.Add(new ProjectTask
                {
                    ID = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    IsCompleted = reader.GetBoolean(2),
                    ProjectID = reader.GetInt32(3)
                });
            }

            return tasks;
        }

        /// <summary>
        /// Извлекает список задач, связанных с конкретным проектом
        /// </summary>
        /// <param name="projectId">Идентификатор проекта</param>
        /// <returns>Список объектов класса <see cref="ProjectTask"/></returns>
        public async Task<List<ProjectTask>> ListAsync(int projectId)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Task WHERE ProjectID = @projectId";
            selectCmd.Parameters.AddWithValue("@projectId", projectId);
            var tasks = new List<ProjectTask>();

            await using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tasks.Add(new ProjectTask
                {
                    ID = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    IsCompleted = reader.GetBoolean(2),
                    ProjectID = reader.GetInt32(3)
                });
            }

            return tasks;
        }

        /// <summary>
        /// Извлекает конкретную задачу по ее идентификатору
        /// </summary>
        /// <param name="id">Идентификатор задачи</param>
        /// <returns>Возвращает задачу класса <see cref="ProjectTask"/>, если не найдена, то null</returns>
        public async Task<ProjectTask?> GetAsync(int id)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Task WHERE ID = @id";
            selectCmd.Parameters.AddWithValue("@id", id);

            await using var reader = await selectCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ProjectTask
                {
                    ID = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    IsCompleted = reader.GetBoolean(2),
                    ProjectID = reader.GetInt32(3)
                };
            }

            return null;
        }

        /// <summary>
        /// Сохраняет задачу в базе данных. Если идентификатор задачи равен 0, создается новая задача; в противном случае обновляется существующая задача
        /// </summary>
        /// <param name="item">Задача для сохранения</param>
        /// <returns>Идентификатор сохранённой задачи</returns>
        public async Task<int> SaveItemAsync(ProjectTask item)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var saveCmd = connection.CreateCommand();
            if (item.ID == 0)
            {
                saveCmd.CommandText = @"
                    INSERT INTO Task (Title, IsCompleted, ProjectID) VALUES (@title, @isCompleted, @projectId);
                    SELECT last_insert_rowid();";
            }
            else
            {
                saveCmd.CommandText = @"
                    UPDATE Task SET Title = @title, IsCompleted = @isCompleted, ProjectID = @projectId WHERE ID = @id";
                saveCmd.Parameters.AddWithValue("@id", item.ID);
            }

            saveCmd.Parameters.AddWithValue("@title", item.Title);
            saveCmd.Parameters.AddWithValue("@isCompleted", item.IsCompleted);
            saveCmd.Parameters.AddWithValue("@projectId", item.ProjectID);

            var result = await saveCmd.ExecuteScalarAsync();
            if (item.ID == 0)
            {
                item.ID = Convert.ToInt32(result);
            }

            return item.ID;
        }

        /// <summary>
        /// Удаляет задачу из базы данных
        /// </summary>
        /// <param name="item">Задача для удаления</param>
        /// <returns>Кол-во строк задетых скриптом удаления</returns>
        public async Task<int> DeleteItemAsync(ProjectTask item)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM Task WHERE ID = @id";
            deleteCmd.Parameters.AddWithValue("@id", item.ID);

            return await deleteCmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Удаляет таблицу задач из базы данных
        /// </summary>
        public async Task DropTableAsync()
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var dropTableCmd = connection.CreateCommand();
            dropTableCmd.CommandText = "DROP TABLE IF EXISTS Task";
            await dropTableCmd.ExecuteNonQueryAsync();
            _hasBeenInitialized = false;
        }
    }
}