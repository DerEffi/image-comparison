using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageComparison.Models;
using ImageComparison.Services;
using ImageComparisonGUI.Services;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ImageComparisonGUI.ViewModels;

public partial class LogsPageViewModel : ViewModelBase
{
    [ObservableProperty] private string logs = "";

    public LogsPageViewModel()
    {
        LogService.Logs.ForEach(log => appendLog(log));
        LogService.OnLog += OnLog;
    }

    private void OnLog(object? sender, LogEventArgs logEvent)
    {
        appendLog(logEvent.Log);
    }

    private void appendLog(Log log)
    {
        Logs += $"{log.Time.ToLongTimeString()}  -  {log.LogLevel}  -  {ToLiteral(log.Text)}\n";
    }

    private static string ToLiteral(string valueTextForCompiler)
    {
        return Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(valueTextForCompiler, false);
    }
}
