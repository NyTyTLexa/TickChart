using System;

namespace UTS.AvaloniaUI.ComponentTask1.Models;

public class TickDataEventArgs : EventArgs
{
    public double Timestamp { get; }
    public double Price { get; }

    public TickDataEventArgs(double timestamp, double price)
    {
        Timestamp = timestamp;
        Price = price;
    }
}