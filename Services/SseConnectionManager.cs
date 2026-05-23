using System.Collections.Concurrent;
using System.Text.Json;

namespace ElearningAPI.Services
{
    public class SseClient
    {
        public string Channel { get; set; } = string.Empty;
        public int UserId { get; set; }
        public HttpResponse Response { get; set; } = null!;
        public CancellationToken CancellationToken { get; set; }
        public SemaphoreSlim WriteLock { get; } = new SemaphoreSlim(1, 1);
        public bool IsAlive => !CancellationToken.IsCancellationRequested;
    }

    public class SseConnectionManager : ISseConnectionManager
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<SseClient>> _channels = new();
        private readonly ILogger<SseConnectionManager> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public SseConnectionManager(ILogger<SseConnectionManager> logger)
        {
            _logger = logger;
        }

        public async Task AddClientAsync(string channel, HttpResponse response, CancellationToken cancellationToken)
        {
            var client = new SseClient
            {
                Channel = channel,
                Response = response,
                CancellationToken = cancellationToken
            };

            var bag = _channels.GetOrAdd(channel, _ => new ConcurrentBag<SseClient>());
            bag.Add(client);

            _logger.LogInformation("SSE client connected to channel '{Channel}'. Total on channel: {Count}", channel, bag.Count);

            // Keep connection alive until client disconnects
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Client disconnected — normal
            }
            finally
            {
                _logger.LogInformation("SSE client disconnected from channel '{Channel}'.", channel);
                // Cleanup dead clients on channel
                CleanupChannel(channel);
            }
        }

        public async Task BroadcastAsync(string channel, string eventName, object data)
        {
            if (!_channels.TryGetValue(channel, out var bag) || bag.IsEmpty)
                return;

            var payload = SerializeEvent(eventName, data);
            var aliveClients = bag.Where(c => c.IsAlive).ToList();

            var tasks = aliveClients.Select(client => SendToClientAsync(client, payload));
            await Task.WhenAll(tasks);
        }

        public async Task BroadcastToAdminAsync(string eventName, object data)
        {
            await BroadcastAsync("admin-stats", eventName, data);
        }

        public int GetConnectionCount(string channel)
        {
            if (_channels.TryGetValue(channel, out var bag))
                return bag.Count(c => c.IsAlive);
            return 0;
        }

        private async Task SendToClientAsync(SseClient client, string payload)
        {
            if (!client.IsAlive) return;

            await client.WriteLock.WaitAsync();
            try
            {
                await client.Response.WriteAsync(payload, client.CancellationToken);
                await client.Response.Body.FlushAsync(client.CancellationToken);
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is IOException)
            {
                // Client disconnected mid-write — safe to ignore
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error writing SSE event to client on channel '{Channel}'.", client.Channel);
            }
            finally
            {
                client.WriteLock.Release();
            }
        }

        private static string SerializeEvent(string eventName, object data)
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            return $"event: {eventName}\ndata: {json}\n\n";
        }

        private void CleanupChannel(string channel)
        {
            if (_channels.TryGetValue(channel, out var bag))
            {
                // Replace bag with only alive clients
                var alive = bag.Where(c => c.IsAlive).ToList();
                if (!alive.Any())
                {
                    _channels.TryRemove(channel, out _);
                }
                else
                {
                    var newBag = new ConcurrentBag<SseClient>(alive);
                    _channels[channel] = newBag;
                }
            }
        }
    }
}
