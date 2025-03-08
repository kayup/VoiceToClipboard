using Microsoft.Maui.Controls;
using NAudio.Wave;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Vosk;

namespace VoiceToClipboard.Views;

public partial class VoiceWindow : ContentPage
{
    private WaveInEvent? waveIn;// マイク入力用のイベント
    private VoskRecognizer? recognizer;// 音声認識器
    private Model? model;// 音声認識モデル
    private bool isListening = false;// 音声認識が実行中かどうかのフラグ
    private CancellationTokenSource? cts;// タスクキャンセル用のトークンソース
    private static string resultText = string.Empty;// 認識結果のテキスト

    public VoiceWindow()
	{
		InitializeComponent();
        // ボタンのクリックイベントにハンドラを追加
        PasteButton.Clicked += OnPasteButtonClicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await StartRecognitionAsync();// 音声認識を開始
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopRecognition();// 音声認識を停止
    }

    private async Task StartRecognitionAsync()
    {
        if (isListening) return;// 既に実行中の場合は何もしない
        isListening = true;

        // 音声認識モデルのパスを設定
        string modelPath = Path.Combine(AppContext.BaseDirectory, "vosk-model-small-ja-0.22");
        model = new Model(modelPath);
        recognizer = new VoskRecognizer(model, 16000.0f);

        // マイクの設定
        waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 1)// サンプリングレートとチャンネル数を設定
        };
        waveIn.DataAvailable += OnDataAvailable;// データが利用可能になったときのイベントハンドラを追加
        waveIn.StartRecording();// 録音を開始

        //// 30分で終了
        //cts = new CancellationTokenSource();
        //try
        //{
        //    await Task.Delay(TimeSpan.FromMinutes(30), cts.Token);// 30分待機
        //}
        //catch (TaskCanceledException) { } // タスクキャンセル時は何もしない

        //StopRecognition();// 音声認識を停止

        // 非同期メソッドにするためにダミーの非同期操作を追加
        await Task.CompletedTask;
    }



    private void StopRecognition()
    {
        if (!isListening) return; // 実行中でない場合は何もしない
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

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (recognizer == null) return;
        if (recognizer.AcceptWaveform(e.Buffer, e.Buffer.Length))
        {
            var result = recognizer.Result();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RecognitionResultLabel.Text = result;
                var textTmp = JsonDocument.Parse(result).RootElement.GetProperty("text").GetString();
                if (String.IsNullOrEmpty(textTmp)) return;
                resultText += textTmp + "\n";
            });
        }
    }

    private void OnPasteButtonClicked(object? sender, EventArgs e)
    {
        Clipboard.SetTextAsync(resultText.Replace(" ", ""));
    }
}