namespace MauiXaml.Services;

/// <summary>
/// Сервис обработки ошибок
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Обработать ошибку в пользовательском интерфейсе
    /// </summary>
    /// <param name="ex">Исключение, которое было выброшено</param>
    void HandleError(Exception ex);
}