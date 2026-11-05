using System.Windows;
using System.Windows.Controls;

namespace HyperLisUI.Helpers
{
    public static class ScrollViewerBinding
    {
        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.RegisterAttached("HorizontalOffset", typeof(double), typeof(ScrollViewerBinding),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHorizontalOffsetChanged));

        public static double GetHorizontalOffset(DependencyObject obj)
        {
            return (double)obj.GetValue(HorizontalOffsetProperty);
        }

        public static void SetHorizontalOffset(DependencyObject obj, double value)
        {
            obj.SetValue(HorizontalOffsetProperty, value);
        }

        private static void OnHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer sv && (double)e.NewValue != sv.HorizontalOffset)
            {
                sv.ScrollToHorizontalOffset((double)e.NewValue);
            }
        }

        public static readonly DependencyProperty MonitorScrollProperty =
            DependencyProperty.RegisterAttached("MonitorScroll", typeof(bool), typeof(ScrollViewerBinding),
                new PropertyMetadata(false, OnMonitorScrollChanged));

        public static bool GetMonitorScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(MonitorScrollProperty);
        }

        public static void SetMonitorScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(MonitorScrollProperty, value);
        }

        private static void OnMonitorScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer sv)
            {
                if ((bool)e.NewValue)
                {
                    sv.ScrollChanged += Sv_ScrollChanged;
                }
                else
                {
                    sv.ScrollChanged -= Sv_ScrollChanged;
                }
            }
        }

        private static void Sv_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer sv)
            {
                SetHorizontalOffset(sv, sv.HorizontalOffset);
            }
        }
    }
}
