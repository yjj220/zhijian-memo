using System.IO;
using StickyPad.Models;
using StickyPad.Services;

namespace StickyPad.Tests;

public sealed class DeskTaskTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "zhijian-tests-" + Guid.NewGuid());
    private string FilePath => Path.Combine(_folder, "tasks.json");
    private static readonly DateTime Now = new(2026, 10, 25, 8, 0, 0, DateTimeKind.Utc);
    public void Dispose() { if (Directory.Exists(_folder)) Directory.Delete(_folder, true); }

    [Fact]
    public void OverdueReminderSurvivesRestart_AndAcknowledgementPreventsRepeat()
    {
        var store = new DeskTaskStore(FilePath);
        var task = new DeskTask { Title = "中文待办", DueUtc = Now.AddHours(-2), ReminderUtc = Now.AddHours(-3) };
        store.Save(task);
        var restored = new DeskTaskStore(FilePath);
        Assert.True(Assert.Single(restored.Items).NeedsReminder(Now));
        restored.Acknowledge(task.Id);
        Assert.False(Assert.Single(new DeskTaskStore(FilePath).Items).NeedsReminder(Now.AddDays(1)));
    }

    [Fact]
    public void SnoozeRetainsDeadline_AndOnlyFiresAtNewReminderTime()
    {
        var store = new DeskTaskStore(FilePath);
        var task = new DeskTask { Title = "会议", DueUtc = Now, ReminderUtc = Now };
        store.Save(task);
        store.Snooze(task.Id, Now);
        var restored = Assert.Single(new DeskTaskStore(FilePath).Items);
        Assert.Equal(Now, restored.DueUtc);
        Assert.False(restored.NeedsReminder(Now.AddMinutes(9)));
        Assert.True(restored.NeedsReminder(Now.AddMinutes(10)));
    }

    [Fact]
    public void CompletedDeletedAndUnscheduledTasksDoNotRemind()
    {
        var store = new DeskTaskStore(FilePath);
        var task = new DeskTask { Title = "完成", ReminderUtc = Now };
        store.Save(task);
        store.Complete(task.Id, true);
        Assert.False(Assert.Single(store.Items).NeedsReminder(Now));
        store.Complete(task.Id, false);
        Assert.True(Assert.Single(store.Items).NeedsReminder(Now));
        store.Delete(task.Id);
        Assert.Empty(new DeskTaskStore(FilePath).Items);
        Assert.False(new DeskTask { Title = "无时间" }.NeedsReminder(Now));
        Assert.False(new DeskTask { ReminderUtc = Now.AddSeconds(1) }.NeedsReminder(Now));
    }

    [Fact]
    public void CorruptPrimaryRecoversLastBackup_AndPreservesDamagedFile()
    {
        var store = new DeskTaskStore(FilePath);
        var task = new DeskTask { Title = "原始内容" };
        store.Save(task);
        store.Save(task with { Title = "更新内容" });
        File.WriteAllText(FilePath, "{broken");
        var restored = new DeskTaskStore(FilePath);
        Assert.Equal("原始内容", Assert.Single(restored.Items).Title);
        Assert.NotNull(restored.RecoveryMessage);
        Assert.Single(Directory.GetFiles(_folder, "*.damaged-*"));
    }

    [Fact]
    public void InvalidImportDoesNotReplaceExistingData_ValidImportMerges()
    {
        var store = new DeskTaskStore(FilePath);
        var task = new DeskTask { Title = "保留我" };
        store.Save(task);
        var backup = Path.Combine(_folder, "import.json");
        File.WriteAllText(backup, "[{}]");
        Assert.Throws<InvalidDataException>(() => store.Import(backup));
        Assert.Equal(task.Id, Assert.Single(store.Items).Id);
        store.Export(backup);
        store.Save(new DeskTask { Title = "新事项" });
        store.Import(backup);
        Assert.Equal(2, new DeskTaskStore(FilePath).Items.Count);
    }

    [Fact]
    public void WriteFailureDoesNotPublishUnsavedChanges()
    {
        var store = new DeskTaskStore(FilePath);
        var task = new DeskTask { Title = "已保存" };
        store.Save(task);
        Directory.CreateDirectory(FilePath + ".tmp");
        Assert.Throws<UnauthorizedAccessException>(() => store.Save(task with { Title = "不能保存" }));
        Assert.Equal("已保存", Assert.Single(store.Items).Title);
        Assert.Equal("已保存", Assert.Single(new DeskTaskStore(FilePath).Items).Title);
    }
}
