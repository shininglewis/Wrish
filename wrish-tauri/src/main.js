// Wrish Tauri Frontend
// ─── 状态 ───
let currentFileName = "未命名";
let isDirty = false;
let autoSaveTimer = null;
let isDark = true;
let wordCountVisible = true;
let lastFontSize = 18;

// ─── 获取光标所在行 ───
function getActiveLine() {
    const sel = window.getSelection();
    if (!sel.rangeCount) return null;

    const node = sel.anchorNode;
    if (!node) return null;

    const element = node.nodeType === Node.TEXT_NODE ? node.parentElement : node;
    return element.closest('.line');
}

// ─── 打字机居中 ───
function scrollToCenter() {
    const activeLine = getActiveLine();
    if (!activeLine) return;

    const editor = document.getElementById('editor');
    // 获取行相对于 editor 内容顶部的位置
    const lineTop = activeLine.offsetTop;
    const lineHeight = activeLine.offsetHeight;
    const lineCenter = lineTop + lineHeight / 2;
    const viewCenter = editor.clientHeight / 2;

    // 目标滚动位置 = 行中心 - 视口中心
    const targetScroll = lineCenter - viewCenter;

    // 平滑滚动
    editor.scrollTo({
        top: targetScroll,
        behavior: 'smooth'
    });
}

// ─── 焦点暗淡 ───
function refreshFocus() {
    const lines = document.querySelectorAll('.line');
    const activeLine = getActiveLine();

    lines.forEach(line => {
        line.classList.toggle('active', line === activeLine);
    });
}

// ─── 字数统计 ───
function updateWordCount() {
    const text = getText();
    let count = 0;
    let inWord = false;
    for (const c of text) {
        if (c >= '\u4e00' && c <= '\u9fff') {
            count++;
            inWord = false;
        } else if (/[a-zA-Z0-9]/.test(c)) {
            if (!inWord) { count++; inWord = true; }
        } else {
            inWord = false;
        }
    }
    document.getElementById('wordcount').textContent = count;
}

// ─── 获取纯文本 ───
function getText() {
    return Array.from(document.querySelectorAll('.line'))
        .map(line => line.textContent)
        .join('\n');
}

// ─── 设置文本 ───
function setText(text) {
    const editor = document.getElementById('editor');
    editor.innerHTML = '';
    const lines = text.split('\n');
    lines.forEach((line, i) => {
        const div = document.createElement('div');
        div.className = 'line';
        div.textContent = line;
        if (i === 0) div.classList.add('active');
        editor.appendChild(div);
    });
    if (lines.length === 0) {
        const div = document.createElement('div');
        div.className = 'line active';
        div.innerHTML = '<br>';
        editor.appendChild(div);
    }
}

// ─── 确保每行都是 .line div ───
function normalizeLines() {
    const editor = document.getElementById('editor');
    const children = Array.from(editor.childNodes);
    children.forEach(child => {
        if (child.nodeType === Node.TEXT_NODE) {
            const div = document.createElement('div');
            div.className = 'line';
            div.textContent = child.textContent;
            editor.replaceChild(div, child);
        } else if (child.tagName !== 'DIV' || !child.classList.contains('line')) {
            const div = document.createElement('div');
            div.className = 'line';
            div.appendChild(child.cloneNode(true));
            editor.replaceChild(div, child);
        }
    });
}

// ─── 主题切换 ───
function toggleTheme() {
    isDark = !isDark;
    document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');
    if (window.__TAURI__) {
        window.__TAURI__.core.invoke('set_theme', { dark: isDark });
    }
}

// ─── 防抖自动保存 ───
function triggerAutoSave() {
    if (autoSaveTimer) clearTimeout(autoSaveTimer);
    autoSaveTimer = setTimeout(() => {
        const text = getText();
        if (window.__TAURI__) {
            window.__TAURI__.core.invoke('auto_save', { content: text });
        }
    }, 2000);
}

// ─── 更新标题 ───
function updateTitle() {
    const mark = isDirty ? ' *' : '';
    document.title = `Wrish - ${currentFileName}${mark}`;
}

