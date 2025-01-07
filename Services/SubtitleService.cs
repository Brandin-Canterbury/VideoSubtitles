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
                var buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    mp3Writer.Write(buffer, 0, bytesRead);
                }
            }

            _logAction?.Invoke($"Audio extraction to MP3 completed successfully. Saved to: {mp3Path}", false);

            // Split MP3 into chunks
            var chunkPaths = SplitAudioIntoChunks(mp3Path, 25 * 1024 * 1024); // 25MB max size

            // Translate chunks and generate SRT
            await TranslateChunksToSrt(chunkPaths, outputSrtPath);
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

    private List<string> SplitAudioIntoChunks(string mp3Path, long maxSizeInBytes)
    {
        var chunkPaths = new List<string>();
        using var reader = new MediaFoundationReader(mp3Path);

        int chunkIndex = 0;
        long currentChunkSize = 0;
        var writer = CreateChunkWriter(mp3Path, chunkIndex, reader.WaveFormat);

        var buffer = new byte[4096];
        int bytesRead;
        while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (currentChunkSize + bytesRead > maxSizeInBytes)
            {
                writer.Dispose();
                chunkPaths.Add(GetChunkPath(mp3Path, chunkIndex));
                chunkIndex++;
                currentChunkSize = 0;
                writer = CreateChunkWriter(mp3Path, chunkIndex, reader.WaveFormat);
            }

            writer.Write(buffer, 0, bytesRead);
            currentChunkSize += bytesRead;
        }

        writer.Dispose();
        chunkPaths.Add(GetChunkPath(mp3Path, chunkIndex));

        return chunkPaths;
    }

    private LameMP3FileWriter CreateChunkWriter(string mp3Path, int chunkIndex, WaveFormat waveFormat)
    {
        string chunkPath = GetChunkPath(mp3Path, chunkIndex);
        return new LameMP3FileWriter(chunkPath, waveFormat, LAMEPreset.STANDARD);
    }

    private string GetChunkPath(string mp3Path, int chunkIndex)
    {
        string directory = Path.GetDirectoryName(mp3Path);
        string fileName = Path.GetFileNameWithoutExtension(mp3Path);
        return Path.Combine(directory, $"{fileName}_chunk{chunkIndex}.mp3");
    }

    private async Task TranslateChunksToSrt(List<string> chunkPaths, string outputSrtPath)
    {
        var srtEntries = new List<string>();
        TimeSpan offset = TimeSpan.Zero;

        foreach (var chunkPath in chunkPaths)
        {
            using var audioStream = File.OpenRead(chunkPath);
            var content = new MultipartFormDataContent
            {
                { new StreamContent(audioStream), "file", Path.GetFileName(chunkPath) },
                { new StringContent("whisper-1"), "model" },
                { new StringContent("srt"), "response_format" }
            };

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/translations", content);
            response.EnsureSuccessStatusCode();

            var srtContent = await response.Content.ReadAsStringAsync();
            srtEntries.Add(AdjustTimestamps(srtContent, offset));

            // Update offset
            var duration = GetAudioDuration(chunkPath);
            offset += duration;
        }

        File.WriteAllText(outputSrtPath, string.Join(Environment.NewLine, srtEntries));
    }

    private string AdjustTimestamps(string srtContent, TimeSpan offset)
    {
        var adjustedContent = new StringBuilder();

        foreach (var line in srtContent.Split(Environment.NewLine))
        {
            if (line.Contains("-->"))
            {
                var parts = line.Split("-->");
                var start = TimeSpan.Parse(parts[0].Trim());
                var end = TimeSpan.Parse(parts[1].Trim());

                adjustedContent.AppendLine($"{(start + offset):hh\\:mm\\:ss\\.fff} --> {(end + offset):hh\\:mm\\:ss\\.fff}");
            }
            else
            {
                adjustedContent.AppendLine(line);
            }
        }

        return adjustedContent.ToString();
    }

    private TimeSpan GetAudioDuration(string mp3Path)
    {
        using var reader = new MediaFoundationReader(mp3Path);
        return reader.TotalTime;
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