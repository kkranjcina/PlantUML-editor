using System;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows;

namespace PlantUMLEditor.Services
{
    internal class SpinnerService
    {
        private readonly RotateTransform _spinnerRotation;

        public SpinnerService(RotateTransform spinnerRotation)
        {
            _spinnerRotation = spinnerRotation;
        }

        public void StartAnimation()
        {
            var spinnerAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(1)),
                RepeatBehavior = RepeatBehavior.Forever
            };
            _spinnerRotation.BeginAnimation(RotateTransform.AngleProperty, spinnerAnimation);
        }

        public void StopAnimation()
        {
            _spinnerRotation.BeginAnimation(RotateTransform.AngleProperty, null);
        }
    }
}
