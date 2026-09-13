# TickChart

Пользовательский контрол Avalonia (`TemplatedControl`) для потока тиковых данных
в реальном времени на ScottPlot.

- `MaxVisibleTicks` — сколько последних точек держать на экране; старшие вытесняются
- `Theme` — Dark / Light
- обновление графика по таймеру ~16 мс, данные копятся под lock
- в демо-приложении — генератор тиков и крутилка `MaxVisibleTicks`

.NET 8 · Avalonia 11 · ScottPlot 5 · xUnit

```bash
dotnet restore
dotnet run --project UTS.AvaloniaUI.ComponentTask1
dotnet test
```
