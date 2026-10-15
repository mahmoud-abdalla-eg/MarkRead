using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace MarkRead.Services;

/// <summary>
/// High-performance visual line transformer for AvalonEdit that renders rich syntax highlighting
/// for Markdown elements: distinct colors for each Heading level (H1..H6), Bold, Italic,
/// Inline Code, Code Fences, Blockquotes, Lists, Task Checkboxes, Links, and Horizontal Rules.
/// </summary>
public class MarkdownColorizer : DocumentColorizingTransformer
{
    // Distinct Heading Brushes (H1..H6)
    private static readonly SolidColorBrush H1Brush = new(Color.FromRgb(244, 63, 94));    // Rose-500
    private static readonly SolidColorBrush H2Brush = new(Color.FromRgb(245, 158, 11));   // Amber-500
    private static readonly SolidColorBrush H3Brush = new(Color.FromRgb(52, 211, 153));   // Emerald-400
    private static readonly SolidColorBrush H4Brush = new(Color.FromRgb(56, 189, 248));   // Sky-400
    private static readonly SolidColorBrush H5Brush = new(Color.FromRgb(167, 139, 250));  // Violet-400
    private static readonly SolidColorBrush H6Brush = new(Color.FromRgb(244, 114, 182));  // Pink-400

    // Content & Inline Brushes
    private static readonly SolidColorBrush CodeBlockBrush = new(Color.FromRgb(236, 72, 153));   // Hot Pink
    private static readonly SolidColorBrush InlineCodeBrush = new(Color.FromRgb(45, 212, 191));  // Teal-400
    private static readonly SolidColorBrush InlineCodeBg = new(Color.FromArgb(120, 30, 41, 59)); // Slate-800 bg
    private static readonly SolidColorBrush BlockquoteBrush = new(Color.FromRgb(192, 132, 252)); // Purple-400
    private static readonly SolidColorBrush BulletBrush = new(Color.FromRgb(56, 189, 248));      // Sky-400
    private static readonly SolidColorBrush TaskPendingBrush = new(Color.FromRgb(251, 191, 36)); // Amber-400
    private static readonly SolidColorBrush TaskDoneBrush = new(Color.FromRgb(52, 211, 153));    // Emerald-400
    private static readonly SolidColorBrush LinkTextBrush = new(Color.FromRgb(96, 165, 250));    // Blue-400
    private static readonly SolidColorBrush LinkUrlBrush = new(Color.FromRgb(148, 163, 184));    // Slate-400
    private static readonly SolidColorBrush HrBrush = new(Color.FromRgb(100, 116, 139));         // Slate-500
    private static readonly SolidColorBrush BoldBrush = new(Color.FromRgb(255, 255, 255));        // Crisp White
    private static readonly SolidColorBrush ItalicBrush = new(Color.FromRgb(253, 224, 71));       // Yellow-300
    private static readonly SolidColorBrush TablePipeBrush = new(Color.FromRgb(100, 116, 139));   // Slate-500

