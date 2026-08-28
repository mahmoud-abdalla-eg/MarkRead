// MarkRead Reader JavaScript

document.addEventListener('DOMContentLoaded', () => {
    initCopyButtons();
    initLinkHandler();
    initDropzoneEvents();
});

function initCopyButtons() {
    const preBlocks = document.querySelectorAll('pre');
    preBlocks.forEach((pre) => {
        if (pre.classList.contains('raw-markdown')) return;
        if (pre.querySelector('.copy-btn')) return;

        const btn = document.createElement('button');
        btn.className = 'copy-btn';
        btn.type = 'button';
        btn.textContent = 'Copy';
        btn.setAttribute('aria-label', 'Copy code to clipboard');

        btn.addEventListener('click', async (e) => {
            e.stopPropagation();
            const code = pre.querySelector('code') || pre;
            const text = code.innerText;

            try {
                await navigator.clipboard.writeText(text);
                btn.textContent = '✓ Copied!';
                btn.classList.add('copied');
                setTimeout(() => {
                    btn.textContent = 'Copy';
                    btn.classList.remove('copied');
                }, 2000);
            } catch (err) {
                console.error('Failed to copy: ', err);
            }
        });

        pre.appendChild(btn);
    });
}

function initLinkHandler() {
    document.addEventListener('click', (e) => {
        const anchor = e.target.closest('a');
        if (!anchor) return;

        const href = anchor.getAttribute('href');
        if (!href) return;

        // Hash anchors: smooth scroll
        if (href.startsWith('#')) {
            const target = document.getElementById(href.substring(1));
            if (target) {
                e.preventDefault();
                target.scrollIntoView({ behavior: 'smooth' });
            }
            return;
        }

        // Web URLs
        if (href.startsWith('http://') || href.startsWith('https://') || href.startsWith('mailto:')) {
            e.preventDefault();
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage({ type: 'openExternal', url: href });
            }
            return;
        }

        // Relative local files (like docs/setup.md)
        if (href.endsWith('.md') || href.endsWith('.markdown') || href.endsWith('.txt')) {
            e.preventDefault();
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage({ type: 'openLocalFile', path: href });
            }
        }
    });
}

let dragCounter = 0;
function initDropzoneEvents() {
    const overlay = document.getElementById('dropzone-overlay');
    if (!overlay) return;

    window.addEventListener('dragenter', (e) => {
        e.preventDefault();
        dragCounter++;
        overlay.classList.add('active');
    });

    window.addEventListener('dragover', (e) => {
        e.preventDefault();
    });

    window.addEventListener('dragleave', (e) => {
        e.preventDefault();
        dragCounter--;
        if (dragCounter <= 0) {
            dragCounter = 0;
            overlay.classList.remove('active');
        }
    });

    window.addEventListener('drop', (e) => {
        e.preventDefault();
        dragCounter = 0;
        overlay.classList.remove('active');
    });
}

// C# Interop API
window.markRead = {
    setTheme: (theme) => {
        const root = document.documentElement;
        if (theme === 'dark') {
            root.setAttribute('data-theme', 'dark');
        } else if (theme === 'light') {
            root.removeAttribute('data-theme');
        } else {
            // System auto
            const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
            if (prefersDark) {
                root.setAttribute('data-theme', 'dark');
            } else {
                root.removeAttribute('data-theme');
            }
        }
    },

    getScrollRatio: () => {
        const h = document.documentElement.scrollHeight - window.innerHeight;
        return h > 0 ? window.scrollY / h : 0;
    },

    setScrollRatio: (ratio) => {
        const h = document.documentElement.scrollHeight - window.innerHeight;
        if (h > 0) {
            window.scrollTo({ top: ratio * h, behavior: 'instant' });
        }
    },

    setFontSize: (size) => {
        const px = typeof size === 'number' ? size + 'px' : size;
        document.documentElement.style.setProperty('--base-font-size', px);
    },

    setMaxWidth: (width) => {
        document.documentElement.style.setProperty('--reading-width', width);
    },

    printDocument: () => {
        window.print();
    }
};

