using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace Wrish
{
    public partial class MainWindow : Window
    {
        // ─── 状态 ───
        private string? _currentFilePath;
        private bool _isDarkTheme = true;
        private bool _isDirty = false;
        private bool _wordCountVisible = true;
        private bool _isProgrammaticChange = false;
        private bool _pendingScroll = false;

        // ─── 定时器 ───
        private DispatcherTimer? _autoSaveTimer;

        // ─── 系统托盘 ───
        private Forms.NotifyIcon? _notifyIcon;

        // ─── 主题色 ───
        private readonly Brush _darkBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
        private readonly Brush _darkFg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 212, 212));
        private readonly Brush _darkDim = new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
        private readonly Brush _darkBorder = new SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 51, 51));

        private readonly Brush _lightBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
        private readonly Brush _lightFg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
        private readonly Brush _lightDim = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 220));
        private readonly Brush _lightBorder = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 220));

        // ─── 窗口拖拽/缩放 ───
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private ResizeDirection _resizeDir = ResizeDirection.None;

        private enum ResizeDirection { None, Left, Right, Top, Bottom, TopLeft, TopRight, BottomLeft, BottomRight }

        public MainWindow()
        {
            InitializeComponent();
            SetupNotifyIcon();
            SetupTimers();
            ApplyTheme();
            UpdateWordCount();
        }

        private void SetupNotifyIcon()
        {
            _notifyIcon = new Forms.NotifyIcon
            {
                Icon = new System.Drawing.Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wrish.ico")),
                Visible = true,
                Text = "Wrish"
            };
            _notifyIcon.DoubleClick += (_, _) => RestoreFromTray();
            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == Forms.MouseButtons.Right)
                {
                    var menu = new Forms.ContextMenuStrip();
                    menu.Items.Add("显示", null, (_, _) => RestoreFromTray());
                    menu.Items.Add("退出", null, (_, _) => { _notifyIcon?.Dispose(); System.Windows.Application.Current.Shutdown(); });
                    menu.Show(Forms.Cursor.Position);
                }
            };
        }

        private void SetupTimers()
        {
            _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2000) };
            _autoSaveTimer.Tick += (_, _) => { _autoSaveTimer.Stop(); PerformAutoSave(); };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Editor.Focus();
            // 布局完成后再计算 Padding，确保 ViewportHeight 有效
            Dispatcher.BeginInvoke(new Action(UpdatePaddingAndScroll), DispatcherPriority.Render);
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _notifyIcon?.Dispose();
        }

        private void ToggleTheme()
        {
            _isDarkTheme = !_isDarkTheme;
            ApplyTheme();
            RefreshAllTextFormatting();
        }

        private void ApplyTheme()
        {
            Brush bg, fg, border, caret, selection;

            if (_isDarkTheme)
            {
                bg = _darkBg; fg = _darkFg; border = _darkBorder;
                caret = _darkFg; selection = new SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 79, 120));
            }
            else
            {
                bg = _lightBg; fg = _lightFg; border = _lightBorder;
                caret = _lightFg; selection = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 210, 240));
            }

            MainBorder.Background = bg;
            MainBorder.BorderBrush = border;
            Editor.Foreground = fg;
            Editor.CaretBrush = caret;
            Editor.SelectionBrush = selection;
            WordCountText.Foreground = fg;

            RefreshAllTextFormatting();
        }

        private void Editor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_isProgrammaticChange) return;
            RefreshFocusHighlighting();
            RequestScrollToCenter();
        }

        private void RefreshFocusHighlighting()
        {
            if (_isProgrammaticChange) return;
            var doc = Editor.Document;
            var caretPos = Editor.CaretPosition;
            if (caretPos == null) return;
            var currentPara = caretPos.Paragraph;
            if (currentPara == null) return;

            var allParagraphs = new List<Paragraph>();
            foreach (var block in doc.Blocks)
            {
                if (block is Paragraph p) allParagraphs.Add(p);
            }

            Brush dimBrush = _isDarkTheme ? _darkDim : _lightDim;
            Brush normalBrush = _isDarkTheme ? _darkFg : _lightFg;

            foreach (var para in allParagraphs)
            {
                var brush = (para == currentPara) ? normalBrush : dimBrush;
                para.Foreground = brush;
                foreach (var inline in para.Inlines)
                {
                    inline.Foreground = brush;
                }
            }
        }

        private void RefreshAllTextFormatting()
        {
            _isProgrammaticChange = true;
            try
            {
                var doc = Editor.Document;
                Brush dimBrush = _isDarkTheme ? _darkDim : _lightDim;
                Brush normalBrush = _isDarkTheme ? _darkFg : _lightFg;

                Paragraph? currentPara = Editor.CaretPosition?.Paragraph;

                foreach (var block in doc.Blocks)
                {
                    if (block is not Paragraph para) continue;
                    var brush = (para == currentPara) ? normalBrush : dimBrush;
                    para.Foreground = brush;
                    foreach (var inline in para.Inlines)
                    {
                        inline.Foreground = brush;
                    }
                }
            }
            finally
            {
                _isProgrammaticChange = false;
            }
        }

        /// <summary>
        /// 核心：动态设置上下 Padding 为半个视口高度，
        /// 这样即使只有一行文本，也能滚动到屏幕正中央。
        /// </summary>
        private void UpdatePaddingAndScroll()
        {
            double half = EditorScroll.ViewportHeight / 2.0;
            if (half <= 0 || double.IsNaN(half)) return;

            TopPadding.Height = half;
            BottomPadding.Height = half;

            RequestScrollToCenter();
        }

        private void RequestScrollToCenter()
        {
            if (_pendingScroll) return;
            _pendingScroll = true;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _pendingScroll = false;
                ScrollCurrentLineToCenterImpl();
            }), DispatcherPriority.Render);
        }

        private void ScrollCurrentLineToCenterImpl()
        {
            if (Editor.CaretPosition == null) return;

            try
            {
                var rect = Editor.CaretPosition.GetCharacterRect(LogicalDirection.Forward);
                if (rect.IsEmpty) return;

                // 光标行中心在 RichTextBox 内的 Y 坐标
                // 由于 TopPadding = half-viewport，StackPanel 总高度 = half + content + half
                // 目标滚动偏移量 = 光标中心在 RichTextBox 内的 Y（TopPadding 的 half 已在布局中抵消）
                double targetOffset = rect.Y + rect.Height / 2.0;

                // 2px 防抖阈值
                double current = EditorScroll.VerticalOffset;
                if (Math.Abs(targetOffset - current) <= 2.0) return;

                // 边界限制
                double maxOffset = Math.Max(0, EditorScroll.ExtentHeight - EditorScroll.ViewportHeight);
                targetOffset = Math.Max(0, Math.Min(targetOffset, maxOffset));

                EditorScroll.ScrollToVerticalOffset(targetOffset);
            }
            catch { }
        }

        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isProgrammaticChange) return;

            _isDirty = true;
            UpdateTitle();
            UpdateWordCount();
            RequestScrollToCenter();

            _autoSaveTimer?.Stop();
            _autoSaveTimer?.Start();
        }

        private void Editor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                double newSize = Editor.FontSize + (e.Delta > 0 ? 1 : -1);
                if (newSize >= 8 && newSize <= 72)
                {
                    Editor.FontSize = newSize;
                }
                e.Handled = true;
                return;
            }
            // 让外层 ScrollViewer 自然处理滚轮，RichTextBox 不内部滚动
        }

        private void Editor_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                HideToTray();
                return;
            }

            var modifiers = Keyboard.Modifiers;
            bool ctrl = (modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool shift = (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (ctrl)
            {
                switch (e.Key)
                {
                    case Key.N: e.Handled = true; NewDocument(); return;
                    case Key.O: e.Handled = true; OpenDocument(); return;
                    case Key.S:
                        e.Handled = true;
                        if (shift)
                            SaveAsDocument();
                        else
                            SaveDocument();
                        return;
                    case Key.T: e.Handled = true; ToggleTheme(); return;
                }
            }
            else if (e.Key == Key.F4)
            {
                _wordCountVisible = !_wordCountVisible;
                WordCountText.Visibility = _wordCountVisible ? Visibility.Visible : Visibility.Collapsed;
                e.Handled = true;
                return;
            }

            if (e.Key is Key.Up or Key.Down or Key.Left or Key.Right
                or Key.Return or Key.Back or Key.Delete or Key.PageUp or Key.PageDown)
            {
                RequestScrollToCenter();
            }
        }

        private void UpdateWordCount()
        {
            try
            {
                string text = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd).Text;
                int count = 0;
                bool inWord = false;
                foreach (char c in text)
                {
                    if (c >= '\u4e00' && c <= '\u9fff')
                    {
                        count++;
                        inWord = false;
                    }
                    else if (char.IsLetterOrDigit(c))
                    {
                        if (!inWord)
                        {
                            count++;
                            inWord = true;
                        }
                    }
                    else
                    {
                        inWord = false;
                    }
                }
                WordCountText.Text = count.ToString();
            }
            catch { }
        }

        private void NewDocument()
        {
            if (_isDirty && AskSaveIfNeeded()) return;

            _isProgrammaticChange = true;
            Editor.Document.Blocks.Clear();
            Editor.Document.Blocks.Add(new Paragraph());
            _isProgrammaticChange = false;

            _currentFilePath = null;
            _isDirty = false;
            UpdateTitle();
            UpdateWordCount();
            Editor.Focus();
        }

        private void OpenDocument()
        {
            if (_isDirty && AskSaveIfNeeded()) return;

            var dlg = new OpenFileDialog
            {
                Filter = "文本文件|*.txt;*.md|所有文件|*.*",
                Title = "打开文档"
            };
            if (dlg.ShowDialog() == true)
            {
                LoadFile(dlg.FileName);
            }
        }

        private void LoadFile(string path)
        {
            try
            {
                string content = File.ReadAllText(path, Encoding.UTF8);
                _isProgrammaticChange = true;
                Editor.Document.Blocks.Clear();
                var para = new Paragraph();
                para.Inlines.Add(new Run(content));
                Editor.Document.Blocks.Add(para);
                _isProgrammaticChange = false;

                _currentFilePath = path;
                _isDirty = false;
                UpdateTitle();
                UpdateWordCount();
                RefreshAllTextFormatting();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"打开文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveDocument()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveAsDocument();
            }
            else
            {
                DoSave(_currentFilePath);
            }
        }

        private void SaveAsDocument()
        {
            var dlg = new SaveFileDialog
            {
                Filter = "文本文件|*.txt|Markdown|*.md|所有文件|*.*",
                Title = "另存为",
                FileName = "未命名.txt"
            };
            if (dlg.ShowDialog() == true)
            {
                DoSave(dlg.FileName);
                _currentFilePath = dlg.FileName;
                UpdateTitle();
            }
        }

        private void DoSave(string path)
        {
            try
            {
                string text = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd).Text;
                File.WriteAllText(path, text, Encoding.UTF8);
                _isDirty = false;
                UpdateTitle();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool AskSaveIfNeeded()
        {
            var result = System.Windows.MessageBox.Show("当前文档有未保存的更改，是否保存？", "Wrish",
                MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                SaveDocument();
                return false;
            }
            return result == MessageBoxResult.Cancel;
        }

        private void UpdateTitle()
        {
            string name = string.IsNullOrEmpty(_currentFilePath) ? "未命名" : Path.GetFileName(_currentFilePath);
            string dirtyMark = _isDirty ? " *" : "";
            Title = $"Wrish - {name}{dirtyMark}";
        }

        private void PerformAutoSave()
        {
            if (!_isDirty) return;

            string text = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd).Text;

            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                try { File.WriteAllText(_currentFilePath, text, Encoding.UTF8); _isDirty = false; UpdateTitle(); }
                catch { }
            }
            else
            {
                string tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".temp_draft.txt");
                try { File.WriteAllText(tempPath, text, Encoding.UTF8); }
                catch { }
            }
        }

        private void HideToTray()
        {
            Hide();
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            Editor.Focus();
        }

        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is not RichTextBox)
            {
                Point pos = e.GetPosition(this);
                _resizeDir = GetResizeDirection(pos);
                if (_resizeDir != ResizeDirection.None)
                {
                    _isDragging = true;
                    _dragStartPoint = pos;
                    CaptureMouse();
                    e.Handled = true;
                }
                else
                {
                    if (pos.Y < 40)
                    {
                        DragMove();
                    }
                }
            }
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging && _resizeDir != ResizeDirection.None)
            {
                Point pos = e.GetPosition(this);
                ResizeWindow(pos);
            }
            else
            {
                Point pos = e.GetPosition(this);
                var dir = GetResizeDirection(pos);
                Cursor = dir switch
                {
                    ResizeDirection.Left or ResizeDirection.Right => Cursors.SizeWE,
                    ResizeDirection.Top or ResizeDirection.Bottom => Cursors.SizeNS,
                    ResizeDirection.TopLeft or ResizeDirection.BottomRight => Cursors.SizeNWSE,
                    ResizeDirection.TopRight or ResizeDirection.BottomLeft => Cursors.SizeNESW,
                    _ => Cursors.Arrow
                };
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_isDragging)
            {
                _isDragging = false;
                _resizeDir = ResizeDirection.None;
                ReleaseMouseCapture();
            }
        }

        private const int RESIZE_BORDER = 6;

        private ResizeDirection GetResizeDirection(Point pos)
        {
            bool left = pos.X <= RESIZE_BORDER;
            bool right = pos.X >= ActualWidth - RESIZE_BORDER;
            bool top = pos.Y <= RESIZE_BORDER;
            bool bottom = pos.Y >= ActualHeight - RESIZE_BORDER;

            if (top && left) return ResizeDirection.TopLeft;
            if (top && right) return ResizeDirection.TopRight;
            if (bottom && left) return ResizeDirection.BottomLeft;
            if (bottom && right) return ResizeDirection.BottomRight;
            if (left) return ResizeDirection.Left;
            if (right) return ResizeDirection.Right;
            if (top) return ResizeDirection.Top;
            if (bottom) return ResizeDirection.Bottom;
            return ResizeDirection.None;
        }

        private void ResizeWindow(Point currentPos)
        {
            double dx = currentPos.X - _dragStartPoint.X;
            double dy = currentPos.Y - _dragStartPoint.Y;

            switch (_resizeDir)
            {
                case ResizeDirection.Right:
                    Width = Math.Max(MinWidth, Width + dx);
                    _dragStartPoint = currentPos;
                    break;
                case ResizeDirection.Bottom:
                    Height = Math.Max(MinHeight, Height + dy);
                    _dragStartPoint = currentPos;
                    break;
                case ResizeDirection.BottomRight:
                    Width = Math.Max(MinWidth, Width + dx);
                    Height = Math.Max(MinHeight, Height + dy);
                    _dragStartPoint = currentPos;
                    break;
                case ResizeDirection.Left:
                    double newWidthL = Math.Max(MinWidth, Width - dx);
                    double newLeftL = Left + (Width - newWidthL);
                    if (newWidthL > MinWidth) { Left = newLeftL; Width = newWidthL; _dragStartPoint = currentPos; }
                    break;
                case ResizeDirection.Top:
                    double newHeightT = Math.Max(MinHeight, Height - dy);
                    double newTopT = Top + (Height - newHeightT);
                    if (newHeightT > MinHeight) { Top = newTopT; Height = newHeightT; _dragStartPoint = currentPos; }
                    break;
                case ResizeDirection.TopLeft:
                    double newWTL = Math.Max(MinWidth, Width - dx);
                    double newHTL = Math.Max(MinHeight, Height - dy);
                    double newLTL = Left + (Width - newWTL);
                    double newTTL = Top + (Height - newHTL);
                    if (newWTL > MinWidth) Left = newLTL;
                    if (newHTL > MinHeight) Top = newTTL;
                    Width = newWTL; Height = newHTL;
                    _dragStartPoint = currentPos;
                    break;
                case ResizeDirection.TopRight:
                    double newHTR = Math.Max(MinHeight, Height - dy);
                    double newTTR = Top + (Height - newHTR);
                    if (newHTR > MinHeight) Top = newTTR;
                    Height = newHTR;
                    Width = Math.Max(MinWidth, Width + dx);
                    _dragStartPoint = currentPos;
                    break;
                case ResizeDirection.BottomLeft:
                    double newWBL = Math.Max(MinWidth, Width - dx);
                    double newLBL = Left + (Width - newWBL);
                    if (newWBL > MinWidth) Left = newLBL;
                    Width = newWBL;
                    Height = Math.Max(MinHeight, Height + dy);
                    _dragStartPoint = currentPos;
                    break;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePaddingAndScroll();
        }
    }
}