    static MarkdownColorizer()
    {
        H1Brush.Freeze();
        H2Brush.Freeze();
        H3Brush.Freeze();
        H4Brush.Freeze();
        H5Brush.Freeze();
        H6Brush.Freeze();
        CodeBlockBrush.Freeze();
        InlineCodeBrush.Freeze();
        InlineCodeBg.Freeze();
        BlockquoteBrush.Freeze();
        BulletBrush.Freeze();
        TaskPendingBrush.Freeze();
        TaskDoneBrush.Freeze();
        LinkTextBrush.Freeze();
        LinkUrlBrush.Freeze();
        HrBrush.Freeze();
        BoldBrush.Freeze();
        ItalicBrush.Freeze();
        TablePipeBrush.Freeze();
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        int lineStartOffset = line.Offset;
        string text = CurrentContext.Document.GetText(line);
        if (string.IsNullOrEmpty(text)) return;

        // 1. Heading 1 to 6 (at start of line)
        string trimmedStart = text.TrimStart();
        if (trimmedStart.StartsWith("#"))
        {
            if (trimmedStart.StartsWith("###### "))
            {
                ApplyStyle(lineStartOffset, lineStartOffset + text.Length, H6Brush, FontWeights.SemiBold);
                return;
            }
            if (trimmedStart.StartsWith("##### "))
            {
                ApplyStyle(lineStartOffset, lineStartOffset + text.Length, H5Brush, FontWeights.SemiBold);
                return;
            }
            if (trimmedStart.StartsWith("#### "))
            {
                ApplyStyle(lineStartOffset, lineStartOffset + text.Length, H4Brush, FontWeights.Bold);
                return;
            }
            if (trimmedStart.StartsWith("### "))
            {
                ApplyStyle(lineStartOffset, lineStartOffset + text.Length, H3Brush, FontWeights.Bold);
                return;
            }
            if (trimmedStart.StartsWith("## "))
            {
                ApplyStyle(lineStartOffset, lineStartOffset + text.Length, H2Brush, FontWeights.Bold);
                return;
            }
            if (trimmedStart.StartsWith("# "))
            {
                ApplyStyle(lineStartOffset, lineStartOffset + text.Length, H1Brush, FontWeights.Bold);
                return;
            }
        }

        // 2. Code fence ``` or ~~~
        if (trimmedStart.StartsWith("```") || trimmedStart.StartsWith("~~~"))
        {
            ApplyStyle(lineStartOffset, lineStartOffset + text.Length, CodeBlockBrush, FontWeights.Bold);
            return;
        }

        // 3. Blockquotes >
        if (trimmedStart.StartsWith(">"))
        {
            ApplyStyle(lineStartOffset, lineStartOffset + text.Length, BlockquoteBrush, fontStyle: FontStyles.Italic);
            return;
        }

        // 4. Horizontal Rules ---, ***, ___
        string trimmed = text.Trim();
        if (trimmed is "---" or "***" or "___" or "----" or "-----")
        {
            ApplyStyle(lineStartOffset, lineStartOffset + text.Length, HrBrush, FontWeights.Bold);
            return;
        }

        // 5. Task List items: - [ ] or - [x]
        var taskMatch = Regex.Match(text, @"^\s*([*\-+]\s+\[([ xX])\])");
        if (taskMatch.Success)
        {
            bool isChecked = taskMatch.Groups[2].Value is "x" or "X";
            int taskEnd = lineStartOffset + taskMatch.Groups[1].Length;
            ApplyStyle(lineStartOffset, taskEnd, isChecked ? TaskDoneBrush : TaskPendingBrush, FontWeights.Bold);
        }
        else
        {
            // 6. Regular list bullets: - item, * item, + item, 1. item
            var bulletMatch = Regex.Match(text, @"^\s*([*\-+]|\d+\.)\s");
            if (bulletMatch.Success)
            {
                int bulletEnd = lineStartOffset + bulletMatch.Length;
                ApplyStyle(lineStartOffset, bulletEnd, BulletBrush, FontWeights.SemiBold);
            }
        }

        // 7. Inline code: `code`
        MatchRegex(text, lineStartOffset, @"`([^`\r\n]+)`", InlineCodeBrush, background: InlineCodeBg);

        // 8. Bold: **text** or __text__
        MatchRegex(text, lineStartOffset, @"(\*\*|__)(.*?)\1", BoldBrush, fontWeight: FontWeights.Bold);

        // 9. Italic: *text* or _text_ (excluding double asterisks/underscores)
        MatchRegex(text, lineStartOffset, @"(?<!\*)\*(?!\*)(.*?)(?<!\*)\*(?!\*)|(?<!_)_(?!_)(.*?)(?<!_)_(?!_)", ItalicBrush, fontStyle: FontStyles.Italic);

        // 10. Links: [text](url)
        var linkMatches = Regex.Matches(text, @"\[([^\]]+)\]\(([^)]+)\)");
        foreach (Match m in linkMatches)
        {
            int linkStart = lineStartOffset + m.Index;
            int textLen = m.Groups[1].Length + 2; // [text]
            ApplyStyle(linkStart, linkStart + textLen, LinkTextBrush, FontWeights.Medium);
            ApplyStyle(linkStart + textLen, linkStart + m.Length, LinkUrlBrush);
        }

        // 11. Table Pipes |
        if (text.Contains('|'))
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '|')
                {
                    ApplyStyle(lineStartOffset + i, lineStartOffset + i + 1, TablePipeBrush, FontWeights.Bold);
                }
            }
        }
    }

    private void ApplyStyle(int startOffset, int endOffset, Brush foreground, FontWeight? fontWeight = null, FontStyle? fontStyle = null, Brush? background = null)
    {
        if (startOffset >= endOffset) return;
        ChangeLinePart(startOffset, endOffset, element =>
        {
            element.TextRunProperties.SetForegroundBrush(foreground);
            if (background != null)
            {
                element.TextRunProperties.SetBackgroundBrush(background);
            }
            if (fontWeight.HasValue || fontStyle.HasValue)
            {
                Typeface tf = element.TextRunProperties.Typeface;
                element.TextRunProperties.SetTypeface(new Typeface(
                    tf.FontFamily,
                    fontStyle ?? tf.Style,
                    fontWeight ?? tf.Weight,
                    tf.Stretch
                ));
            }
        });
    }

    private void MatchRegex(string text, int lineStartOffset, string pattern, Brush foreground, FontWeight? fontWeight = null, FontStyle? fontStyle = null, Brush? background = null)
    {
        var matches = Regex.Matches(text, pattern);
        foreach (Match m in matches)
        {
            int start = lineStartOffset + m.Index;
            int end = start + m.Length;
            ApplyStyle(start, end, foreground, fontWeight, fontStyle, background);
        }
    }
}
