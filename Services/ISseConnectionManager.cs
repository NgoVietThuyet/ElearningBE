namespace ElearningAPI.Services
{
    public interface ISseConnectionManager
    {
        Task AddClientAsync(string channel, HttpResponse response, CancellationToken cancellationToken);
        Task BroadcastAsync(string channel, string eventName, object data);
        Task BroadcastToAdminAsync(string eventName, object data);
        int GetConnectionCount(string channel);
    }
}
