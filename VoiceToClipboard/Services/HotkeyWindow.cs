#if WINDOWS
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace VoiceToClipboard.Services
{
    // グローバルホットキーを処理するウィンドウクラス
    public partial class HotkeyWindow : NativeWindow, IDisposable
    {
        private const int WM_HOTKEY = 0x0312;    // ホットキーが押されたときに送られる Windows メッセージ
        private const int HOTKEY_ID = 9000;      // ホットキー識別子（一意であればOK）

        // Windows API（user32.dll）からホットキー登録/解除用の関数をインポート
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private Thread messageLoopThread;        // Windows メッセージループ用スレッド
        private readonly Action onHotkeyPressed; // ホットキーが押されたときに呼ばれる処理

        // コンストラクタ（ホットキー押下時のコールバックを受け取る）
        public HotkeyWindow(Action onHotkeyPressed)
        {
            this.onHotkeyPressed = onHotkeyPressed;
            // メッセージループを別スレッドで実行（UIスレッドと独立させる）
            messageLoopThread = new Thread(RunMessageLoop)
            {
                IsBackground = true // アプリ終了時に自動で停止
            };
            messageLoopThread.Start();
        }

        // ホットキーを登録し、メッセージループを開始する
        private void RunMessageLoop()
        {
            // ウィンドウハンドルを作成（見えないウィンドウ）
            CreateHandle(new CreateParams());
            // Ctrl + Shift + V のホットキーを登録
            RegisterHotKey(this.Handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (uint)Keys.V);
            // Windows メッセージループを開始
            System.Windows.Forms.Application.Run();
        }

        // Windows メッセージを処理（WndProc = メッセージディスパッチャ）
        protected override void WndProc(ref Message m)
        {
            // 登録したホットキーが押されたかを確認
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                onHotkeyPressed.Invoke(); // コールバックを実行
            }
            // 他のメッセージはデフォルトの処理に渡す
            base.WndProc(ref m);
        }

        // リソース解放用（明示的に呼ぶ or using文でも可）
        public void Dispose()
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID); // ホットキー登録解除
            this.DestroyHandle();                     // ウィンドウハンドル破棄
            GC.SuppressFinalize(this);                // ファイナライザの呼び出し不要に
        }

        // キー修飾子（Windows API 用）
        private const uint MOD_CONTROL = 0x0002; // Ctrl
        private const uint MOD_SHIFT = 0x0004;   // Shift
    }
}
#endif