using System;
using System.IO;
using System.Net;
using Markdig;

namespace MarkRead.Services;

public class MarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline;
    private string? _templateHtml;
    private string? _cachedCss;
    private string? _cachedJs;

    public MarkdownRenderer()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAutoLinks()
            .UseTaskLists()
            .UsePipeTables()
            .Build();

        LoadAssets();
    }

    private void LoadAssets()
    {
        try
        {
            string assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            
            string templatePath = Path.Combine(assetsDir, "template.html");
            if (File.Exists(templatePath))
            {
                _templateHtml = File.ReadAllText(templatePath);
            }

            string cssPath = Path.Combine(assetsDir, "reader.css");
            if (File.Exists(cssPath))
            {
                _cachedCss = File.ReadAllText(cssPath);
            }

            string jsPath = Path.Combine(assetsDir, "reader.js");
            if (File.Exists(jsPath))
            {
                _cachedJs = File.ReadAllText(jsPath);
            }
        }
        catch
        {
            // Ignore asset reading errors, fallbacks will be used
        }

        _templateHtml ??= DefaultTemplate;
    }

    public string RenderToHtml(string markdownContent, string title, bool isRawMode, string theme = "system", int fontSize = 16, string readingWidth = "860px")
    {
        if (string.IsNullOrEmpty(_templateHtml) || string.IsNullOrEmpty(_cachedCss))
        {
            LoadAssets();
        }

        string bodyContent;
        string bodyClass = "";

        if (isRawMode)
        {
            string encoded = WebUtility.HtmlEncode(markdownContent);
            bodyContent = $"<pre class=\"raw-markdown\"><code>{encoded}</code></pre>";
            bodyClass = "raw-mode";
        }
        else
        {
            bodyContent = Markdown.ToHtml(markdownContent, _pipeline);
        }

        string themeAttr = theme switch
        {
            "dark" => "data-theme=\"dark\"",
            "light" => "",
            _ => ""
        };

        string safeTitle = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(title) ? "MarkRead" : title);

        string html = (_templateHtml ?? DefaultTemplate)
            .Replace("{{TITLE}}", safeTitle)
            .Replace("{{THEME_ATTR}}", themeAttr)
            .Replace("{{BODY_CLASS}}", bodyClass)
            .Replace("{{CONTENT}}", bodyContent);

        // Inject inlined CSS and JS so they are guaranteed to load immediately without 404 or path resolution issues
        string dynamicVars = $"\n:root {{ --base-font-size: {fontSize}px; --reading-width: {readingWidth}; }}\n";
        string inlineStyles = !string.IsNullOrEmpty(_cachedCss) 
            ? $"<style id=\"inlined-reader-css\">\n{_cachedCss}\n{dynamicVars}</style>" 
            : $"<link rel=\"stylesheet\" href=\"https://local.markread/reader.css\"><style>{dynamicVars}</style>";

        string inlineScript = !string.IsNullOrEmpty(_cachedJs) 
            ? $"<script id=\"inlined-reader-js\">\n{_cachedJs}\n</script>" 
            : "<script src=\"https://local.markread/reader.js\"></script>";

        html = html.Replace("{{INLINED_STYLES}}", inlineStyles)
                   .Replace("{{INLINED_SCRIPTS}}", inlineScript);

        return html;
    }

    private const string DefaultTemplate = @"<!DOCTYPE html>
<html lang=""en"" {{THEME_ATTR}}>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{{TITLE}}</title>
    {{INLINED_STYLES}}
</head>
<body class=""{{BODY_CLASS}}"">
    <div id=""dropzone-overlay"">
        <div class=""dropzone-card"">
            <h3>📂 Drop Markdown File Here</h3>
            <p>Open document in MarkRead</p>
        </div>
    </div>
    <main class=""reader-container"">
        {{CONTENT}}
    </main>
    {{INLINED_SCRIPTS}}
</body>
</html>";
}
