using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System;
using System.Linq;
using System.Windows.Media.Animation;

namespace HyperLisUI.Views.Components
{
    public partial class LoadingOverlayView : UserControl
    {
        private int _currentSlide = 0;
        private List<(string Image, string Title, string Desc)> _slides;
        private System.Windows.Threading.DispatcherTimer _slideTimer;

        private class QuoteModel
        {
            public string Content { get; set; }
            public string Type { get; set; }
        }

        public event EventHandler Cancelled;

        public LoadingOverlayView()
        {
            InitializeComponent();
            this.Loaded += LoadingOverlayView_Loaded;
            this.Unloaded += LoadingOverlayView_Unloaded;

            _slideTimer = new System.Windows.Threading.DispatcherTimer();
            _slideTimer.Interval = TimeSpan.FromSeconds(5);
            _slideTimer.Tick += SlideTimer_Tick;
        }

        private void SlideTimer_Tick(object sender, EventArgs e)
        {
            NextSlide_Click(null, null);
        }

        private void LoadingOverlayView_Loaded(object sender, RoutedEventArgs e)
        {
            try 
            {
                InitializeSlides();
                UpdateSlide();
                _slideTimer.Start();
            }
            catch { }
        }

        private void LoadingOverlayView_Unloaded(object sender, RoutedEventArgs e)
        {
            _slideTimer.Stop();
        }

        private void InitializeSlides()
        {
            _slides = new List<(string, string, string)>();
            
            var quotesList = new List<QuoteModel>();
            string defaultTitle = "Did you know?";
            string defaultDesc = "Optimizing Windows services can free up significant RAM.";

            // Try to load quotes from "Download/quotes.json"
            try
            {
                string quotesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Download", "quotes.json");
                if (System.IO.File.Exists(quotesPath))
                {
                    string json = System.IO.File.ReadAllText(quotesPath);
                    var quotes = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, QuoteModel>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (quotes != null && quotes.Count > 0)
                    {
                        quotesList = quotes.Values.ToList();
                    }
                }
            }
            catch { }

            try
            {
                // Try to load from "Images/slide" directory relative to executable
                string slideDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "slide");
                if (System.IO.Directory.Exists(slideDir))
                {
                    var files = System.IO.Directory.GetFiles(slideDir, "*.*")
                                .Where(s => s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                                            s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                                            s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                                .OrderBy(x => Guid.NewGuid()) // Randomize
                                .ToList();

                    var rand = new Random();
                    foreach (var file in files)
                    {
                        string title = defaultTitle;
                        string desc = defaultDesc;

                        if (quotesList.Count > 0)
                        {
                            var q = quotesList[rand.Next(quotesList.Count)];
                            title = q.Type ?? defaultTitle;
                            desc = q.Content ?? defaultDesc;
                        }

                        _slides.Add((file, title, desc));
                    }
                }
            }
            catch { }

            // Fallback if no slides found
            if (_slides.Count == 0)
            {
                // Use default image or logic
                // For now, let's just make sure list isn't empty to avoid crash
                 _slides.Add(("", defaultTitle, defaultDesc));
            }
        }

        private void PrevSlide_Click(object sender, RoutedEventArgs e)
        {
            _currentSlide--;
            if (_currentSlide < 0) _currentSlide = _slides.Count - 1;
            UpdateSlide();
            ResetSlideTimer();
        }

        private void NextSlide_Click(object sender, RoutedEventArgs e)
        {
            _currentSlide++;
            if (_currentSlide >= _slides.Count) _currentSlide = 0;
            UpdateSlide();
            ResetSlideTimer();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private void ResetSlideTimer()
        {
            _slideTimer.Stop();
            _slideTimer.Start();
        }

        private void UpdateSlide()
        {
            if (_slides == null || _slides.Count == 0) return;

            var slide = _slides[_currentSlide];
            try
            {
                if (!string.IsNullOrEmpty(slide.Image))
                    CarouselImage.Source = new BitmapImage(new Uri(slide.Image, UriKind.RelativeOrAbsolute));
            }
            catch { /* Ignore image load errors */ }
            
            CarouselTitle.Text = slide.Title;
            CarouselDesc.Text = slide.Desc;

            UpdateDots();
        }

        private void GenerateDots()
        {
            DotsPanel.Children.Clear();
            if (_slides == null) return;

            foreach (var _ in _slides)
            {
                var border = new Border
                {
                    Width = 8,
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(4),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xCC, 0xCC, 0xCC)) // #CCC
                };
                DotsPanel.Children.Add(border);
            }
        }

