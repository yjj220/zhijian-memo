using System.Media;
using System.Windows;
using System.Windows.Threading;
using StickyPad.Views;

namespace StickyPad.Services;

public sealed class DeskTaskService : IDisposable
{
    private readonly DeskTaskStore _store;
    private readonly IWindowManager _notes;
    private readonly ISettingsService _settings;
    private readonly ITrayService _tray;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(5) };
    private TaskBoardWindow? _board;
    private ReminderWindow? _reminder;
    private readonly HashSet<(Guid, DateTime?)> _announced = new();

    public DeskTaskService(DeskTaskStore store, IWindowManager notes, ISettingsService settings, ITrayService tray)
    {
        _store = store; _notes = notes; _settings = settings; _tray = tray;
        _timer.Tick += (_, _) => CheckReminders();
    }
    public void Start()
    {
        _timer.Start();
        OpenBoard();
        if (_store.RecoveryMessage is { } message) MessageBox.Show(message, "纸间 · 数据恢复");
        CheckReminders();
    }
    public void OpenBoard()
    {
        _board ??= new TaskBoardWindow(_store, _notes, _settings);
        _board.Show();
        if (_board.WindowState == WindowState.Minimized) _board.WindowState = WindowState.Normal;
        _board.Activate();
        _board.Refresh();
    }
    private void CheckReminders()
    {
        var due = _store.Items.Where(t => t.NeedsReminder(DateTime.UtcNow)).OrderBy(t => t.ReminderUtc).ToList();
        if (due.Count == 0) { _reminder?.UpdateTasks(due); return; }
        var fresh = due.Where(t => _announced.Add((t.Id, t.ReminderUtc))).ToList();
        if (_reminder is null)
        {
            _reminder = new ReminderWindow(_store);
            _reminder.Closed += (_, _) => _reminder = null;
            _reminder.UpdateTasks(due);
            _reminder.Show();
        }
        else _reminder.UpdateTasks(due);
        if (fresh.Count > 0)
        {
            _tray.ShowNotification("纸间 · 待办提醒", fresh.Count == 1 ? fresh[0].Title : $"有 {fresh.Count} 件事项需要处理");
            try { SystemSounds.Asterisk.Play(); } catch { /* the persistent reminder window is the primary channel */ }
        }
    }
    public void Dispose()
    {
        _timer.Stop(); _board?.SaveGeometry();
        if (_reminder is not null) _reminder.IsShuttingDown = true;
    }
}
