using System.Text.Json.Serialization;

namespace StickyPad.Models;

/// Times are persisted as UTC; UI input and display use the Windows local time zone.
public sealed record DeskTask
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; init; } = "";
    public string Details { get; init; } = "";
    public DateTime? DueUtc { get; init; }
    public DateTime? ReminderUtc { get; init; }
    public bool ReminderAcknowledged { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    public bool NeedsReminder(DateTime utcNow) => !IsCompleted && !ReminderAcknowledged
        && ReminderUtc is { } reminder && reminder <= utcNow;
    public bool IsOverdueAt(DateTime utcNow) => !IsCompleted && DueUtc is { } due && due < utcNow;
    [JsonIgnore] public string CheckGlyph => IsCompleted ? "✓" : "";
    [JsonIgnore]
    public string DueLabel => IsCompleted ? "已完成" : DueUtc is { } due
        ? $"{(due < DateTime.UtcNow ? "已逾期 · " : "截止 ")}{due.ToLocalTime():MM月dd日 HH:mm}"
        : "未设置截止时间";
    [JsonIgnore] public string ReminderLabel => ReminderUtc is { } time && !IsCompleted
        ? (ReminderAcknowledged ? "本次提醒已处理" : $"提醒 {time.ToLocalTime():MM/dd HH:mm}") : "";
    [JsonIgnore] public string StatusColor => IsCompleted ? "#87938B" : IsOverdueAt(DateTime.UtcNow) ? "#BA4E3F" : "#698175";
    [JsonIgnore] public double CardOpacity => IsCompleted ? 0.65 : 1;
}