        private void UpdateDots()
        {
            if (DotsPanel.Children.Count != _slides.Count)
            {
                GenerateDots();
            }

            for (int i = 0; i < DotsPanel.Children.Count; i++)
            {
                if (DotsPanel.Children[i] is Border border)
                {
                   if (i == _currentSlide)
                   {
                       border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0x78, 0xD7)); // #0078D7
                   }
                   else
                   {
                       border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xCC, 0xCC, 0xCC)); // #CCC
                   }
                }
            }
        }

        public void UpdateProgress(int step, int percentage, string taskName)
        {
            // Forces use of SingleProgressBar with smooth animation
            SingleProgressBar.Visibility = Visibility.Visible;
            if (DetailedProgressContainer != null)
            {
                DetailedProgressContainer.Visibility = Visibility.Collapsed;
            }
            
            double targetValue = 0;

            if (step <= 0)
            {
                // Single Step Mode: 0-100%
                StatusSubTitle.Text = taskName;
                targetValue = (percentage < 0) ? 0 : percentage;
                if (targetValue > 100) targetValue = 100;

                SingleProgressBar.IsIndeterminate = false;
            }
            else
            {
                // Multi-Step Mode: Map 1-3 steps to 0-100%
                // Step 1: 0-33.3%
                // Step 2: 33.3-66.6%
                // Step 3: 66.6-100%
                StatusSubTitle.Text = $"Step {step} of 3: {taskName}";
                SingleProgressBar.IsIndeterminate = false;

                double stepSize = 100.0 / 3.0; // ~33.33
                double baseValue = (step - 1) * stepSize;
                double stepProgress = (percentage / 100.0) * stepSize;
                
                targetValue = baseValue + stepProgress;
                if (targetValue > 100) targetValue = 100;
            }

            if (OverallPercentText != null)
            {
                OverallPercentText.Text = $"{(int)Math.Round(targetValue)}%";
            }

            if (percentage >= 0)
            {
                // Smooth Animation
                // Animate from current value to target value over 400ms
                DoubleAnimation animation = new DoubleAnimation(targetValue, TimeSpan.FromMilliseconds(400));
                SingleProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, animation);
            }
            else
            {
                // Run 0 to 100 exactly once
                DoubleAnimation animation = new DoubleAnimation(0, 100, TimeSpan.FromSeconds(2.0));
                animation.AutoReverse = false;
                SingleProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, animation);
            }
        }

        public void UpdateDetailedProgress(int step, int overallPercentage, int filePercentage, string taskName, string fileName, string transferSize, string speed)
        {
            SingleProgressBar.Visibility = Visibility.Visible;
            if (DetailedProgressContainer != null)
            {
                DetailedProgressContainer.Visibility = Visibility.Visible;
            }

            int clampedOverall = Math.Clamp(overallPercentage, 0, 100);

            if (step <= 0)
            {
                StatusSubTitle.Text = taskName;
            }
            else
            {
                StatusSubTitle.Text = $"Step {step} of 3: {taskName}";
            }

            if (OverallPercentText != null)
            {
                OverallPercentText.Text = $"{clampedOverall}%";
            }

            // Đồng bộ trực tiếp 1:1 giữa thanh Overall Progress Bar và số % hiển thị
            DoubleAnimation overallAnim = new DoubleAnimation(clampedOverall, TimeSpan.FromMilliseconds(200));
            SingleProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, overallAnim);

            if (CurrentFileNameText != null)
            {
                CurrentFileNameText.Text = string.IsNullOrEmpty(fileName) ? "Copying files..." : fileName;
            }

            int clampedFilePct = Math.Clamp(filePercentage, 0, 100);
            if (FilePercentText != null)
            {
                FilePercentText.Text = $"{clampedFilePct}%";
            }

            if (FileProgressBar != null)
            {
                DoubleAnimation fileAnim = new DoubleAnimation(clampedFilePct, TimeSpan.FromMilliseconds(200));
                FileProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, fileAnim);
            }

            if (TransferSizeText != null)
            {
                TransferSizeText.Text = string.IsNullOrEmpty(transferSize) ? "" : transferSize;
            }

            if (TransferSpeedText != null)
            {
                TransferSpeedText.Text = string.IsNullOrEmpty(speed) ? "" : speed;
            }
        }

        public void Reset()
        {
            SingleProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, null);
            SingleProgressBar.Value = 0;
            if (FileProgressBar != null)
            {
                FileProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, null);
                FileProgressBar.Value = 0;
            }
            if (DetailedProgressContainer != null)
            {
                DetailedProgressContainer.Visibility = Visibility.Collapsed;
            }
            if (OverallPercentText != null) OverallPercentText.Text = "0%";
            if (FilePercentText != null) FilePercentText.Text = "0%";
            if (CurrentFileNameText != null) CurrentFileNameText.Text = "Preparing...";
            if (TransferSizeText != null) TransferSizeText.Text = "0 MB / 0 MB";
            if (TransferSpeedText != null) TransferSpeedText.Text = "0 MB/s";
            StatusSubTitle.Text = "Initializing...";
        }
    }
}
