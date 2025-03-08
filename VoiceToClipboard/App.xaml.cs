using VoiceToClipboard.Views;

#if WINDOWS
using System.Drawing;
using System.Windows.Forms;
#endif

namespace VoiceToClipboard
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        public App()
        {
            InitializeComponent();
        }

#if WINDOWS
        private NotifyIcon? _notifyIcon;
#endif

        protected override Window CreateWindow(IActivationState? activationState)
        {
            try
            {
                var window = new Window(new VoiceWindow());

#if WINDOWS
                InitializeTrayIcon();
#endif

                return window;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unhandled exception: {ex}");
                throw;
            }
        }

#if WINDOWS
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
                Icon = new Icon(iconPath), // アイコンファイルをプロジェクトに追加
                Visible = true,
                Text = "タスクトレイ常駐アプリ"
            };

            var contextMenu = new ContextMenuStrip();
            var exitItem = new ToolStripMenuItem("終了", null, (s, e) => ExitApplication());
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void ExitApplication()
        {
            _notifyIcon?.Dispose();
            Environment.Exit(0);
        }
#endif
    }
}
