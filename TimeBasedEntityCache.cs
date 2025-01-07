namespace EffectZones;

using ExileCore2.PoEMemory.MemoryObjects;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

public class TimeBasedEntityCache
{
    private readonly ConcurrentDictionary<uint, (Entity Entity, DateTime Timestamp)> _cache;
    private readonly TimeSpan _expirationTime;
    private readonly Timer _cleanupTimer;

    public TimeBasedEntityCache(TimeSpan expirationTime)
    {
        _cache = new ConcurrentDictionary<uint, (Entity Entity, DateTime Timestamp)>();
        _expirationTime = expirationTime;

        // Create a timer that runs cleanup every 100ms
        _cleanupTimer = new Timer(CleanupExpiredEntities, null, 0, 100);
    }

    public void Add(Entity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _cache.AddOrUpdate(
            entity.Id, 
            _ => (entity, DateTime.UtcNow),
            (_, _) => (entity, DateTime.UtcNow)
        );
    }

    public Entity Get(uint id)
    {
        if (_cache.TryGetValue(id, out var cachedItem))
        {
            if (DateTime.UtcNow - cachedItem.Timestamp <= _expirationTime)
            {
                return cachedItem.Entity;
            }

            // If the entity is expired, remove it and return null
            _cache.TryRemove(id, out _);
        }

        return null;
    }

    public Entity[] GetAll()
    {
        var now = DateTime.UtcNow;
        return _cache
            .Where(kvp => now - kvp.Value.Timestamp <= _expirationTime)
            .Select(kvp => kvp.Value.Entity)
            .ToArray();
    }

    private void CleanupExpiredEntities(object state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => now - kvp.Value.Timestamp > _expirationTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}
