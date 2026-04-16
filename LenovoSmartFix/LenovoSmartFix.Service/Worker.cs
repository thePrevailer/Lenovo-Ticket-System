using LenovoSmartFix.Service.IPC;

namespace LenovoSmartFix.Service;

/// <summary>
/// Hosted service that starts the named pipe IPC server and keeps it running.
/// </summary>
public sealed class SmartFixWorker : BackgroundService
{
    private readonly NamedPipeServer _pipeServer;
    private readonly ILogger<SmartFixWorker> _logger;

    public SmartFixWorker(NamedPipeServer pipeServer, ILogger<SmartFixWorker> logger)
    {
        _pipeServer = pipeServer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SmartFix Service starting");
        try
        {
            await _pipeServer.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SmartFix Service encountered a fatal error");
            throw;
        }
        finally
        {
            _logger.LogInformation("SmartFix Service stopped");
        }
    }
}
