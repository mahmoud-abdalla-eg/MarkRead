import React, { useState, useEffect, useRef } from 'react';
import { parseMarkdown } from './lib/markdown';
import { SAMPLE_MARKDOWN } from './lib/sampleMarkdown';
import { TokenBucketLimiter, DropThrottler } from './lib/rateLimiter';
import html2pdf from 'html2pdf.js';
import './App.css';

const STORAGE_KEY_MD = 'markread_draft_content';
const STORAGE_KEY_FILENAME = 'markread_draft_filename';
const STORAGE_KEY_THEME = 'markread_reader_theme';
const STORAGE_KEY_DARK = 'markread_dark_mode';
const STORAGE_KEY_VIEW = 'markread_view_mode';

export function App() {
  // State with LocalStorage Persistence
  const [markdown, setMarkdown] = useState<string>(() => {
    return localStorage.getItem(STORAGE_KEY_MD) || SAMPLE_MARKDOWN;
  });
  const [theme, setTheme] = useState<'modern' | 'github' | 'academic'>(() => {
    return (localStorage.getItem(STORAGE_KEY_THEME) as any) || 'modern';
  });
  const [isDark, setIsDark] = useState<boolean>(() => {
    const saved = localStorage.getItem(STORAGE_KEY_DARK);
    return saved !== null ? saved === 'true' : true;
  });
  const [viewMode, setViewMode] = useState<'split' | 'reader' | 'editor'>(() => {
    if (typeof window !== 'undefined' && window.innerWidth < 768) return 'reader';
    return (localStorage.getItem(STORAGE_KEY_VIEW) as any) || 'split';
  });
  const [fileName, setFileName] = useState<string>(() => {
    return localStorage.getItem(STORAGE_KEY_FILENAME) || 'EXPLANATION_FOR_YOU.md';
  });

  const [isDragging, setIsDragging] = useState<boolean>(false);
  const [isGeneratingPdf, setIsGeneratingPdf] = useState<boolean>(false);

  // Rate Limiting States
  const [pdfCooldown, setPdfCooldown] = useState<number>(0);
  const [toast, setToast] = useState<{ message: string; type?: 'warning' | 'error' } | null>(null);

  // Undo / Redo History Stack
  const [history, setHistory] = useState<string[]>([markdown]);
  const [historyIndex, setHistoryIndex] = useState<number>(0);
  const debounceTimerRef = useRef<any>(null);

  const pdfLimiter = useRef(new TokenBucketLimiter(3, 5000, 15));
  const dropLimiter = useRef(new DropThrottler(3, 5000));
  const toastTimeoutRef = useRef<any>(null);

  const fileInputRef = useRef<HTMLInputElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Sync color mode attribute
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');
    localStorage.setItem(STORAGE_KEY_DARK, String(isDark));
  }, [isDark]);

  // Persist Markdown Draft
  useEffect(() => {
    localStorage.setItem(STORAGE_KEY_MD, markdown);
  }, [markdown]);

  // Persist File Name
  useEffect(() => {
    localStorage.setItem(STORAGE_KEY_FILENAME, fileName);
  }, [fileName]);

  // Persist Reader Theme
  useEffect(() => {
    localStorage.setItem(STORAGE_KEY_THEME, theme);
  }, [theme]);

  // Persist View Mode
  useEffect(() => {
    localStorage.setItem(STORAGE_KEY_VIEW, viewMode);
  }, [viewMode]);

  // Handle Mobile Screen Resize
  useEffect(() => {
    const handleResize = () => {
      if (window.innerWidth < 768) {
        setViewMode((current) => (current === 'split' ? 'reader' : current));
      }
    };
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  // Cooldown countdown timer
  useEffect(() => {
    if (pdfCooldown <= 0) return;
    const interval = setInterval(() => {
      setPdfCooldown((prev) => {
        if (prev <= 1) {
          clearInterval(interval);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
    return () => clearInterval(interval);
  }, [pdfCooldown]);

  const showToast = (message: string, type: 'warning' | 'error' = 'warning') => {
    if (toastTimeoutRef.current) {
      clearTimeout(toastTimeoutRef.current);
    }
    setToast({ message, type });
    toastTimeoutRef.current = setTimeout(() => {
      setToast(null);
    }, 4500);
  };

  // Push new state into history
  const pushHistory = (newText: string, immediate: boolean = false) => {
    if (immediate) {
      if (debounceTimerRef.current) clearTimeout(debounceTimerRef.current);
      setHistory((prev) => {
        const sliced = prev.slice(0, historyIndex + 1);
        if (sliced.length >= 100) sliced.shift();
        return [...sliced, newText];
      });
      setHistoryIndex((prev) => prev + 1);
    } else {
      if (debounceTimerRef.current) clearTimeout(debounceTimerRef.current);
      debounceTimerRef.current = setTimeout(() => {
        setHistory((prev) => {
          const sliced = prev.slice(0, historyIndex + 1);
          if (sliced.length >= 100) sliced.shift();
          return [...sliced, newText];
        });
        setHistoryIndex((prev) => prev + 1);
      }, 350);
    }
  };

  // Undo Action
  const handleUndo = () => {
    if (historyIndex > 0) {
      const targetIndex = historyIndex - 1;
      setHistoryIndex(targetIndex);
      setMarkdown(history[targetIndex]);
    }
  };

  // Redo Action
  const handleRedo = () => {
    if (historyIndex < history.length - 1) {
      const targetIndex = historyIndex + 1;
      setHistoryIndex(targetIndex);
      setMarkdown(history[targetIndex]);
    }
  };

  // Textarea input change
  const handleTextareaChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const val = e.target.value;
    setMarkdown(val);
    pushHistory(val, false);
  };

  // Keyboard Shortcuts: Ctrl+Z (Undo) and Ctrl+Y / Ctrl+Shift+Z (Redo)
  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    // Ctrl+Z or Cmd+Z (Undo)
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'z' && !e.shiftKey) {
      e.preventDefault();
      handleUndo();
      return;
    }

    // Redo: Ctrl+Y, Cmd+Y, or Ctrl+Shift+Z, Cmd+Shift+Z
    if (
      ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'y') ||
      ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key.toLowerCase() === 'z')
    ) {
      e.preventDefault();
      handleRedo();
      return;
    }
  };

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

    // Throttle rapid drop spam
    if (!dropLimiter.current.allowDrop()) {
      showToast('Drop rate limit active: Please wait a moment between file uploads.', 'warning');
      return;
    }

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
        const content = event.target.result;
        setMarkdown(content);
        setFileName(file.name);
        pushHistory(content, true);
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
    pushHistory(nextValue, true);

    setTimeout(() => {
      textarea.focus();
      textarea.setSelectionRange(start + before.length, start + before.length + (selected.length || 4));
    }, 0);
  };

  // Export to PDF with Token Bucket Rate Limiter & Direct Browser Download
  const handleExportPdf = async () => {
    const result = pdfLimiter.current.tryConsume();
    if (!result.allowed) {
      setPdfCooldown(result.cooldownRemainingSeconds);
      showToast(
        `Rate limit active: Please wait ${result.cooldownRemainingSeconds}s before exporting again.`,
        'warning'
      );
      return;
    }

    setIsGeneratingPdf(true);
    showToast('Generating and downloading PDF file...', 'warning');

    const targetFileName = fileName.replace(/\.[^/.]+$/, '') + '.pdf';

    try {
      // Create isolated export container with clean A4 print styles
      const exportContainer = document.createElement('div');
      exportContainer.className = `pdf-export-container theme-${theme}`;
      exportContainer.innerHTML = htmlContent;
      document.body.appendChild(exportContainer);

      const opt = {
        margin: [12, 14, 12, 14] as [number, number, number, number],
        filename: targetFileName,
        image: { type: 'jpeg' as const, quality: 0.98 },
        html2canvas: {
          scale: 2,
          useCORS: true,
          logging: false,
          backgroundColor: '#ffffff',
        },
        jsPDF: {
          unit: 'mm',
          format: 'a4',
          orientation: 'portrait' as const,
        },
        pagebreak: {
          mode: ['avoid-all', 'css', 'legacy'],
          avoid: ['tr', 'pre', 'blockquote', 'img', 'h1', 'h2', 'h3'],
        },
      };

      // Generate and trigger an authentic browser file download!
      // This registers in Chrome's Download Tray and chrome://downloads tab!
      await html2pdf().set(opt).from(exportContainer).save();
      document.body.removeChild(exportContainer);
      showToast(`Downloaded "${targetFileName}" — check Chrome Downloads!`, 'warning');
    } catch (err: any) {
      console.error('PDF direct download error, falling back to print dialog:', err);
      showToast('Downloading via print fallback...', 'warning');
      window.print();
    } finally {
      setIsGeneratingPdf(false);
    }
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
          {/* View Mode Segmented Control */}
          <div className="segmented-control">
            <button
              className={`seg-btn ${viewMode === 'split' ? 'active' : ''}`}
              onClick={() => setViewMode('split')}
              data-mode="split"
              title="Side-by-side Editor & Preview"
            >
              ◫ Split
            </button>
            <button
              className={`seg-btn ${viewMode === 'reader' ? 'active' : ''}`}
              onClick={() => setViewMode('reader')}
              data-mode="reader"
              title="Full-width Reading Mode"
            >
              📖 Reader
            </button>
            <button
              className={`seg-btn ${viewMode === 'editor' ? 'active' : ''}`}
              onClick={() => setViewMode('editor')}
              data-mode="editor"
              title="Full-width Editor Mode"
            >
              ✏️ Editor
            </button>
          </div>

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
              pushHistory(SAMPLE_MARKDOWN, true);
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

          {/* Export to PDF with Rate-Limiting Protection & Direct Browser Download */}
          {isGeneratingPdf ? (
            <button className="btn btn-cooldown" disabled title="Generating and preparing PDF download...">
              ⏳ Generating...
            </button>
          ) : pdfCooldown > 0 ? (
            <button
              className="btn btn-cooldown"
              disabled
              title={`Rate limit active. Please wait ${pdfCooldown} seconds.`}
            >
              ⏳ Cooldown <span className="cooldown-badge">{pdfCooldown}s</span>
            </button>
          ) : (
            <button
              className="btn btn-primary"
              onClick={handleExportPdf}
              title="Generate and download clean publication-grade PDF directly into Chrome downloads"
            >
              📥 Download PDF
            </button>
          )}
        </div>
      </header>

      {/* Drop Zone Banner */}
      <div className={`dropzone-banner ${isDragging ? 'dragging' : ''}`}>
        <span className="dropzone-text">
          📂 <strong>Drag and drop</strong> any Markdown file (<code>.md</code>, <code>.markdown</code>, <code>.txt</code>) anywhere on this window to view and edit.
        </span>
      </div>

      {/* Main Workspace: Split Editor & Preview */}
      <main className={`workspace mode-${viewMode}`}>
        {/* Left: Editor */}
        <section className="editor-panel">
          <div className="panel-header">
            <span className="panel-title">Editor ({fileName})</span>
            <div className="editor-toolbar">
              {/* Undo / Redo buttons */}
              <button
                className="tool-btn"
                onClick={handleUndo}
                disabled={historyIndex <= 0}
                style={{ opacity: historyIndex <= 0 ? 0.4 : 1 }}
                title="Undo edit (Ctrl+Z)"
              >
                ↩ Undo
              </button>
              <button
                className="tool-btn"
                onClick={handleRedo}
                disabled={historyIndex >= history.length - 1}
                style={{ opacity: historyIndex >= history.length - 1 ? 0.4 : 1 }}
                title="Redo edit (Ctrl+Y or Ctrl+Shift+Z)"
              >
                ↪ Redo
              </button>

              {/* Formatting tools */}
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
            onChange={handleTextareaChange}
            onKeyDown={handleKeyDown}
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

      {/* Floating Toast Notification Container */}
      {toast && (
        <div className="toast-container">
          <div className={`toast-alert ${toast.type || ''}`}>
            <div className="toast-content">
              <span>⚠️</span>
              <span>{toast.message}</span>
            </div>
            <button className="toast-close" onClick={() => setToast(null)}>✕</button>
          </div>
        </div>
      )}
    </div>
  );
}

export default App;
