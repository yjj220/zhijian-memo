using System;
using System.Collections.Generic;
using StickyPad.Resources;

namespace StickyPad.Models;

/// 내장 노트 템플릿. Name 은 현재 언어로 지연 평가(리소스), Content 의 "{{date}}" 는 생성 시 오늘 날짜로 치환.
public sealed record NoteTemplate(Func<string> Name, string Content, NoteContentFormat Format, NoteColor Color);

public static class NoteTemplates
{
    public static IReadOnlyList<NoteTemplate> All { get; } = new List<NoteTemplate>
    {
        new(() => Strings.Template_DailyLog,
            "# {{date}}\n\n## 今日清单\n- [ ] \n\n## 随手记\n", NoteContentFormat.Markdown, NoteColor.Blue),
        new(() => Strings.Template_Meeting,
            "# 会议记录 — {{date}}\n\n参会人：\n\n## 议程\n- \n\n## 决定\n- \n\n## 后续事项\n- [ ] ",
            NoteContentFormat.Markdown, NoteColor.Green),
        new(() => Strings.Template_Todo,
            "# 勾选清单\n\n- [ ] \n- [ ] \n- [ ] ", NoteContentFormat.Markdown, NoteColor.Yellow),
    };
}
