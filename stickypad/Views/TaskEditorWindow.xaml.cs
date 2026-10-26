using System.Globalization;
using System.Windows;
using StickyPad.Models;

namespace StickyPad.Views;

public partial class TaskEditorWindow : Window
{
    private readonly DeskTask? _original;
    private readonly Action<DeskTask> _save;
    private readonly DateTime? _originalDue;
    private readonly int _originalChoice;
    private static readonly int[] LeadMinutes = [0, 5, 15, 60, 1440];

    public TaskEditorWindow(Action<DeskTask> save, DeskTask? original = null)
    {
        InitializeComponent();
        _save = save;
        _original = original;
        TitleInput.Text = original?.Title ?? "";
        DetailsInput.Text = original?.Details ?? "";
        var local = original?.DueUtc?.ToLocalTime() ?? DateTime.Now.AddHours(1);
        DueDate.SelectedDate = local.Date;
        DueTime.Text = local.ToString("HH:mm");
        HasDue.IsChecked = original?.DueUtc is not null;
        if (original is not null)
        {
            var lead = original.DueUtc is { } due && original.ReminderUtc is { } reminder
                ? (int)(due - reminder).TotalMinutes : -1;
            var index = Array.IndexOf(LeadMinutes, lead);
            ReminderChoice.SelectedIndex = original.ReminderUtc is null ? 5 : Math.Max(0, index);
        }
        _originalDue = original?.DueUtc;
        _originalChoice = ReminderChoice.SelectedIndex;
        Loaded += (_, _) => TitleInput.Focus();
    }

    private void DueChanged(object sender, RoutedEventArgs e) { }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        if (string.IsNullOrWhiteSpace(TitleInput.Text)) { ErrorText.Text = "请先写下待办事项。"; return; }
        DateTime? due = null;
        if (HasDue.IsChecked == true)
        {
            if (DueDate.SelectedDate is not { } date || !TimeOnly.TryParseExact(DueTime.Text.Trim(),
                ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            { ErrorText.Text = "请选择日期，并填写有效时间，例如 09:30。"; return; }
            var local = DateTime.SpecifyKind(date.Date + time.ToTimeSpan(), DateTimeKind.Unspecified);
            if (TimeZoneInfo.Local.IsInvalidTime(local)) { ErrorText.Text = "该时间处于夏令时跳转区间，请选择其他时间。"; return; }
            due = TimeZoneInfo.ConvertTimeToUtc(local);
        }
        DateTime? reminder = due is { } value && ReminderChoice.SelectedIndex < 5
            ? value.AddMinutes(-LeadMinutes[ReminderChoice.SelectedIndex]) : null;
        var sameSchedule = due == _originalDue && ReminderChoice.SelectedIndex == _originalChoice;
        var task = (_original ?? new DeskTask()) with
        {
            Title = TitleInput.Text, Details = DetailsInput.Text, DueUtc = due,
            ReminderUtc = sameSchedule && _original is not null ? _original.ReminderUtc : reminder,
            ReminderAcknowledged = sameSchedule && (_original?.ReminderAcknowledged ?? false),
        };
        try { _save(task); DialogResult = true; }
        catch (Exception ex) { ErrorText.Text = "保存失败，内容仍在此处：" + ex.Message; }
    }
}
