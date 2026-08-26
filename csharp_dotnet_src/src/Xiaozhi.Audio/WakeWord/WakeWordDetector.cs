using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SherpaOnnx;
using Xiaozhi.Core.Constants;

namespace Xiaozhi.Audio.WakeWord;

public class WakeWordDetector : IDisposable
{
    private KeywordSpotter? _spotter;
    private OnlineStream? _stream;
    private bool _isRunning;
    private readonly string _modelDir;

    public event Action<string>? OnKeywordDetected;
    public bool IsEnabled { get; set; } = true;

    public WakeWordDetector(string? modelDir = null)
    {
        _modelDir = modelDir ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "models");
        InitializeModel();
    }

    private void InitializeModel()
    {
        try
        {
            var tokensPath = Path.Combine(_modelDir, "tokens.txt");
            var encoderPath = Path.Combine(_modelDir, "encoder.onnx");
            var decoderPath = Path.Combine(_modelDir, "decoder.onnx");
            var joinerPath = Path.Combine(_modelDir, "joiner.onnx");
            var keywordsPath = Path.Combine(_modelDir, "keywords.txt");

            if (File.Exists(tokensPath) && File.Exists(encoderPath) && File.Exists(decoderPath) && File.Exists(joinerPath))
            {
                var config = new KeywordSpotterConfig();
                config.FeatConfig.SampleRate = SystemConstants.SampleRate;
                config.FeatConfig.FeatureDim = 80;
                config.ModelConfig.Transducer.Encoder = encoderPath;
                config.ModelConfig.Transducer.Decoder = decoderPath;
                config.ModelConfig.Transducer.Joiner = joinerPath;
                config.ModelConfig.Tokens = tokensPath;
                config.KeywordsFile = keywordsPath;
                config.KeywordsScore = 2.0f;
                config.KeywordsThreshold = 0.25f;

                _spotter = new KeywordSpotter(config);
                _stream = _spotter.CreateStream();
                _isRunning = true;
            }
        }
        catch
        {
            // Fallback if model files are not present in models folder
            _isRunning = false;
        }
    }

    public void ProcessAudio(byte[] pcmData)
    {
        if (!IsEnabled || !_isRunning || _spotter == null || _stream == null) return;

        var floatSamples = new float[pcmData.Length / 2];
        for (int i = 0; i < floatSamples.Length; i++)
        {
            short sample = BitConverter.ToInt16(pcmData, i * 2);
            floatSamples[i] = sample / 32768.0f;
        }

        _stream.AcceptWaveform(SystemConstants.SampleRate, floatSamples);
        while (_spotter.IsReady(_stream))
        {
            _spotter.Decode(_stream);
            var result = _spotter.GetResult(_stream);
            if (!string.IsNullOrEmpty(result.Keyword))
            {
                OnKeywordDetected?.Invoke(result.Keyword);
                _spotter.Reset(_stream);
            }
        }
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _spotter?.Dispose();
    }
}
