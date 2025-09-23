using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Controls.Presenters;
using ScottPlot.Stylers;
using Color = System.Drawing.Color;

namespace TickChart.Controls
{
    public class TickChart : TemplatedControl
    {
        private AvaPlot? _plot;
        private Scatter? _scatter;
        private readonly List<double> _xData = new();
        private readonly List<double> _yData = new();
        private readonly object _dataLock = new();
        private Timer? _refreshTimer;
        private int _cachedMaxVisibleTicks = 500; // Cache the property value

        public static readonly StyledProperty<int> MaxVisibleTicksProperty =
            AvaloniaProperty.Register<TickChart, int>(nameof(MaxVisibleTicks), 500);

        public static readonly StyledProperty<ChartTheme> ThemeProperty =
            AvaloniaProperty.Register<TickChart, ChartTheme>(nameof(Theme), ChartTheme.Light);

        public int MaxVisibleTicks
        {
            get => GetValue(MaxVisibleTicksProperty);
            set => SetValue(MaxVisibleTicksProperty, value);
        }

        public ChartTheme Theme
        {
            get => GetValue(ThemeProperty);
            set => SetValue(ThemeProperty, value);
        }

        static TickChart()
        {
            MaxVisibleTicksProperty.Changed.Subscribe(OnMaxVisibleTicksChanged);
            ThemeProperty.Changed.Subscribe(OnThemeChanged);
        }

        public TickChart()
        {
            _refreshTimer = new Timer(RefreshChart, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(16));
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            var contentPresenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
            if (contentPresenter != null)
            {
                _plot = new AvaPlot
                {
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
                };
                contentPresenter.Content = _plot;

                InitializeScatter();
                ConfigureChart();
            }
        }

        private void InitializeScatter()
        {
            if (_plot == null) return;

            lock (_dataLock)
            {
                _scatter = _plot.Plot.Add.Scatter(_xData.ToArray(), _yData.ToArray());
                _scatter.LineWidth = 1;
                _scatter.MarkerSize = 2;
            }
        }

        private void ConfigureChart()
        {
            if (_plot == null) return;

            _plot.Plot.Axes.Left.Label.Text = "Price";
            _plot.Plot.Axes.Bottom.Label.Text = "Time";
            

            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (_plot == null) return;
    
            var colors = Theme == ChartTheme.Dark
                ? (
                    FigureBackground: Color.Black,
                    DataBackground: Color.FromArgb(255, 30, 30, 30), // Темно-серый вместо белого
                    Axis: Color.DeepSkyBlue,  // Белый текст для темной темы
                    Grid: Color.Gray,
                    Text: Color.LightGray  // Светло-серый для текста
                )
                : (
                    FigureBackground: Color.White,
                    DataBackground: Color.WhiteSmoke,
                    Axis: Color.DeepSkyBlue,
                    Grid: Color.Gray,
                    Text: Color.Black  // Черный текст для светлой темы
                );

            _plot.Plot.FigureBackground.Color = ScottPlot.Color.FromColor(colors.FigureBackground);
            _plot.Plot.DataBackground.Color = ScottPlot.Color.FromColor(colors.DataBackground);

            // Применяем цвет к тексту осей
            _plot.Plot.Axes.Left.Label.ForeColor = ScottPlot.Color.FromColor(colors.Text);
            _plot.Plot.Axes.Bottom.Label.ForeColor = ScottPlot.Color.FromColor(colors.Text);
    
            // Также применяем к тикам (цифрам на осях)
            _plot.Plot.Axes.Left.TickLabelStyle.ForeColor = ScottPlot.Color.FromColor(colors.Text);
            _plot.Plot.Axes.Bottom.TickLabelStyle.ForeColor = ScottPlot.Color.FromColor(colors.Text);
    
            // Цвет линий осей
    
            _plot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromColor(colors.Grid);
            _plot.Plot.Grid.MinorLineColor = ScottPlot.Color.FromColor(colors.Grid);
    
            if (_scatter != null)
            {
                _scatter.Color = ScottPlot.Color.FromColor(colors.Axis);
                _scatter.MarkerColor = ScottPlot.Color.FromColor(colors.Axis);
            }
            
            // Принудительно обновляем график
            _plot.Refresh();
        }

