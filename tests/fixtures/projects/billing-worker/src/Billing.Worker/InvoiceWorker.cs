namespace Billing.Worker;

public sealed class InvoiceWorker(ILogger<InvoiceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Waiting for invoice events");
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
