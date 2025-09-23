using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TickChart.Controls;
using Avalonia.Threading;
using UTS.AvaloniaUI.ComponentTask1.Models;

namespace UTS.AvaloniaUI.ComponentTask1.ViewModels
{
    public class SimpleCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public SimpleCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
    }

    public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        private DispatcherTimer? _dataTimer;
        private readonly Random _random = new();
        private double _currentPrice = 100.0;
        private double _currentTime = 0.0;
        private bool _isGenerating = false;
        private bool _disposed = false;

        private int _maxVisibleTicks = 500;
        private ChartTheme _selectedTheme = ChartTheme.Light;
        private string _startStopButtonText = "Start";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (_disposed) return;

            // Всегда вызываем PropertyChanged в UI потоке
            if (Dispatcher.UIThread.CheckAccess())
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            else
            {
                Dispatcher.UIThread.Post(() => 
                {
                    if (!_disposed)
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                });
            }
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public int MaxVisibleTicks
        {
            get => _maxVisibleTicks;
            set => SetField(ref _maxVisibleTicks, value);
        }

        public ChartTheme SelectedTheme
        {
            get => _selectedTheme;
            set => SetField(ref _selectedTheme, value);
        }

        public string StartStopButtonText
        {
            get => _startStopButtonText;
            private set => SetField(ref _startStopButtonText, value);
        }

        public bool IsGenerating
        {
            get => _isGenerating;
            private set => SetField(ref _isGenerating, value);
        }

        public List<ChartTheme> AvailableThemes { get; } = new()
        {
            ChartTheme.Light,
            ChartTheme.Dark
        };

        public ICommand ToggleDataGenerationCommand { get; }

        public event EventHandler<TickDataEventArgs>? TickDataGenerated;

        public MainWindowViewModel()
        {
            ToggleDataGenerationCommand = new SimpleCommand(ToggleDataGeneration);
        }

        private void ToggleDataGeneration()
        {
            if (_disposed) return;

            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(ToggleDataGeneration);
                return;
            }

            if (IsGenerating)
            {
                StopDataGeneration();
            }
            else
            {
                StartDataGeneration();
            }
        }

        private void StartDataGeneration()
        {
            if (IsGenerating || _disposed) return;

            IsGenerating = true;
            StartStopButtonText = "Stop";

            _dataTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            _dataTimer.Tick += OnDataTimerTick;
            _dataTimer.Start();
        }

        private void StopDataGeneration()
        {
            if (!IsGenerating) return;

            IsGenerating = false;
            StartStopButtonText = "Start";
            
            if (_dataTimer != null)
            {
                _dataTimer.Stop();
                _dataTimer.Tick -= OnDataTimerTick;
                _dataTimer = null;
            }
        }

        private void OnDataTimerTick(object? sender, EventArgs e)
        {
            if (!IsGenerating || _disposed) return;

            try
            {
                var priceChange = (_random.NextDouble() - 0.5) * 0.5;
                _currentPrice += priceChange;
                
                if (_currentPrice < 50) _currentPrice = 50;
                if (_currentPrice > 150) _currentPrice = 150;

                _currentTime += 0.01;

                TickDataGenerated?.Invoke(this, new TickDataEventArgs(_currentTime, _currentPrice));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in timer: {ex}");
                StopDataGeneration();
            }
        }

        public void Cleanup()
        {
            if (_disposed) return;

            if (Dispatcher.UIThread.CheckAccess())
            {
                StopDataGeneration();
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (!_disposed) StopDataGeneration();
                });
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            Cleanup();
            GC.SuppressFinalize(this);
        }
    }
    
}