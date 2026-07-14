namespace PaisApp.Services;

public interface ISessionService
{
    Task<bool> CanStartSessionAsync(int userId);
    Task RegisterSessionAsync(int userId, string sessionToken);
    Task CloseSessionAsync(int userId, string sessionToken);
    Task<bool> ValidateSessionAsync(int userId, string sessionToken);
}

public class SessionService : ISessionService
{
    private static readonly Dictionary<int, Dictionary<string, DateTime>> _userSessions = new();
    private static readonly object _lock = new();
    private static readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(8);

    public Task<bool> CanStartSessionAsync(int userId)
    {
        lock (_lock)
        {
            CleanExpiredSessions(userId);

            if (!_userSessions.ContainsKey(userId))
                return Task.FromResult(true);

            if (_userSessions[userId].Count > 0)
                _userSessions.Remove(userId);

            return Task.FromResult(true);
        }
    }

    public Task RegisterSessionAsync(int userId, string sessionToken)
    {
        lock (_lock)
        {
            if (!_userSessions.ContainsKey(userId))
                _userSessions[userId] = new Dictionary<string, DateTime>();

            _userSessions[userId][sessionToken] = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task CloseSessionAsync(int userId, string sessionToken)
    {
        lock (_lock)
        {
            if (_userSessions.ContainsKey(userId))
                _userSessions[userId].Remove(sessionToken);

            if (_userSessions.ContainsKey(userId) && _userSessions[userId].Count == 0)
                _userSessions.Remove(userId);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ValidateSessionAsync(int userId, string sessionToken)
    {
        lock (_lock)
        {
            if (!_userSessions.ContainsKey(userId))
                return Task.FromResult(false);

            if (!_userSessions[userId].ContainsKey(sessionToken))
                return Task.FromResult(false);

            var elapsed = DateTime.UtcNow - _userSessions[userId][sessionToken];
            if (elapsed > _sessionTimeout)
            {
                _userSessions[userId].Remove(sessionToken);
                if (_userSessions[userId].Count == 0)
                    _userSessions.Remove(userId);
                return Task.FromResult(false);
            }

            _userSessions[userId][sessionToken] = DateTime.UtcNow;
            return Task.FromResult(true);
        }
    }

    private void CleanExpiredSessions(int userId)
    {
        if (!_userSessions.ContainsKey(userId)) return;

        var expired = _userSessions[userId]
            .Where(kvp => DateTime.UtcNow - kvp.Value > _sessionTimeout)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var sessionToken in expired)
            _userSessions[userId].Remove(sessionToken);

        if (_userSessions[userId].Count == 0)
            _userSessions.Remove(userId);
    }
}
