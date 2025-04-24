using Microsoft.Maui.Controls;
using NAudio.Wave;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vosk;

namespace VoiceToClipboard.Views;

public partial class VoiceWindow : ContentPage
{
    private WaveInEvent? waveIn;                  // マイク入力用のイベント
    private VoskRecognizer? recognizer;           // Vosk 音声認識器
    private Model? model;                         // Vosk 音声認識モデル
    private bool isListening = false;             // 音声認識実行中フラグ
    private CancellationTokenSource? cts;         // (未使用) タスクキャンセル
    private static string resultText = string.Empty; // 結果テキスト蓄積用

    public VoiceWindow()
    {
        InitializeComponent();
        PasteButton.Clicked += OnPasteButtonClicked; // ボタンにイベント登録
    }

    // ウィンドウが表示されたときに呼ばれる
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await StartRecognitionAsync(); // 音声認識を開始
    }

    // ウィンドウが非表示になったときに呼ばれる
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopRecognition(); // 音声認識を停止
    }

    // 外部から一時停止用
    public void PauseRecognition()
    {
        StopRecognition();
    }

    // 外部から再開用
    public async void ResumeRecognition()
    {
        await StartRecognitionAsync();
    }

    // 音声認識の開始処理
    private async Task StartRecognitionAsync()
    {
        if (isListening) return;
        isListening = true;

		// 音声認識モデルのパスを設定
        string modelPath = Path.Combine(AppContext.BaseDirectory, "VoskModels", "vosk-model-small-ja-0.22");
        model = new Model(modelPath);
        recognizer = new VoskRecognizer(model, 16000.0f);

        // マイクの設定
        waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 1) // サンプリングレートとチャンネル数を設定モノラル・16kHz
        };
        waveIn.DataAvailable += OnDataAvailable; // データが利用可能になったときのイベントハンドラを追加
        waveIn.StartRecording();// 録音を開始

        //// 30分で終了
        //cts = new CancellationTokenSource();
        //try
        //{
        //    await Task.Delay(TimeSpan.FromMinutes(30), cts.Token);// 30分待機
        //}
        //catch (TaskCanceledException) { } // タスクキャンセル時は何もしない

        //StopRecognition();// 音声認識を停止

        await Task.CompletedTask; // 非同期メソッドにするためにダミーの非同期操作を追加
    }

    // 音声認識の停止処理
    private void StopRecognition()
    {
        if (!isListening) return;
        isListening = false;

        waveIn?.StopRecording(); // 録音を停止
        waveIn?.Dispose(); // リソースを解放
        waveIn = null;

if (cts != null)
        {
            Console.WriteLine("Cancelling task.");
            cts.Cancel(); // タスクをキャンセル
            cts.Dispose(); // リソースを解放
            cts = null;
        }
        else
        {
            Console.WriteLine("CancellationTokenSource is null.");
        }
    }

    // 音声データが来たときに呼ばれる
    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (recognizer == null) return;

        if (recognizer.AcceptWaveform(e.Buffer, e.Buffer.Length))
        {
            var result = recognizer.Result();
            var textTmp = JsonDocument.Parse(result).RootElement.GetProperty("text").GetString();
            if (string.IsNullOrEmpty(textTmp)) return;

            resultText += textTmp + "\n"; // 全体保持用（クリップボード用にも使う）

            MainThread.BeginInvokeOnMainThread(() =>
            {
                RecognitionResultLabel.Text += textTmp + "\n";
            });
        }
    }



    // 結果をクリップボードにコピー
    private async void OnPasteButtonClicked(object? sender, EventArgs e)
    {
        // アニメーション（押された感じ）
        await PasteButton.ScaleTo(0.96, 80, Easing.CubicInOut);  // 少し小さく
        await PasteButton.ScaleTo(1.0, 80, Easing.CubicInOut);   // 元に戻す

        // クリップボードへコピー
        await Clipboard.SetTextAsync(resultText.Replace(" ", ""));

        // 一時的にテキスト変更など（任意）
        string originalText = PasteButton.Text;
        PasteButton.Text = "コピーしました！";

        await Task.Delay(1500);
        PasteButton.Text = originalText;
    }

}