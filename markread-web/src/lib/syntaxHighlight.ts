import Prism from 'prismjs';

// Import essential languages
import 'prismjs/components/prism-javascript';
import 'prismjs/components/prism-typescript';
import 'prismjs/components/prism-python';
import 'prismjs/components/prism-csharp';
import 'prismjs/components/prism-bash';
import 'prismjs/components/prism-json';
import 'prismjs/components/prism-markdown';
import 'prismjs/components/prism-css';
import 'prismjs/components/prism-sql';
import 'prismjs/components/prism-yaml';

const ALIAS_MAP: Record<string, string> = {
  js: 'javascript',
  ts: 'typescript',
  py: 'python',
  cs: 'csharp',
  sh: 'bash',
  shell: 'bash',
  zsh: 'bash',
  yml: 'yaml',
  md: 'markdown',
  html: 'markup',
  xml: 'markup',
  svg: 'markup',
};

function escapeHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

/**
 * Highlights a block of code using Prism.js with fallbacks.
 */
export function highlightCode(code: string, language: string): string {
  const normalized = (language || '').toLowerCase().trim();
  const targetLang = ALIAS_MAP[normalized] || normalized;

  if (targetLang && Prism.languages[targetLang]) {
    try {
      return Prism.highlight(code, Prism.languages[targetLang], targetLang);
    } catch {
      return escapeHtml(code);
    }
  }

  return escapeHtml(code);
}
