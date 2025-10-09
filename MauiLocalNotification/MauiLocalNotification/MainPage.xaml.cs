#if ANDROID
using MauiLocalNotification.Platforms.Android.Permissions;
#endif

using MauiLocalNotification.Model;
using MauiLocalNotification.Services;

namespace MauiLocalNotification;

public partial class MainPage : ContentPage
{
    private int count = 0;

    private readonly INotificationManagerService notificationManager;
    private int notificationNumber = 0;

    public MainPage(INotificationManagerService manager)
    {
        InitializeComponent();

        notificationManager = manager;
        notificationManager.NotificationReceived += (sender, eventArgs) =>
        {
            var eventData = (NotificationEventArgs)eventArgs;
            ShowNotification(eventData.Title, eventData.Message);
        };
    }

#if ANDROID
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.RequestAsync<NotificationPermission>();
    }
#endif

    void OnInstalSendClick(object sender, EventArgs e)
    {
        notificationNumber++;
        string title = $"Локальное уведомление №{notificationNumber}";
        string message = $"Вы получили {notificationNumber} уведомлений!";
        notificationManager.SendNotification(title, message);
    }

    void OnSendInTenSecondsButtonClick(object sender, EventArgs e)
    {
        notificationNumber++;
        string title = $"Локальное уведомление №{notificationNumber}";
        string message = $"Вы получили {notificationNumber} уведомлений!";
        notificationManager.SendNotification(title, message, DateTime.Now.AddSeconds(10));
    }

    void OnSendInFiveMinutesButtonClick(object sender, EventArgs e)
    {
        notificationNumber++;
        string title = $"Локальное уведомление №{notificationNumber}";
        string message = $"Вы получили {notificationNumber} уведомлений!";
        notificationManager.SendNotification(title, message, DateTime.Now.AddMinutes(5));
    }

    void ShowNotification(string title, string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var msg = new Label()
            {
                Text = $"Получено уведомление:\nЗаголовок: {title}\nСообщение: {message}"
            };
            verticalStackLayout.Children.Add(msg);
        });
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        CounterBtn.Text = $"Количество кликов: {++count} раз(а)";

        SemanticScreenReader.Announce(CounterBtn.Text);
    }
}