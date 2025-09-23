namespace MauiXaml.Utilities
{
    /// <summary>
    /// Утилиты для задач
    /// </summary>
    public static class TaskUtilities
    {
        /// <summary>
        /// Запустите и забудьте о безопасной асинхронности
        /// </summary>
        /// <param name="task">Задача для запуска и забывания</param>
        /// <param name="handler">Обработчик ошибки</param>
        public static async void FireAndForgetSafeAsync(this Task task, IErrorHandler? handler = null)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                handler?.HandleError(ex);
            }
        }
    }
}