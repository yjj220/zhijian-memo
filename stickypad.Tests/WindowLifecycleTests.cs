using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StickyPad.Services;
using StickyPad.Views;

namespace StickyPad.Tests;

public sealed class WindowLifecycleTests
{
    [Fact]
    public void HideRestoreAndReloadPreserveBlankAndEditedNotes_OnlyDeleteRemovesThem()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            var folder = Path.Combine(Path.GetTempPath(), "zhijian-window-test-" + Guid.NewGuid());
            // Use the base Application: App.OnStartup would connect to the user's real instance.
            var app = new Application();
            app.Resources["ColorToBrush"] = new StickyPad.Utils.ColorToBrushConverter();
            app.Resources["BoolToVis"] = new StickyPad.Utils.BoolToVisibilityConverter();
            app.Resources["InverseBool"] = new StickyPad.Utils.InverseBoolConverter();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            // Pump real Loaded/Closing/visibility events and asynchronous save continuations.
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(async () =>
            {
                try
                {
                    using var repository = new NoteRepository(Path.Combine(folder, "notes.db"));
                    using var services = new ServiceCollection().BuildServiceProvider();
                    using var loggerFactory = LoggerFactory.Create(_ => { });
                    var manager = new WindowManager(repository, services, loggerFactory);
                    var blank = manager.CreateAndShowNew();
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    var blankId = blank.ViewModel.Id;
                    var events = new List<string>();
                    blank.Closing += (_, e) => events.Add($"Closing cancelled={e.Cancel}");
                    blank.Closed += (_, _) => events.Add("Closed");

                    blank.Close(); // Actual X button / Alt+F4 route, not Window.Hide().
                    await blank.ViewModel.FlushAsync();
                    Assert.False(blank.IsVisible);
                    Assert.True(manager.TryGetLiveNoteContent(blankId, out _, out _), string.Join("; ", events));
                    var savedBlank = await repository.GetByIdAsync(blankId);
                    Assert.NotNull(savedBlank);
                    Assert.True(savedBlank.IsHidden);

                    await manager.ShowAllAsync();
                    await manager.ShowAllAsync();
                    Assert.True(blank.IsVisible);
                    Assert.Same(blank, Assert.Single(app.Windows.OfType<NoteWindow>()));

                    blank.Editor.AppendText("输入后立即隐藏，文字不能消失。\n第二行。");
                    blank.Close(); // Close before the 500 ms autosave delay.
                    await blank.ViewModel.FlushAsync();
                    var saved = await repository.GetByIdAsync(blankId);
                    Assert.Contains("文字不能消失", saved!.PlainText);
                    await manager.ShowAllAsync();
                    Assert.Contains("文字不能消失", new TextRange(blank.Editor.Document.ContentStart, blank.Editor.Document.ContentEnd).Text);

                    manager.HideAll();
                    Assert.False(blank.IsVisible);
                    await manager.ShowAllAsync();
                    Assert.True(blank.IsVisible);
                    blank.WindowState = WindowState.Minimized;
                    await manager.ShowAllAsync();
                    Assert.Equal(WindowState.Normal, blank.WindowState);

                    // Recover the same saved note if its native window was lost.
                    await blank.ViewModel.FlushAsync();
                    blank.RequestClose();
                    Assert.False(manager.TryGetLiveNoteContent(blankId, out _, out _));
                    await manager.ShowAllAsync();
                    var recovered = Assert.Single(app.Windows.OfType<NoteWindow>());
                    Assert.Equal(blankId, recovered.ViewModel.Id);
                    Assert.True(recovered.IsVisible);
                    Assert.Contains("文字不能消失", recovered.ViewModel.PlainText);

                    // Hidden state survives the same restoration path used at startup.
                    recovered.Close();
                    await recovered.ViewModel.FlushAsync();
                    await manager.ReloadAsync();
                    var reloaded = Assert.Single(app.Windows.OfType<NoteWindow>());
                    Assert.False(reloaded.IsVisible);
                    await manager.ShowAllAsync();
                    Assert.True(reloaded.IsVisible);
                    Assert.Equal(blankId, reloaded.ViewModel.Id);

                    await reloaded.ViewModel.DeleteCommand.ExecuteAsync(null);
                    await manager.ShowAllAsync();
                    Assert.Empty(app.Windows.OfType<NoteWindow>());
                    Assert.Null(await repository.GetByIdAsync(blankId));
                    var trashed = Assert.Single(await repository.GetTrashedAsync());
                    Assert.Contains("文字不能消失", trashed.PlainText);

                    // No notes means no silent creation on icon clicks.
                    await manager.ShowAllAsync();
                    Assert.Empty(await repository.GetAllAsync());
                }
                catch (Exception ex) { failure = ex; }
                finally
                {
                    foreach (var window in app.Windows.OfType<NoteWindow>().ToArray()) window.RequestClose();
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                }
            }));
            Dispatcher.Run();
            try { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
            catch (Exception ex) { failure ??= ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Desktop lifecycle test timed out.");
        Assert.Null(failure);
    }
}
