using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows;

namespace PlantUMLEditor.Services
{
    internal class StatusNotificationService
    {
        private readonly Border _statusNotification;
        private readonly TextBlock _statusText;

        public StatusNotificationService(Border statusNotification, TextBlock statusText)
        {
            _statusNotification = statusNotification;
            _statusText = statusText;
        }

        public void ShowStatusNotification(string message)
        {
            _statusText.Text = message;
            _statusNotification.Visibility = Visibility.Visible;

            _statusNotification.Opacity = 0;
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(300))
            };
            _statusNotification.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
        }

        public void HideStatusNotification()
        {
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(300))
            };
            fadeOutAnimation.Completed += (s, e) =>
            {
                _statusNotification.Visibility = Visibility.Collapsed;
            };
            _statusNotification.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
        }

        public async Task ShowTemporaryNotification(string message, int durationMs = 10000)
        {
            ShowStatusNotification(message);
            await Task.Delay(durationMs);
            HideStatusNotification();
        }
    }
}
