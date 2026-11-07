using System.Collections.Concurrent;

namespace PracticalWork_3Topics.Middleware;

// Защищает от одновременной обработки одинакового ключа в одном экземпляре приложения.
public sealed class IdempotencyLock
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public SemaphoreSlim For(string key) => _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
}
