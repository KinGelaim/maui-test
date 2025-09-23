using MauiXaml.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace MauiXaml.Data
{
    /// <summary>
    /// Класс репозитория для управления тегами в базе данных
    /// </summary>
    public class TagRepository
    {
        private bool _hasBeenInitialized = false;
        private readonly ILogger _logger;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="TagRepository"/>
        /// </summary>
        /// <param name="logger">Логгер</param>
        public TagRepository(ILogger<TagRepository> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Инициализирует подключение к базе данных, создает таблицу тэгов и таблицу соединения тэгов и проектов, если они не существуют
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
                    CREATE TABLE IF NOT EXISTS Tag (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Color TEXT NOT NULL
                    );";
                await createTableCmd.ExecuteNonQueryAsync();

                createTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ProjectsTags (
                        ProjectID INTEGER NOT NULL,
                        TagID INTEGER NOT NULL,
                        PRIMARY KEY(ProjectID, TagID)
                    );";
                await createTableCmd.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Ошибка создания таблиц тэгов");
                throw;
            }

            _hasBeenInitialized = true;
        }

        /// <summary>
        /// Извлекает список всех тегов из базы данных
        /// </summary>
        /// <returns>Список объектов класса cref="Tag"/></returns>
        public async Task<List<Tag>> ListAsync()
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Tag";
            var tags = new List<Tag>();

            await using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tags.Add(new Tag
                {
                    ID = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Color = reader.GetString(2)
                });
            }

            return tags;
        }

        /// <summary>
        /// Извлекает список тегов, связанных с конкретным проектом
        /// </summary>
        /// <param name="projectID">Идентификатор проекта</param>
        /// <returns>Список объектов класса <see cref="Tag"/></returns>
        public async Task<List<Tag>> ListAsync(int projectID)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"
                SELECT t.*
                FROM Tag t
                JOIN ProjectsTags pt ON t.ID = pt.TagID
                WHERE pt.ProjectID = @ProjectID";
            selectCmd.Parameters.AddWithValue("ProjectID", projectID);

            var tags = new List<Tag>();

            await using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tags.Add(new Tag
                {
                    ID = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Color = reader.GetString(2)
                });
            }

            return tags;
        }

        /// <summary>
        /// Извлекает определенный тег по его идентификатору
        /// </summary>
        /// <param name="id">Идентификатор тэга</param>
        /// <returns>Возвращает тэг класса <see cref="Tag"/>, если не найден, то null</returns>
        public async Task<Tag?> GetAsync(int id)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Tag WHERE ID = @id";
            selectCmd.Parameters.AddWithValue("@id", id);

            await using var reader = await selectCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Tag
                {
                    ID = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Color = reader.GetString(2)
                };
            }

            return null;
        }

        /// <summary>
        /// Сохраняет тег в базе данных. Если идентификатор тега равен 0, создается новый тег; в противном случае существующий тег обновляется
        /// </summary>
        /// <param name="item">Тэг для сохранения</param>
        /// <returns>Идентификатор сохранённого тэга</returns>
        public async Task<int> SaveItemAsync(Tag item)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var saveCmd = connection.CreateCommand();
            if (item.ID == 0)
            {
                saveCmd.CommandText = @"
                    INSERT INTO Tag (Title, Color) VALUES (@Title, @Color);
                    SELECT last_insert_rowid();";
            }
            else
            {
                saveCmd.CommandText = @"
                    UPDATE Tag SET Title = @Title, Color = @Color WHERE ID = @ID";
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
        /// Сохраняет тег в базе данных и связывает его с конкретным проектом
        /// </summary>
        /// <param name="item">Тэг для сохранения</param>
        /// <param name="projectID">Идентификатор проекта</param>
        /// <returns>Кол-во строк задетых скриптом сохранения</returns>
        public async Task<int> SaveItemAsync(Tag item, int projectID)
        {
            await Init();
            await SaveItemAsync(item);

            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var saveCmd = connection.CreateCommand();
            saveCmd.CommandText = @"
                INSERT INTO ProjectsTags (ProjectID, TagID) VALUES (@projectID, @tagID)";
            saveCmd.Parameters.AddWithValue("@projectID", projectID);
            saveCmd.Parameters.AddWithValue("@tagID", item.ID);

            return await saveCmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Удаляет тег из базы данных
        /// </summary>
        /// <param name="item">Тэг для удаления</param>
        /// <returns>Кол-во строк задетых скриптом удаления</returns>
        public async Task<int> DeleteItemAsync(Tag item)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM Tag WHERE ID = @id";
            deleteCmd.Parameters.AddWithValue("@id", item.ID);

            return await deleteCmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Удаляет тег из определенного проекта в базе данных
        /// </summary>
        /// <param name="item">Тэг для удаления</param>
        /// <param name="projectID">Идентификатор проекта</param>
        /// <returns>Кол-во строк задетых скриптом удаления</returns>
        public async Task<int> DeleteItemAsync(Tag item, int projectID)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM ProjectsTags WHERE ProjectID = @projectID AND TagID = @tagID";
            deleteCmd.Parameters.AddWithValue("@projectID", projectID);
            deleteCmd.Parameters.AddWithValue("@tagID", item.ID);

            return await deleteCmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Удаляет таблиц тэгов из базы данных
        /// </summary>
        public async Task DropTableAsync()
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var dropTableCmd = connection.CreateCommand();
            dropTableCmd.CommandText = "DROP TABLE IF EXISTS Tag";
            await dropTableCmd.ExecuteNonQueryAsync();

            dropTableCmd.CommandText = "DROP TABLE IF EXISTS ProjectsTags";
            await dropTableCmd.ExecuteNonQueryAsync();

            _hasBeenInitialized = false;
        }
    }
}