// ═══════════════════════════════════════════════════════
//  事件监听
// ═══════════════════════════════════════════════════════

const editor = document.getElementById('editor');

// 输入事件
editor.addEventListener('input', () => {
    normalizeLines();
    refreshFocus();
    scrollToCenter();
    updateWordCount();
    isDirty = true;
    updateTitle();
    triggerAutoSave();
});

// 选择变化
document.addEventListener('selectionchange', () => {
    refreshFocus();
    scrollToCenter();
});

// 键盘快捷键（window 捕获阶段，确保 Escape 不被 contenteditable 拦截）
window.addEventListener('keydown', async (e) => {
    // Esc 隐藏窗口（通过 Rust command）
    if (e.key === 'Escape') {
        e.preventDefault();
        e.stopPropagation();
        if (window.__TAURI__ && window.__TAURI__.core) {
            try {
                await window.__TAURI__.core.invoke('hide_window');
            } catch (err) {
                console.error('Hide error:', err);
            }
        }
        return;
    }

    // F4 切换字数统计
    if (e.key === 'F4') {
        e.preventDefault();
        wordCountVisible = !wordCountVisible;
        document.getElementById('wordcount').classList.toggle('hidden', !wordCountVisible);
        return;
    }

    const ctrl = e.ctrlKey || e.metaKey;

    if (ctrl) {
        switch (e.key.toLowerCase()) {
            case 'n': // 新建
                e.preventDefault();
                if (!isDirty || confirm('当前文档有未保存的更改，确定新建？')) {
                    setText('');
                    currentFileName = '未命名';
                    isDirty = false;
                    updateTitle();
                    updateWordCount();
                    // 通知 Rust 后端清除当前文件路径
                    if (window.__TAURI__ && window.__TAURI__.core) {
                        window.__TAURI__.core.invoke('new_document');
                    }
                }
                break;

            case 'o': // 打开
                e.preventDefault();
                if (window.__TAURI__) {
                    try {
                        const [content, name] = await window.__TAURI__.core.invoke('read_file');
                        setText(content);
                        currentFileName = name;
                        isDirty = false;
                        updateTitle();
                        updateWordCount();
                    } catch (err) {
                        if (err !== 'Cancelled') console.error(err);
                    }
                }
                break;

            case 's': // 保存 / 另存为
                e.preventDefault();
                if (e.shiftKey) {
                    // 另存为
                    if (window.__TAURI__) {
                        try {
                            const name = await window.__TAURI__.core.invoke('save_as', { content: getText() });
                            currentFileName = name;
                            isDirty = false;
                            updateTitle();
                        } catch (err) {
                            if (err !== 'Cancelled') console.error(err);
                        }
                    }
                } else {
                    // 保存
                    if (window.__TAURI__) {
                        try {
                            const name = await window.__TAURI__.core.invoke('save_file', { content: getText() });
                            currentFileName = name;
                            isDirty = false;
                            updateTitle();
                        } catch (err) {
                            if (err !== 'Cancelled') console.error(err);
                        }
                    }
                }
                break;

            case 't': // 主题
                e.preventDefault();
                toggleTheme();
                break;
        }
    }
});

// Ctrl + 滚轮 字体大小（window 级别，防止 contenteditable 拦截）
window.addEventListener('wheel', (e) => {
    if (e.ctrlKey) {
        e.preventDefault();
        e.stopImmediatePropagation();
        const step = e.deltaY > 0 ? -2 : 2;
        lastFontSize = Math.max(10, Math.min(72, lastFontSize + step));
        document.documentElement.style.setProperty('--font-size', lastFontSize + 'px');
        // 同步更新行高最小高度
        document.querySelectorAll('.line').forEach(line => {
            line.style.minHeight = `calc(${lastFontSize}px * var(--line-height))`;
        });
    }
}, { passive: false, capture: true });

// 初始化
document.addEventListener('DOMContentLoaded', () => {
    editor.focus();
    updateWordCount();
});