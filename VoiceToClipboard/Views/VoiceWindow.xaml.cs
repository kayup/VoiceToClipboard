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
    private WaveInEvent? waveIn;
    private VoskRecognizer? recognizer;
    private Model? model;
    private bool isListening = false;
    private CancellationTokenSource? cts;
    private static string resultText = string.Empty;

    public VoiceWindow()
	{
		InitializeComponent();
        PasteButton.Clicked += OnPasteButtonClicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await StartRecognitionAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopRecognition();
    }

    private async Task StartRecognitionAsync()
    {
        if (isListening) return;
        isListening = true;


        string modelPath = Path.Combine(AppContext.BaseDirectory, "vosk-model-small-ja-0.22");
        model = new Model(modelPath);
        recognizer = new VoskRecognizer(model, 16000.0f);

        // マイクの設定
        waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 1)
        };
        waveIn.DataAvailable += OnDataAvailable;
        waveIn.StartRecording();

        // 30分で終了
        cts = new CancellationTokenSource();
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(30), cts.Token);
        }
        catch (TaskCanceledException) { } // タスクキャンセル時は何もしない

        StopRecognition();
    }



    private void StopRecognition()
    {
        if (!isListening) return;
        isListening = false;

        waveIn?.StopRecording();
        waveIn?.Dispose();
        waveIn = null;

        cts?.Cancel();
        cts?.Dispose();
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