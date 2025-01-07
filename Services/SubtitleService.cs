using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NAudio.Lame;
using NAudio.Wave;

namespace VideoTranslator.Services;

public class SubtitleService
{
    private readonly HttpClient _httpClient;
    private readonly Action<string, bool> _logAction;

    public SubtitleService(string apiKey, Action<string, bool> logAction)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API Key must be provided.");
        }

        _httpClient = new HttpClient
        {
            DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", apiKey) }
        };

        _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));
    }

    public async Task ExtractAndTranslateAudio(string videoPath, string outputSrtPath, Action<double> progressCallback)
    {
        string mp3Path = Path.ChangeExtension(outputSrtPath, ".mp3");

        try
        {
            _logAction?.Invoke("Starting audio extraction to MP3.", false);

            using (var reader = new MediaFoundationReader(videoPath))
            using (var mp3Writer = new LameMP3FileWriter(mp3Path, reader.WaveFormat, LAMEPreset.STANDARD))
            {
                long totalBytes = reader.Length;
                long bytesProcessed = 0;
                var buffer = new byte[4096];
                int lastLoggedProgress = 0; // Track last logged whole number progress

                int bytesRead;
                while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    mp3Writer.Write(buffer, 0, bytesRead);
                    bytesProcessed += bytesRead;

                    // Prevent infinite loop due to reader length issue
                    if (bytesProcessed >= totalBytes)
                    {
                        break;
                    }

                    // Calculate and report progress
                    double progress = Math.Min((double)bytesProcessed / totalBytes * 100, 100);
                    int currentProgress = (int)progress;

                    // Log only when progress reaches a new whole number
                    if (currentProgress > lastLoggedProgress)
                    {
                        lastLoggedProgress = currentProgress;
                        progressCallback?.Invoke(progress);
                        _logAction?.Invoke($"Audio Extraction Progress: {currentProgress}%", false);
                    }
                }
            }

            // Explicitly wait before accessing the file
            _logAction?.Invoke($"Audio extraction to MP3 completed successfully. Saved to: {mp3Path}", false);

            // Translate MP3 file to SRT
            await TranslateAudioToSrt(mp3Path, outputSrtPath);
        }
        catch (Exception ex)
        {
            _logAction?.Invoke($"Error during audio extraction or translation: {ex.Message}", true);
            throw;
        }
        finally
        {
            Cleanup(mp3Path);
        }
    }

    private async Task TranslateAudioToSrt(string mp3Path, string outputSrtPath)
    {
        try
        {
            _logAction?.Invoke("Starting translation to SRT using OpenAI API.", false);

            using var audioStream = File.OpenRead(mp3Path);
            var content = new MultipartFormDataContent
            {
                { new StreamContent(audioStream), "file", Path.GetFileName(mp3Path) },
                { new StringContent("whisper-1"), "model" },
                { new StringContent("srt"), "response_format" }
            };

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/translations", content);
            response.EnsureSuccessStatusCode();

            await using var fileStream = new FileStream(outputSrtPath, FileMode.Create, FileAccess.Write);
            await response.Content.CopyToAsync(fileStream);

            _logAction?.Invoke($"SRT file generated successfully at {outputSrtPath}", false);
        }
        catch (Exception ex)
        {
            _logAction?.Invoke($"Error during translation to SRT: {ex.Message}", true);
            throw;
        }
    }

    public void Cleanup(string mp3Path = null)
    {
        try
        {
            _logAction?.Invoke("Starting cleanup of temporary files.", false);

            if (!string.IsNullOrEmpty(mp3Path) && File.Exists(mp3Path))
            {
                File.Delete(mp3Path);
                _logAction?.Invoke($"Deleted temporary MP3 file: {mp3Path}", false);
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "chunks");
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
                _logAction("Temporary files cleaned up.", false);
            }
        }
        catch (Exception ex)
        {
            _logAction?.Invoke($"Error during cleanup: {ex.Message}", true);
        }
    }
}