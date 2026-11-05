using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HyperLisUI.Views.Components
{
    public partial class CircularProgressBar : UserControl
    {
        public CircularProgressBar()
        {
            InitializeComponent();
            UpdateArc(0);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(CircularProgressBar), 
                new PropertyMetadata(0.0, OnValueChanged));

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(CircularProgressBar), 
                new PropertyMetadata("0%", OnTextChanged));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty SubtextProperty =
            DependencyProperty.Register("Subtext", typeof(string), typeof(CircularProgressBar), 
                new PropertyMetadata("", OnSubtextChanged));

        public string Subtext
        {
            get { return (string)GetValue(SubtextProperty); }
            set { SetValue(SubtextProperty, value); }
        }

        public static readonly DependencyProperty ProgressColorProperty =
            DependencyProperty.Register("ProgressColor", typeof(Brush), typeof(CircularProgressBar), 
                new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4FD4D8")), OnColorChanged));

        public Brush ProgressColor
        {
            get { return (Brush)GetValue(ProgressColorProperty); }
            set { SetValue(ProgressColorProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularProgressBar cp)
            {
                cp.UpdateArc((double)e.NewValue);
            }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularProgressBar cp) cp.ValueText.Text = (string)e.NewValue;
        }

        private static void OnSubtextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularProgressBar cp) cp.SubText.Text = (string)e.NewValue;
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularProgressBar cp) cp.ProgressPath.Stroke = (Brush)e.NewValue;
        }

        private void UpdateArc(double value)
        {
            // Value is 0 to 100
            if (value < 0) value = 0;
            if (value > 100) value = 100;
            
            double angle = 360 * (value / 100);
            
            // New Radius 44 to match XAML
            // Center is 50,50
            double radius = 44;
            double cx = 50;
            double cy = 50;
            
            double rad = (Math.PI / 180) * angle;
            
            double x = cx + radius * Math.Sin(rad);
            double y = cy - radius * Math.Cos(rad);
            
            if (ProgressPath.Data is PathGeometry pg && pg.Figures.Count > 0)
            {
                // Start Point at Top Center (50, 6)
                pg.Figures[0].StartPoint = new Point(cx, cy - radius);
                ProgressArc.Size = new Size(radius, radius);
                
                // Avoid full circle closing prematurely or disappearing
                if (Math.Abs(value - 100) < 0.01)
                {
                    // Slightly offset to keep Arc Valid
                    x = cx - 0.01; 
                    y = cy - radius;
                }

                ProgressArc.Point = new Point(x, y);
                ProgressArc.IsLargeArc = angle > 180;
            }
        }
    }
}
