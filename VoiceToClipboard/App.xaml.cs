using VoiceToClipboard.Views;

#if WINDOWS
using System.Drawing;
using System.Windows.Forms;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Windows.Graphics;
using System.Runtime.InteropServices;
using WinRT.Interop;
#endif

namespace VoiceToClipboard
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        private Window? _mainWindow;
        private VoiceWindow? _voiceWindow;

#if WINDOWS
        private NotifyIcon? _notifyIcon;
#endif

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            _voiceWindow = new VoiceWindow();
            _mainWindow = new Window(_voiceWindow);

#if WINDOWS
            // 起動時にウインドウを表示しないようにする
            _mainWindow.Created += (s, e) =>
            {
                _mainWindow?.Dispatcher.Dispatch(() =>
                {
                    HideWindow(_mainWindow);
                });
            };

            _mainWindow.HandlerChanged += (s, e) =>
            {
                var nativeWindow = _mainWindow?.Handler?.PlatformView as Microsoft.UI.Xaml.Window; // Windows上で動いている本物のウインドウ
                if (nativeWindow != null)
                {
                    var hWnd = WindowNative.GetWindowHandle(nativeWindow); // そのウインドウのウインドウハンドルを取得
                    var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
                    var appWindow = AppWindow.GetFromWindowId(windowId);
                    if (appWindow != null)
                    {
                        appWindow.Closing += (sender, args) =>
                        {
                            args.Cancel = true; // 「×」ボタンで終了しない(終了処理をキャンセル)
                            var hWnd = WindowNative.GetWindowHandle(nativeWindow); // そのウインドウのウインドウハンドルを取得
                            ShowWindow(hWnd, SW_HIDE); // Win32 APIを使用してそのウインドウを非表示にする
                        };
                    }
                }
            };

            InitializeTrayIcon();
#endif

            return _mainWindow!;
        }

#if WINDOWS
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private void HideWindow(Window window)
        {
            var nativeWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

            if (nativeWindow is not null)
            {
                var hWnd = WindowNative.GetWindowHandle(nativeWindow);
                ShowWindow(hWnd, SW_HIDE);
            }
        }

        private void ShowWindow(Window window)
        {
            var nativeWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

            if (nativeWindow is not null)
            {
                var hWnd = WindowNative.GetWindowHandle(nativeWindow); // そのウインドウのウインドウハンドルを取得
                ShowWindow(hWnd, SW_SHOW); // 非表示にしたウインドウをWin32 APIで再表示
            }
        }

        private void InitializeTrayIcon()
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "icon.ico");

            if (!File.Exists(iconPath))
            {
                System.Diagnostics.Debug.WriteLine($"アイコンファイルが見つかりません: {iconPath}");
                return;
            }

            _notifyIcon = new NotifyIcon
            {
                Icon = new Icon(iconPath),
                Visible = true,
                Text = "タスクトレイ常駐アプリ"
            };

            var contextMenu = new ContextMenuStrip();
            var openItem = new ToolStripMenuItem("起動", null, (s, e) =>
            {
                if (_mainWindow != null)
                {
                    ShowWindow(_mainWindow);
                }
            });

            var exitItem = new ToolStripMenuItem("終了", null, (s, e) =>
            {
                _notifyIcon?.Dispose(); // タスクトレイアイコンを片づける
                Environment.Exit(0); // アプリを強制終了(※0は正常終了)
            });

            contextMenu.Items.Add(openItem);
            contextMenu.Items.Add(exitItem);
            _notifyIcon.ContextMenuStrip = contextMenu;
        }
#endif
    }
}