        // Thread-safe method that can be called from any thread
        public void AddTickData(double timestamp, double price)
        {
            // Check if we're on the UI thread
            if (Dispatcher.UIThread.CheckAccess())
            {
                // We're on UI thread, call the actual implementation
                AddTickDataInternal(timestamp, price);
            }
            else
            {
                // We're on a background thread, marshal to UI thread
                Dispatcher.UIThread.Post(() => AddTickDataInternal(timestamp, price), DispatcherPriority.Normal);
            }
        }

        // Thread-safe batch method
        public void AddTickDataBatch(IEnumerable<(double timestamp, double price)> tickData)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                AddTickDataBatchInternal(tickData);
            }
            else
            {
                // Convert to array to avoid multiple enumeration across threads
                var dataArray = tickData.ToArray();
                Dispatcher.UIThread.Post(() => AddTickDataBatchInternal(dataArray), DispatcherPriority.Normal);
            }
        }

        // Internal implementation that must run on UI thread
        private void AddTickDataInternal(double timestamp, double price)
        {
            lock (_dataLock)
            {
                _xData.Add(timestamp);
                _yData.Add(price);

                // Safe to access MaxVisibleTicks here since we're on UI thread
                while (_xData.Count > MaxVisibleTicks)
                {
                    _xData.RemoveAt(0);
                    _yData.RemoveAt(0);
                }

                UpdateScatter();
            }
        }

        private void AddTickDataBatchInternal(IEnumerable<(double timestamp, double price)> tickData)
        {
            lock (_dataLock)
            {
                foreach (var (timestamp, price) in tickData)
                {
                    _xData.Add(timestamp);
                    _yData.Add(price);
                }

                // Safe to access MaxVisibleTicks here since we're on UI thread
                while (_xData.Count > MaxVisibleTicks)
                {
                    _xData.RemoveAt(0);
                    _yData.RemoveAt(0);
                }

                UpdateScatter();
            }
        }

        public void ClearData()
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                ClearDataInternal();
            }
            else
            {
                Dispatcher.UIThread.Post(ClearDataInternal, DispatcherPriority.Normal);
            }
        }

        private void ClearDataInternal()
        {
            lock (_dataLock)
            {
                _xData.Clear();
                _yData.Clear();
                UpdateScatter();
            }
        }

        private void UpdateScatter()
        {
            if (_plot == null) return;

            // This method should only be called from UI thread
            _plot.Plot.PlottableList.Clear();
            _scatter = _plot.Plot.Add.Scatter(_xData.ToArray(), _yData.ToArray());
            _scatter.LineWidth = 2;
            _scatter.MarkerSize = 2;
            ApplyTheme();
        }

        private void RefreshChart(object? state)
        {
            if (_plot == null) return;

            Dispatcher.UIThread.Post(() =>
            {
                lock (_dataLock)
                {
                    if (_xData.Count > 0)
                        _plot.Plot.Axes.AutoScale();
                }
                _plot.Refresh();
            }, DispatcherPriority.Background);
        }

        private static void OnMaxVisibleTicksChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is TickChart chart && e.NewValue is int newMax)
            {
                // Update cached value
                chart._cachedMaxVisibleTicks = newMax;

                // Ensure we're on UI thread for the cleanup
                if (Dispatcher.UIThread.CheckAccess())
                {
                    chart.TrimDataToMaxTicks(newMax);
                }
                else
                {
                    Dispatcher.UIThread.Post(() => chart.TrimDataToMaxTicks(newMax), DispatcherPriority.Normal);
                }
            }
        }

        private void TrimDataToMaxTicks(int maxTicks)
        {
            lock (_dataLock)
            {
                while (_xData.Count > maxTicks)
                {
                    _xData.RemoveAt(0);
                    _yData.RemoveAt(0);
                }
                UpdateScatter();
            }
        }

        private static void OnThemeChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is TickChart chart)
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    chart.ApplyTheme();
                }
                else
                {
                    Dispatcher.UIThread.Post(chart.ApplyTheme, DispatcherPriority.Normal);
                }
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _refreshTimer?.Dispose();
            _refreshTimer = null;
            base.OnDetachedFromVisualTree(e);
        }
    }

    public enum ChartTheme
    {
        Light,
        Dark
    }
}