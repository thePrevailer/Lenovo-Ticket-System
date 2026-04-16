using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace LenovoSmartFix.Service.IPC;

/// <summary>
/// Named pipe server that accepts JSON-encoded IpcRequest messages from the UI process
/// and returns IpcResponse messages. Accepts multiple concurrent connections.
/// </summary>
public sealed class NamedPipeServer
{
    private readonly string _pipeName;
    private readonly IpcMessageHandler _handler;
    private readonly ILogger<NamedPipeServer> _logger;

    public NamedPipeServer(
        IOptions<SmartFixOptions> opts,
        IpcMessageHandler handler,
        ILogger<NamedPipeServer> logger)
    {
        _pipeName = opts.Value.IpcPipeName;
        _handler = handler;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("Named pipe server starting on pipe '{PipeName}'", _pipeName);

        while (!ct.IsCancellationRequested)
        {
            var pipe = CreatePipe();
            try
            {
                await pipe.WaitForConnectionAsync(ct);
                _ = HandleConnectionAsync(pipe, ct);
            }
            catch (OperationCanceledException)
            {
                pipe.Dispose();
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error waiting for pipe connection");
                pipe.Dispose();
            }
        }
    }

    private NamedPipeServerStream CreatePipe()
    {
        // Allow current user and local service accounts to connect
        var ps = new PipeSecurity();
        ps.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
            PipeAccessRights.ReadWrite,
            AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            _pipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Message,
            PipeOptions.Asynchronous,
            4096, 4096,
            ps);
    }

    private async Task HandleConnectionAsync(NamedPipeServerStream pipe, CancellationToken ct)
    {
        using (pipe)
        {
            try
            {
                var requestJson = await ReadMessageAsync(pipe, ct);
                var request = JsonSerializer.Deserialize<IpcRequest>(requestJson)
                    ?? throw new InvalidOperationException("Null IPC request");

                var response = await _handler.HandleAsync(request, ct);
                var responseJson = JsonSerializer.Serialize(response);
                await WriteMessageAsync(pipe, responseJson, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "IPC connection error");
            }
        }
    }

    private static async Task<string> ReadMessageAsync(
        PipeStream pipe, CancellationToken ct)
    {
        var buffer = new byte[65536];
        using var ms = new MemoryStream();
        do
        {
            var read = await pipe.ReadAsync(buffer, ct);
            ms.Write(buffer, 0, read);
        } while (!pipe.IsMessageComplete);

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static async Task WriteMessageAsync(
        PipeStream pipe, string message, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await pipe.WriteAsync(bytes, ct);
        await pipe.FlushAsync(ct);
    }
}
