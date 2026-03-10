using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace wmux.Services;

/// <summary>
/// Named pipe server — equivalent of cmux's socket API.
/// The wmux CLI tool connects here to send commands.
/// Pipe name: \\.\pipe\wmux
/// </summary>
public sealed class PipeServerService : IDisposable
{
    private const string PipeName = "wmux";
    private CancellationTokenSource _cts = new();

    public event Action<PipeCommand>? CommandReceived;

    public void Start()
    {
        Task.Run(AcceptLoop, _cts.Token);
    }

    private async Task AcceptLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var pipe = new NamedPipeServerStream(PipeName,
                    PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                await pipe.WaitForConnectionAsync(_cts.Token);
                _ = HandleClient(pipe);
            }
            catch (OperationCanceledException) { break; }
            catch { await Task.Delay(500); }
        }
    }

    private async Task HandleClient(NamedPipeServerStream pipe)
    {
        using (pipe)
        {
            try
            {
                var buffer = new byte[4096];
                int read = await pipe.ReadAsync(buffer, _cts.Token);
                if (read == 0) return;

                var json = Encoding.UTF8.GetString(buffer, 0, read);
                var cmd = JsonSerializer.Deserialize<PipeCommand>(json);
                if (cmd is not null)
                {
                    CommandReceived?.Invoke(cmd);

                    // Send ACK
                    var ack = Encoding.UTF8.GetBytes("{\"ok\":true}");
                    await pipe.WriteAsync(ack, _cts.Token);
                }
            }
            catch { }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
    }
}

public record PipeCommand(string Action, Dictionary<string, string>? Args = null)
{
    // Actions: new-workspace, close-workspace, new-pane, split-horizontal,
    //          split-vertical, notify, focus-workspace, open-browser, send-keys
}
