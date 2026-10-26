using System.IO;
using System.Text.Json;
using StickyPad.Models;

namespace StickyPad.Services;

/// All mutations run on the UI thread. Publish state only after the atomic disk write succeeds.
public sealed class DeskTaskStore
{
    private readonly string _path;
    private List<DeskTask> _items;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public IReadOnlyList<DeskTask> Items => _items.AsReadOnly();
    public event Action? Changed;
    public string? RecoveryMessage { get; private set; }

    public DeskTaskStore(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path)) { _items = new(); return; }
        try { _items = Read(path); }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        {
            // Preserve damaged data for recovery instead of silently replacing it with an empty list.
            var damaged = path + ".damaged-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            File.Copy(path, damaged);
            if (!File.Exists(path + ".bak"))
                throw new InvalidDataException($"待办数据无法读取，已保留副本：{damaged}", ex);
            _items = Read(path + ".bak");
            File.Copy(path + ".bak", path, overwrite: true);
            RecoveryMessage = "待办数据已从上一次备份恢复，原文件已保留。请检查最近的修改。";
        }
    }

    private static List<DeskTask> Read(string path)
    {
        var items = JsonSerializer.Deserialize<List<DeskTask>>(File.ReadAllText(path))
            ?? throw new InvalidDataException("待办文件为空。");
        if (items.Any(t => t is null || t.Id == Guid.Empty || string.IsNullOrWhiteSpace(t.Title))
            || items.Select(t => t.Id).Distinct().Count() != items.Count)
            throw new InvalidDataException("待办文件含有无效或重复的任务。");
        return items;
    }

    public void Save(DeskTask task)
    {
        if (string.IsNullOrWhiteSpace(task.Title)) throw new ArgumentException("请填写待办事项。");
        var next = _items.Where(t => t.Id != task.Id).ToList();
        next.Add(task with { Title = task.Title.Trim(), Details = task.Details.Trim() });
        Commit(next);
    }

    public void Delete(Guid id) => Commit(_items.Where(t => t.Id != id).ToList());
    public void Complete(Guid id, bool completed) => Change(id, t => t with { IsCompleted = completed });
    public void Acknowledge(Guid id) => Change(id, t => t with { ReminderAcknowledged = true });
    public void Snooze(Guid id, DateTime utcNow) => Change(id, t => t with
    {
        ReminderUtc = utcNow.AddMinutes(10), ReminderAcknowledged = false,
    });
    private void Change(Guid id, Func<DeskTask, DeskTask> change)
    {
        if (_items.All(t => t.Id != id)) return;
        Commit(_items.Select(t => t.Id == id ? change(t) : t).ToList());
    }

    private void Commit(List<DeskTask> next)
    {
        var temp = _path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, next, JsonOptions);
            stream.Flush(flushToDisk: true);
        }
        if (File.Exists(_path)) File.Replace(temp, _path, _path + ".bak");
        else File.Move(temp, _path);
        _items = next;
        Changed?.Invoke();
    }

    public void Export(string path) => File.WriteAllText(path, JsonSerializer.Serialize(_items, JsonOptions));
    public void Import(string path)
    {
        var imported = Read(path);
        var ids = imported.Select(t => t.Id).ToHashSet();
        Commit(_items.Where(t => !ids.Contains(t.Id)).Concat(imported).ToList());
    }
}
