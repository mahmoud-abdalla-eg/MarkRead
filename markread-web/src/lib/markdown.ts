/**
 * High-performance, zero-dependency GitHub-Flavored Markdown Parser
 * Produces clean, accessible HTML with support for tables, task lists, and syntax blocks.
 */

export function parseMarkdown(md: string): string {
    if (!md) return '';

    const lines = md.split(/\r?\n/);
    const html: string[] = [];
    let inCodeBlock = false;
    let codeLanguage = '';
    let codeBuffer: string[] = [];
    let inTable = false;
    let tableBuffer: string[] = [];
    let inBlockquote = false;
    let blockquoteBuffer: string[] = [];
    let inUl = false;
    let inOl = false;

    function flushBlockquote() {
        if (inBlockquote) {
            const inner = parseMarkdown(blockquoteBuffer.join('\n'));
            html.push(`<blockquote>${inner}</blockquote>`);
            blockquoteBuffer = [];
            inBlockquote = false;
        }
    }

    function flushList() {
        if (inUl) {
            html.push('</ul>');
            inUl = false;
        }
        if (inOl) {
            html.push('</ol>');
            inOl = false;
        }
    }

    function flushTable() {
        if (inTable) {
            html.push(renderTable(tableBuffer));
            tableBuffer = [];
            inTable = false;
        }
    }

    for (let i = 0; i < lines.length; i++) {
        const line = lines[i];

        // 1. Code block fence (```)
        if (line.trim().startsWith('```')) {
            flushList();
            flushTable();
            flushBlockquote();

            if (inCodeBlock) {
                const escaped = escapeHtml(codeBuffer.join('\n'));
                const langClass = codeLanguage ? ` class="language-${codeLanguage}"` : '';
                html.push(
                    `<pre><button class="copy-btn" onclick="navigator.clipboard.writeText(this.parentElement.querySelector('code').innerText);this.innerText='Copied!';setTimeout(()=>this.innerText='Copy',1500)">Copy</button><code${langClass}>${escaped}</code></pre>`
                );
                codeBuffer = [];
                inCodeBlock = false;
                codeLanguage = '';
            } else {
                inCodeBlock = true;
                codeLanguage = line.trim().slice(3).trim().toLowerCase();
            }
            continue;
        }

        if (inCodeBlock) {
            codeBuffer.push(line);
            continue;
        }

        // 2. Blockquotes (> ...)
        if (line.trim().startsWith('>')) {
            flushList();
            flushTable();
            inBlockquote = true;
            blockquoteBuffer.push(line.replace(/^\s*>\s?/, ''));
            continue;
        } else if (inBlockquote) {
            flushBlockquote();
        }

        // 3. Tables (| ... |)
        if (line.trim().startsWith('|') && line.trim().endsWith('|')) {
            flushList();
            flushBlockquote();
            inTable = true;
            tableBuffer.push(line);
            continue;
        } else if (inTable) {
            flushTable();
        }

        // 4. Horizontal rules (---, ***, ___)
        if (/^(\s*[-*_]\s*){3,}$/.test(line.trim())) {
            flushList();
            flushTable();
            flushBlockquote();
            html.push('<hr />');
            continue;
        }

        // 5. Headings (# ... ######)
        const headingMatch = line.match(/^(#{1,6})\s+(.*)$/);
        if (headingMatch) {
            flushList();
            flushTable();
            flushBlockquote();
            const level = headingMatch[1].length;
            const text = formatInline(headingMatch[2].trim());
            const id = slugify(headingMatch[2]);
            html.push(`<h${level} id="${id}">${text}</h${level}>`);
            continue;
        }

        // 6. Task lists (- [ ] or - [x])
        const taskMatch = line.match(/^\s*[-*]\s+\[([ xX])\]\s+(.*)$/);
        if (taskMatch) {
            if (!inUl) {
                flushList();
                html.push('<ul class="task-list">');
                inUl = true;
            }
            const checked = taskMatch[1].toLowerCase() === 'x';
            const text = formatInline(taskMatch[2].trim());
            html.push(
                `<li class="task-item"><input type="checkbox" ${checked ? 'checked' : ''} disabled /> <span>${text}</span></li>`
            );
            continue;
        }

        // 7. Unordered lists (- or *)
        const ulMatch = line.match(/^\s*[-*]\s+(.*)$/);
        if (ulMatch) {
            if (!inUl) {
                flushList();
                html.push('<ul>');
                inUl = true;
            }
            html.push(`<li>${formatInline(ulMatch[1].trim())}</li>`);
            continue;
        }

        // 8. Ordered lists (1. ...)
        const olMatch = line.match(/^\s*\d+\.\s+(.*)$/);
        if (olMatch) {
            if (!inOl) {
                flushList();
                html.push('<ol>');
                inOl = true;
            }
            html.push(`<li>${formatInline(olMatch[1].trim())}</li>`);
            continue;
        }

        // Blank lines
        if (!line.trim()) {
            flushList();
            flushTable();
            flushBlockquote();
            continue;
        }

        // Regular paragraph
        flushList();
        flushTable();
        flushBlockquote();
        html.push(`<p>${formatInline(line.trim())}</p>`);
    }

    // Flush any pending blocks
    flushList();
    flushTable();
    flushBlockquote();

    if (inCodeBlock) {
        const escaped = escapeHtml(codeBuffer.join('\n'));
        html.push(`<pre><code>${escaped}</code></pre>`);
    }

    return html.join('\n');
}

function renderTable(tableLines: string[]): string {
    if (tableLines.length < 2) return tableLines.join('<br />');

    const parseRow = (row: string) =>
        row
            .split('|')
            .slice(1, -1)
            .map((c) => c.trim());

    const headerCells = parseRow(tableLines[0]);
    const alignRow = parseRow(tableLines[1]);

    const alignments = alignRow.map((a) => {
        if (a.startsWith(':') && a.endsWith(':')) return 'center';
        if (a.endsWith(':')) return 'right';
        return 'left';
    });

    let output = '<table><thead><tr>';
    headerCells.forEach((c, idx) => {
        const align = alignments[idx] || 'left';
        output += `<th style="text-align: ${align}">${formatInline(c)}</th>`;
    });
    output += '</tr></thead><tbody>';

    for (let r = 2; r < tableLines.length; r++) {
        const cells = parseRow(tableLines[r]);
        output += '<tr>';
        cells.forEach((c, idx) => {
            const align = alignments[idx] || 'left';
            output += `<td style="text-align: ${align}">${formatInline(c)}</td>`;
        });
        output += '</tr>';
    }

    output += '</tbody></table>';
    return output;
}

function formatInline(text: string): string {
    let res = text;

    // Inline code (`code`)
    res = res.replace(/`([^`]+)`/g, (_, code) => `<code>${escapeHtml(code)}</code>`);

    // Bold + Italic (***text*** or ___text___)
    res = res.replace(/(\*\*\*|___)(.*?)\1/g, '<strong><em>$2</em></strong>');

    // Bold (**text** or __text__)
    res = res.replace(/(\*\*|__)(.*?)\1/g, '<strong>$2</strong>');

    // Italic (*text* or _text_)
    res = res.replace(/(\*|_)(.*?)\1/g, '<em>$2</em>');

    // Strikethrough (~~text~~)
    res = res.replace(/~~(.*?)~~/g, '<del>$1</del>');

    // Images (![alt](url))
    res = res.replace(/!\[([^\]]*)\]\(([^)]+)\)/g, '<img src="$2" alt="$1" />');

    // Links ([text](url))
    res = res.replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2" target="_blank" rel="noopener noreferrer">$1</a>');

    return res;
}

function escapeHtml(str: string): string {
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

function slugify(text: string): string {
    return text
        .toLowerCase()
        .replace(/[^\w\s-]/g, '')
        .replace(/\s+/g, '-');
}
