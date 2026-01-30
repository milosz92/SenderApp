namespace ClientB.Services;

public class InboxService
{
    private readonly string _inboxFilePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly HashSet<Guid> _processedOrderIds;

    public InboxService(string inboxFilePath = "inbox.txt")
    {
        _inboxFilePath = inboxFilePath;
        _processedOrderIds = new HashSet<Guid>();
        LoadProcessedOrders();
    }

    private void LoadProcessedOrders()
    {
        if (File.Exists(_inboxFilePath))
        {
            var lines = File.ReadAllLines(_inboxFilePath);
            foreach (var line in lines)
            {
                if (Guid.TryParse(line.Trim(), out var orderId))
                {
                    _processedOrderIds.Add(orderId);
                }
            }
            Console.WriteLine($"[InboxService] Loaded {_processedOrderIds.Count} processed order IDs from inbox");
        }
        else
        {
            Console.WriteLine($"[InboxService] Created new inbox file: {_inboxFilePath}");
        }
    }

    public async Task<bool> IsOrderProcessedAsync(Guid orderId)
    {
        await _semaphore.WaitAsync();
        try
        {
            return _processedOrderIds.Contains(orderId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task MarkAsProcessedAsync(Guid orderId)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_processedOrderIds.Add(orderId))
            {
                await File.AppendAllTextAsync(_inboxFilePath, $"{orderId}{Environment.NewLine}");
                Console.WriteLine($"[InboxService] Order {orderId} marked as processed");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
