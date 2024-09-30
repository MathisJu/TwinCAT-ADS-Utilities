using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace AdsUtilitiesUI;


public interface ILoggerService
{
    void LogSuccess(string message);
    void LogError(string message);
    void LogInfo(string message);
    void LogWarning(string message);

    event EventHandler<LogMessage> OnNewLogMessage;
}

public enum LogLevel
{
    Success,
    Error,
    Info,
    Warning
}

public class LogMessage
{
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public LogLevel LogLevel { get; set; } // LogLevel for filtering
}

public class LoggerService : ILoggerService
{
    private readonly ObservableCollection<LogMessage> _logMessages;
    private readonly Dispatcher _dispatcher;

    private static readonly object _lock = new object();

    public LoggerService(ObservableCollection<LogMessage> logMessages, Dispatcher dispatcher)
    {
        _logMessages = logMessages;
        _dispatcher = dispatcher;
    }

    public void LogSuccess(string message) => Log(message, LogLevel.Success);

    public void LogError(string message) => Log(message, LogLevel.Error);

    public void LogInfo(string message) => Log(message, LogLevel.Info);

    public void LogWarning(string message) => Log(message, LogLevel.Warning);

    public event EventHandler<LogMessage> OnNewLogMessage;

    private void Log(string message, LogLevel logLevel)
    {
        var logMessage = new LogMessage
        {
            Message = message,
            Timestamp = DateTime.Now,
            LogLevel = logLevel
        };

        // Thread-Sicherheit durch Dispatcher gewährleisten
        _dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                _logMessages.Add(logMessage);
                OnNewLogMessage?.Invoke(this, logMessage);
            }
        });
    }
}