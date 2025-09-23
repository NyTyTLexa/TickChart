using System;
using Avalonia.Controls;
using UTS.AvaloniaUI.ComponentTask1.Models;
using UTS.AvaloniaUI.ComponentTask1.ViewModels;

namespace UTS.AvaloniaUI.ComponentTask1.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? _viewModel;
        private TickChart.Controls.TickChart? _tickChart;

        public MainWindow()
        {
            InitializeComponent();
        
            _tickChart = this.FindControl<TickChart.Controls.TickChart>("TickChart");
        
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.TickDataGenerated -= OnTickDataGenerated;
            }

            _viewModel = DataContext as MainWindowViewModel;

            if (_viewModel != null)
            {
                _viewModel.TickDataGenerated += OnTickDataGenerated;
            }
        }

        private void OnTickDataGenerated(object? sender, TickDataEventArgs e)
        {
            _tickChart?.AddTickData(e.Timestamp, e.Price);
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel?.Cleanup();
        
            if (_viewModel != null)
            {
                _viewModel.TickDataGenerated -= OnTickDataGenerated;
            }

            base.OnClosed(e);
        }
    }
}