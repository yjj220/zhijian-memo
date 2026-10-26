using System.Windows;

namespace StickyPad.Services;

/// This fork must never replace itself with the upstream executable.
public sealed class LocalBuildUpdateService : IUpdateService
{
    public Task CheckAsync(bool userInitiated)
    {
        if (userInitiated) MessageBox.Show("纸间 2.4.2\n\n已统一弹窗名称。便签隐藏后保留，包括空白便签。只有明确删除才移入回收站。\n\n基于 StickyPad（MIT 许可）定制。", "纸间");
        return Task.CompletedTask;
    }
}
