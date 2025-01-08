using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NAudio.Lame;
using NAudio.Wave;
using VideoTranslator.Enums;
using VideoTranslator.Utilities;

namespace VideoTranslator.Services;

public class SubtitleService(FormUpdateService formUpdateService)
{
    private readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", FileManager.LoadApiKey()) }
    };

    private string _tempPath = string.Empty;
    private string _outputPath = string.Empty;
    private string _videoPath = string.Empty;
    private string _mp3Path = string.Empty;

    private bool _cancel;

    private List<string> _chunkPaths = [];

    private const string URL = "https://api.openai.com/v1/audio/translations";

    public void SetPaths(string videoPath, string outputSrtPath)
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "VideoTranslator");
        FileManager.CreateDirectoryIfNotExists(_tempPath);
        _videoPath = videoPath;
        _outputPath = outputSrtPath;
        _mp3Path = Path.Combine(_tempPath, Path.ChangeExtension(Path.GetFileName(_videoPath), ".mp3"));
    }

    public void CancelActions()
    {
        _cancel = true;
    }
    
    public async Task ExtractAudio()
    {
        try
        {
            _cancel = false;
            formUpdateService.UpdateSecondaryProcess(0);
            formUpdateService.LogMessage("Starting audio extraction to MP3.");

            await using (var reader = new MediaFoundationReader(_videoPath))
            await using (var mp3Writer = new LameMP3FileWriter(_mp3Path, reader.WaveFormat, LAMEPreset.STANDARD))
            {
                var buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (_cancel) break;
                    mp3Writer.Write(buffer, 0, bytesRead);
                    SetSecondaryProgress(reader.Position, reader.Length);
                }
            }

            formUpdateService.LogMessage($"Audio extraction to MP3 completed successfully. Saved to: {_mp3Path}",
                LogType.Success);


        }
        catch (Exception ex)
        {
            formUpdateService.LogMessage($"Audio extraction to MP3 completed successfully. Saved to: {_mp3Path}",
                LogType.Success);
            formUpdateService.LogMessage($"Error during audio extraction or translation: {ex.Message}", LogType.Error);
        }
    }

    public void SplitAudioIntoChunks(long maxSizeInBytes)
    {
        try
        {
            formUpdateService.UpdateSecondaryProcess(0);
            _chunkPaths = new List<string>();
            using var reader = new MediaFoundationReader(_mp3Path);

            var completedChunks = 0;
            var chunkIndex = 0;
            long currentChunkSize = 0;
            var writer = CreateChunkWriter(chunkIndex, reader.WaveFormat);

            var buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (_cancel) break;
                if (currentChunkSize + bytesRead > maxSizeInBytes)
                {
                    writer.Dispose();
                    var chunkPath = GetChunkPath(chunkIndex);
                    _chunkPaths.Add(chunkPath);
                    chunkIndex++;
                    completedChunks++;
                    
                    formUpdateService.LogMessage($"Creating Chunk {completedChunks}/{_chunkPaths.Count}: {chunkPath}");
                    SetSecondaryProgress(completedChunks, _chunkPaths.Count);
                    currentChunkSize = 0;
                    writer = CreateChunkWriter(chunkIndex, reader.WaveFormat);
                }

                writer.Write(buffer, 0, bytesRead);
                currentChunkSize += bytesRead;
            }

            writer.Dispose();
            _chunkPaths.Add(GetChunkPath(chunkIndex));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    
    public async Task TranslateChunksToSrt()
    {
        var srtEntries = new List<string>();
        var offset = TimeSpan.Zero;
        var completedChunks = 0;

        foreach (var chunkPath in _chunkPaths)
        {
            if (_cancel) break;
            await using var audioStream = File.OpenRead(chunkPath);
            formUpdateService.LogMessage($"Uploading Chunk: {completedChunks + 1}/{_chunkPaths.Count} to {URL}");
            var content = new MultipartFormDataContent
            {
                { new StreamContent(audioStream), "file", Path.GetFileName(chunkPath) },
                { new StringContent("whisper-1"), "model" },
                { new StringContent("srt"), "response_format" }
            };

            var response = await _httpClient.PostAsync(URL, content);
            response.EnsureSuccessStatusCode();

            var srtContent = await response.Content.ReadAsStringAsync();
            srtEntries.Add(srtContent);

            // Update offset
            var duration = GetAudioDuration(chunkPath);
            offset += duration;
            completedChunks++;
            formUpdateService.LogMessage($"Completed translation of chunk: {completedChunks}/{_chunkPaths.Count}");
            SetSecondaryProgress(completedChunks, _chunkPaths.Count);
        }

        await File.WriteAllTextAsync(_outputPath, string.Join(Environment.NewLine, srtEntries));
    }

    private LameMP3FileWriter CreateChunkWriter(int chunkIndex, WaveFormat waveFormat)
    {
        var chunkPath = GetChunkPath(chunkIndex);
        return new LameMP3FileWriter(chunkPath, waveFormat, LAMEPreset.STANDARD);
    }

    private string GetChunkPath(int chunkIndex)
    {
        var directory = Path.GetDirectoryName(_mp3Path);
        var fileName = Path.GetFileNameWithoutExtension(_mp3Path);
        return Path.Combine(directory ?? string.Empty, $"{fileName}_chunk{chunkIndex}.mp3");
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

                adjustedContent.AppendLine(
                    $"{(start + offset):hh\\:mm\\:ss\\.fff} --> {(end + offset):hh\\:mm\\:ss\\.fff}");
            }
            else
            {
                adjustedContent.AppendLine(line);
            }
        }

        return adjustedContent.ToString();
    }

    private TimeSpan GetAudioDuration(string filePath)
    {
        using var reader = new MediaFoundationReader(filePath);
        return reader.TotalTime;
    }

    public void Cleanup()
    {
        try
        {
            formUpdateService.LogMessage("Starting cleanup of temporary files.");
            if (!Directory.Exists(_tempPath)) return;
            Directory.Delete(_tempPath, true);
            formUpdateService.LogMessage("Temporary files cleaned up.");
            formUpdateService.UpdateMainProcess(0);
            formUpdateService.UpdateSecondaryProcess(0);
        }
        catch (Exception ex)
        {
            formUpdateService.LogMessage($"Error during cleanup: {ex.Message}", LogType.Error);
        }
    }

    private void SetSecondaryProgress(int lowNumber, int highNumber)
    {
        var percentage = lowNumber / highNumber * 100;
        formUpdateService.UpdateSecondaryProcess(percentage);
    }
    
    private void SetSecondaryProgress(long lowNumber, long highNumber)
    {
        var percentage = (int)(lowNumber / highNumber) * 100;
        formUpdateService.UpdateSecondaryProcess(percentage);
    }
}