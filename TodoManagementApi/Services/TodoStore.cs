using System.Collections.Concurrent;
using System.Threading;
using TodoManagementApi.Models;

namespace TodoManagementApi.Services;

public sealed class TodoStore
{
    private readonly ConcurrentDictionary<int, TodoItem> _todos = new();
    private int _nextId;

    public IReadOnlyCollection<TodoItem> GetAll(bool? completed = null)
    {
        IEnumerable<TodoItem> items = _todos.Values;

        if (completed.HasValue)
        {
            items = items.Where(item => item.IsCompleted == completed.Value);
        }

        return items.OrderBy(item => item.Id).ToArray();
    }

    public TodoItem? GetById(int id)
    {
        return _todos.TryGetValue(id, out var item) ? item : null;
    }

    public TodoItem Add(string title)
    {
        var id = Interlocked.Increment(ref _nextId);
        var item = new TodoItem(id, title.Trim(), false);
        _todos[id] = item;
        return item;
    }
}
