using PraxisChessEngine.Business.Diagnostics;
using System;
using System.IO;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PraxisChessEngine.CLIApp.Diagnostics;

internal sealed class FileDiagnosticLogger : IDiagnosticLogger, IAsyncDisposable
{
    private readonly Channel<string> _messages;
    private readonly StreamWriter _writer;
    private readonly Task _writeTask;

    public FileDiagnosticLogger(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous), new UTF8Encoding(false))
        {
            AutoFlush = false
        };
        _messages = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _writeTask = WriteAsync();
    }

    public void Log(string message)
    {
        _messages.Writer.TryWrite($"{DateTimeOffset.Now:O} {message}");
    }

    public void Log(Exception exception, string message)
    {
        Log($"{message}{Environment.NewLine}{exception}");
    }

    public async ValueTask DisposeAsync()
    {
        _messages.Writer.TryComplete();
        await _writeTask;
        await _writer.DisposeAsync();
    }

    private async Task WriteAsync()
    {
        await foreach (string message in _messages.Reader.ReadAllAsync())
        {
            await _writer.WriteLineAsync(message);
        }

        await _writer.FlushAsync();
    }
}
