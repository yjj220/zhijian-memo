using System.Windows;
using System.Windows.Controls;
using StickyPad.Models;
using StickyPad.Services;

namespace StickyPad.Views;

public partial class ReminderWindow : Window
{
    private readonly DeskTaskStore _store;
    private List<DeskTask> _shown = new();
    public bool IsShuttingDown { get; set; }
    public ReminderWindow(DeskTaskStore store)
    {
        _store = store;
        InitializeComponent();
        Closing += (_, e) =>
        {
            if (IsShuttingDown) return;
            try { foreach (var task in _shown) _store.Acknowledge(task.Id); }
            catch (Exception ex) { e.Cancel = true; MessageBox.Show(this, "提醒状态未保存：" + ex.Message); }
        };
    }
    public void UpdateTasks(List<DeskTask> tasks)
    {
        _shown = tasks;
        ReminderItems.ItemsSource = tasks;
        if (tasks.Count == 0) Close();
    }
    private void CompleteClick(object sender, RoutedEventArgs e) => Run(() => _store.Complete(((DeskTask)((Button)sender).Tag).Id, true));
    private void SnoozeClick(object sender, RoutedEventArgs e) => Run(() => _store.Snooze(((DeskTask)((Button)sender).Tag).Id, DateTime.UtcNow));
    private void DismissClick(object sender, RoutedEventArgs e) => Run(() => _store.Acknowledge(((DeskTask)((Button)sender).Tag).Id));
    private void Run(Action action)
    {
        try { action(); UpdateTasks(_store.Items.Where(t => t.NeedsReminder(DateTime.UtcNow)).ToList()); }
        catch (Exception ex) { MessageBox.Show(this, "操作未保存：" + ex.Message); }
    }
}
