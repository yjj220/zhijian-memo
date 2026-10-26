using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using StickyPad.Models;
using StickyPad.Services;

namespace StickyPad.Views;

public partial class TaskBoardWindow : Window
{
    private readonly DeskTaskStore _store;
    private readonly IWindowManager _notes;
    private readonly ISettingsService _settings;
    private readonly DispatcherTimer _refresh;
    private bool _ready;

    public TaskBoardWindow(DeskTaskStore store, IWindowManager notes, ISettingsService settings)
    {
        _store = store; _notes = notes; _settings = settings;
        InitializeComponent();
        Icon = Utils.IconFactory.CreateAppIcon();
        Topmost = settings.Current.TaskBoardPinned;
        Width = Math.Max(MinWidth, settings.Current.TaskBoardWidth);
        Height = Math.Max(MinHeight, settings.Current.TaskBoardHeight);
        if (settings.Current.TaskBoardX is { } x && settings.Current.TaskBoardY is { } y)
        { WindowStartupLocation = WindowStartupLocation.Manual; Left = x; Top = y; }
        Loaded += (_, _) =>
        {
            var r = Utils.MonitorHelper.EnsureOnScreen(Left, Top, Width, Height);
            Left = r.X; Top = r.Y;
            Refresh();
        };
        Closing += (_, e) => { e.Cancel = true; SaveGeometry(); Hide(); };
        _store.Changed += Refresh;
        _refresh = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _refresh.Tick += (_, _) => Refresh();
        IsVisibleChanged += (_, _) => { if (IsVisible) { Refresh(); _refresh.Start(); } else _refresh.Stop(); };
        InputBindings.Add(new KeyBinding(new BoardCommand(() => Edit(null)), Key.N, ModifierKeys.Control));
        _ready = true;
        Refresh();
    }

    public void SaveGeometry()
    {
        var s = _settings.Current;
        s.TaskBoardPinned = Topmost; s.TaskBoardX = Left; s.TaskBoardY = Top;
        s.TaskBoardWidth = ActualWidth; s.TaskBoardHeight = ActualHeight;
        _ = _settings.SaveAsync();
    }

    public void Refresh()
    {
        if (!_ready) return;
        var now = DateTime.UtcNow;
        var today = DateTime.Today;
        var items = _store.Items;
        DateLabel.Text = DateTime.Now.ToString("M月d日 · dddd");
        StatsLabel.Text = $"{items.Count(t => !t.IsCompleted)} 件待办   /   {items.Count(t => t.IsOverdueAt(now))} 件逾期   /   {items.Count(t => t.IsCompleted)} 件已完成";
        PinButton.Content = Topmost ? "已置顶" : "置顶";
        var query = Search.Text.Trim();
        var visible = items.Where(t => Filter.SelectedIndex switch
        {
            0 => !t.IsCompleted,
            1 => !t.IsCompleted && t.DueUtc?.ToLocalTime().Date == today,
            2 => t.IsOverdueAt(now),
            3 => t.IsCompleted,
            _ => true,
        }).Where(t => query.Length == 0 || t.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
            || t.Details.Contains(query, StringComparison.OrdinalIgnoreCase))
          .OrderBy(t => t.IsCompleted).ThenBy(t => t.DueUtc ?? DateTime.MaxValue).ThenByDescending(t => t.CreatedUtc).ToList();
        TaskItems.ItemsSource = visible;
        EmptyState.Visibility = visible.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyLabel.Text = items.Count == 0 ? "暂时没有待办，留一点空白也很好。" : "当前筛选下没有事项。";
    }

    private void Edit(DeskTask? task) => new TaskEditorWindow(_store.Save, task) { Owner = this }.ShowDialog();
    private void AddClick(object sender, RoutedEventArgs e) => Edit(null);
    private void EditClick(object sender, RoutedEventArgs e) => Edit((DeskTask)((Button)sender).Tag);
    private void CompleteClick(object sender, RoutedEventArgs e)
    { var task = (DeskTask)((Button)sender).Tag; TryAction(() => _store.Complete(task.Id, !task.IsCompleted)); }
    private void DeleteClick(object sender, RoutedEventArgs e)
    {
        var task = (DeskTask)((Button)sender).Tag;
        if (MessageBox.Show(this, $"删除“{task.Title}”？", "删除待办", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            TryAction(() => _store.Delete(task.Id));
    }
    private void NoteClick(object sender, RoutedEventArgs e) => _notes.CreateAndShowNew();
    private void NotesClick(object sender, RoutedEventArgs e) => _notes.OpenNotesList();
    private void SettingsClick(object sender, RoutedEventArgs e) => _notes.OpenSettings();
    private void PinClick(object sender, RoutedEventArgs e) { Topmost = !Topmost; Refresh(); SaveGeometry(); }
    private void HideClick(object sender, RoutedEventArgs e) { SaveGeometry(); Hide(); }
    private void DragHeader(object sender, MouseButtonEventArgs e)
    { if (e.LeftButton == MouseButtonState.Pressed) { DragMove(); SaveGeometry(); } }
    private void FilterChanged(object sender, SelectionChangedEventArgs e) => Refresh();
    private void SearchChanged(object sender, TextChangedEventArgs e) => Refresh();
    private void MenuClick(object sender, RoutedEventArgs e)
    { var button = (Button)sender; button.ContextMenu.PlacementTarget = button; button.ContextMenu.IsOpen = true; }
    private void ExportClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "待办备份 (*.json)|*.json", FileName = $"纸间-待办-{DateTime.Now:yyyyMMdd}.json" };
        if (dialog.ShowDialog(this) == true) TryAction(() => _store.Export(dialog.FileName));
    }
    private void ImportClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "待办备份 (*.json)|*.json" };
        if (dialog.ShowDialog(this) != true) return;
        if (MessageBox.Show(this, "导入后，同一编号的待办将被备份中的内容替换，其他事项保留。", "导入待办", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            TryAction(() => _store.Import(dialog.FileName));
    }
    private void TryAction(Action action)
    { try { action(); } catch (Exception ex) { MessageBox.Show(this, "操作未完成：" + ex.Message, "纸间", MessageBoxButton.OK, MessageBoxImage.Warning); } }

    private sealed class BoardCommand(Action execute) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
