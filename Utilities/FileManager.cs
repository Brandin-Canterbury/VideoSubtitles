namespace VideoTranslator.Utilities;


public static class FileManager
{
    private const string ApiKeyFile = "api_key.txt";
    private const string LastDirectoryFile = "last_directory.txt";

    public static void SaveApiKey(string apiKey)
    {
        File.WriteAllText(ApiKeyFile, apiKey);
    }

    public static string LoadApiKey()
    {
        return File.Exists(ApiKeyFile) ? File.ReadAllText(ApiKeyFile) : string.Empty;
    }

    public static void SaveLastDirectory(string path)
    {
        File.WriteAllText(LastDirectoryFile, path);
    }

    public static string GetLastDirectory()
    {
        return File.Exists(LastDirectoryFile) ? File.ReadAllText(LastDirectoryFile) : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
    
    public static void CreateDirectoryIfNotExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public static string GenerateAudioFilePath()
    {
        return Path.Combine(Path.GetTempPath(), "extracted_audio.wav");
    }

    public static string GenerateSubtitleFilePath(string videoPath)
    {
        var directory = Path.GetDirectoryName(videoPath);
        var fileName = Path.GetFileNameWithoutExtension(videoPath);
        return Path.Combine(directory, $"{fileName}_subtitles.srt");
    }
}