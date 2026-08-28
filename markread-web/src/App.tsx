import React, { useState, useEffect, useRef } from 'react';
import { parseMarkdown } from './lib/markdown';
import { SAMPLE_MARKDOWN } from './lib/sampleMarkdown';
import './App.css';

export function App() {
  const [markdown, setMarkdown] = useState<string>(SAMPLE_MARKDOWN);
  const [theme, setTheme] = useState<'modern' | 'github' | 'academic'>('modern');
  const [isDark, setIsDark] = useState<boolean>(true);
  const [isDragging, setIsDragging] = useState<boolean>(false);
  const [fileName, setFileName] = useState<string>('EXPLANATION_FOR_YOU.md');
  const fileInputRef = useRef<HTMLInputElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Sync color mode attribute
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');
  }, [isDark]);

  // Handle Drag & Drop
  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    if (!e.currentTarget.contains(e.relatedTarget as Node)) {
      setIsDragging(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);

    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      const file = e.dataTransfer.files[0];
      readFile(file);
    }
  };

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      const file = e.target.files[0];
      readFile(file);
    }
  };

  const readFile = (file: File) => {
    const reader = new FileReader();
    reader.onload = (event) => {
      if (typeof event.target?.result === 'string') {
        setMarkdown(event.target.result);
        setFileName(file.name);
      }
    };
    reader.readAsText(file);
  };

  // Quick Editor Formatting
  const insertFormat = (before: string, after: string = '') => {
    if (!textareaRef.current) return;
    const textarea = textareaRef.current;
    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const selected = textarea.value.substring(start, end);
    const replacement = `${before}${selected || 'text'}${after}`;

    const nextValue =
      textarea.value.substring(0, start) + replacement + textarea.value.substring(end);
    setMarkdown(nextValue);

    setTimeout(() => {
      textarea.focus();
      textarea.setSelectionRange(start + before.length, start + before.length + (selected.length || 4));
    }, 0);
  };

  // Export to PDF
  const handleExportPdf = () => {
    const originalTitle = document.title;
    document.title = fileName.replace(/\.[^/.]+$/, '') + '.pdf';
    window.print();
    document.title = originalTitle;
  };

  const htmlContent = parseMarkdown(markdown);
  const wordCount = markdown.trim().split(/\s+/).filter(Boolean).length;
  const charCount = markdown.length;

  return (
    <div className="app-container" onDragOver={handleDragOver} onDragLeave={handleDragLeave} onDrop={handleDrop}>
      {/* Header */}
      <header className="app-header">
        <div className="header-brand">
          <div className="brand-icon">M↓</div>
          <div>
            <span className="brand-title">MarkRead</span>
            <span className="brand-badge" style={{ marginLeft: 8 }}>Web</span>
          </div>
        </div>

        <div className="header-actions">
          {/* Theme Selector */}
          <select
            className="select-input"
            value={theme}
            onChange={(e) => setTheme(e.target.value as any)}
            title="Choose Reading & PDF Typography Style"
          >
            <option value="modern">Theme: Modern Clean</option>
            <option value="github">Theme: GitHub Style</option>
            <option value="academic">Theme: Academic Paper</option>
          </select>

          {/* Dark Mode Toggle */}
          <button
            className="btn"
            onClick={() => setIsDark(!isDark)}
            title="Toggle Dark / Light Mode"
          >
            {isDark ? '☀️ Light' : '🌙 Dark'}
          </button>

          {/* Load Sample Document */}
          <button
            className="btn"
            onClick={() => {
              setMarkdown(SAMPLE_MARKDOWN);
              setFileName('Antispam_Case_Review.md');
            }}
            title="Load comprehensive markdown demo"
          >
            📄 Sample
          </button>

          {/* Select Local File */}
          <input
            type="file"
            ref={fileInputRef}
            onChange={handleFileInput}
            accept=".md,.markdown,.txt"
            style={{ display: 'none' }}
          />
          <button
            className="btn"
            onClick={() => fileInputRef.current?.click()}
            title="Open markdown file from your computer"
          >
            📂 Open File
          </button>

          {/* Export to PDF */}
          <button
            className="btn btn-primary"
            onClick={handleExportPdf}
            title="Export clean publication-grade PDF without headers (mdtopdf.pro style)"
          >
            📥 Export PDF
          </button>
        </div>
      </header>

      {/* Drop Zone Banner */}
      <div className={`dropzone-banner ${isDragging ? 'dragging' : ''}`}>
        <span className="dropzone-text">
          📂 <strong>Drag and drop</strong> any Markdown file (<code>.md</code>, <code>.markdown</code>, <code>.txt</code>) anywhere on this window to view and edit.
        </span>
      </div>

      {/* Main Workspace: Split Editor & Preview */}
      <main className="workspace">
        {/* Left: Editor */}
        <section className="editor-panel">
          <div className="panel-header">
            <span className="panel-title">Editor ({fileName})</span>
            <div className="editor-toolbar">
              <button className="tool-btn" onClick={() => insertFormat('**', '**')} title="Bold">B</button>
              <button className="tool-btn" onClick={() => insertFormat('*', '*')} title="Italic">I</button>
              <button className="tool-btn" onClick={() => insertFormat('## ')} title="Heading 2">H2</button>
              <button className="tool-btn" onClick={() => insertFormat('- ')} title="List">• List</button>
              <button className="tool-btn" onClick={() => insertFormat('- [ ] ')} title="Task List">☑ Task</button>
              <button className="tool-btn" onClick={() => insertFormat('```python\n', '\n```')} title="Code Block">&lt;/&gt;</button>
              <button className="tool-btn" onClick={() => insertFormat('> ')} title="Blockquote">” Quote</button>
              <button className="tool-btn" onClick={() => insertFormat('[', '](https://)')} title="Link">Link</button>
            </div>
            <span style={{ fontSize: 11, color: 'var(--text-muted)' }}>
              {wordCount} words · {charCount} chars
            </span>
          </div>

          <textarea
            ref={textareaRef}
            className="editor-textarea"
            value={markdown}
            onChange={(e) => setMarkdown(e.target.value)}
            placeholder="Type or paste your Markdown content here..."
            spellCheck="false"
          />
        </section>

        {/* Right: Live Formatted Preview */}
        <section className="preview-panel">
          <div className="panel-header">
            <span className="panel-title">Live Preview &amp; PDF Target</span>
            <span style={{ fontSize: 11, color: 'var(--text-muted)' }}>
              Style: {theme.charAt(0).toUpperCase() + theme.slice(1)} · A4 Print Ready
            </span>
          </div>

          <div
            className={`preview-content theme-${theme}`}
            dangerouslySetInnerHTML={{ __html: htmlContent }}
          />
        </section>

        {/* Drag Overlay */}
        {isDragging && (
          <div className="drag-overlay">
            <div className="drag-card">
              <h2 style={{ fontSize: 24, marginBottom: 8 }}>📂 Drop Markdown File Here</h2>
              <p style={{ color: 'var(--text-muted)' }}>Open document instantly in MarkRead Web</p>
            </div>
          </div>
        )}
      </main>
    </div>
  );
}

export default App;
