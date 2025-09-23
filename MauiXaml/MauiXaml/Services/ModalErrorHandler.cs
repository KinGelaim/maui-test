namespace MauiXaml.Services
{
    /// <summary>
    /// Модальный обработчик ошибок
    /// </summary>
    public class ModalErrorHandler : IErrorHandler
    {
        SemaphoreSlim _semaphore = new(1, 1);

        /// <inheritdoc/>
        public void HandleError(Exception ex)
        {
            DisplayAlert(ex).FireAndForgetSafeAsync();
        }

        async Task DisplayAlert(Exception ex)
        {
            try
            {
                await _semaphore.WaitAsync();
                if (Shell.Current is Shell shell)
                    await shell.DisplayAlert("Ошибка", ex.Message, "ОК");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}