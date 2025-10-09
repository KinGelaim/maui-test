using Android.Content;
using MauiLocalNotification.Platforms.Android.Services;

namespace MauiLocalNotification.Platforms.Android.Handlers;

[BroadcastReceiver(Enabled = true, Label = "Приемник широковещательной передачи локальных уведомлений")]
internal sealed class AlarmHandler : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if ((intent?.Extras) == null)
        {
            return;
        }

        string title = intent.GetStringExtra(NotificationManagerService.TitleKey);
        string message = intent.GetStringExtra(NotificationManagerService.MessageKey);

        NotificationManagerService manager = NotificationManagerService.Instance ?? new NotificationManagerService();
        manager.Show(title, message);
    }
}