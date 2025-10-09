namespace MauiLocalNotification.Model;

internal sealed class NotificationEventArgs : EventArgs
{
    public string Title { get; set; }
    public string Message { get; set; }
}