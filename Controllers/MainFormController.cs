using VideoTranslator.Enums;
using VideoTranslator.Services;
using VideoTranslator.Utilities;

namespace VideoTranslator.Controllers;

public class MainFormController
{
    private readonly MainForm _view;
    private readonly FormUpdateService _formUpdateService;
    private readonly SubtitleService _subtitleService;

    public MainFormController(MainForm view)
    {
        _view = view;
        _formUpdateService = new FormUpdateService(_view);
        _subtitleService = new SubtitleService(_formUpdateService);
    }
    
    public void GetApiKey()
    {
        var apiKey = FileManager.LoadApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            _formUpdateService.LogMessage("API Key Must be filled before processing", LogType.Error);
            return;
        }
        _formUpdateService.UpdateApiKey(apiKey);
    }

    public void SaveApiKey(string apiKey)
    {
        FileManager.SaveApiKey(apiKey);
        _formUpdateService.LogMessage("API Key saved.", LogType.Success);
    }
    
    public void SelectFile()
    {
        using var openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Video Files|*.mp4;*.mkv;*.avi";
        openFileDialog.InitialDirectory = FileManager.GetLastDirectory();

        if (openFileDialog.ShowDialog() != DialogResult.OK) return;
        var videoPath = openFileDialog.FileName;
        FileManager.SaveLastDirectory(Path.GetDirectoryName(videoPath) ?? string.Empty);
        _formUpdateService.UpdateFilePath(videoPath);
        _formUpdateService.LogMessage($"File selected: {videoPath}");
    }

    public async void CancelAllActions()
    {
        _subtitleService.CancelActions();
    }

    public void ProcessSelectedFile()
    {
        Task.Run(ProcessFile);
    }
    
    private async void ProcessFile()
    {
        var videoPath = _view.FilePathTextBox.Text;
        var outputSrtPath = Path.ChangeExtension(videoPath, ".srt");

        try
        {
            _formUpdateService.LogMessage("Processing started.");
            _formUpdateService.UpdateMainProcess(0);
            _subtitleService.SetPaths(videoPath, outputSrtPath);

            await _subtitleService.ExtractAudio();
            _formUpdateService.UpdateMainProcess(33);
            _subtitleService.SplitAudioIntoChunks(25 * 1024 * 1024);
            _formUpdateService.UpdateMainProcess(66);
            await _subtitleService.TranslateChunksToSrt();
            _formUpdateService.UpdateMainProcess(100);

            _formUpdateService.LogMessage($"Processing completed. Subtitle file saved at: {outputSrtPath}");
            _formUpdateService.LogMessage($"Subtitle file created: {outputSrtPath}", LogType.Success);

        }
        catch (Exception ex)
        {
            _formUpdateService.LogMessage($"Error during processing: {ex.Message}", LogType.Error);
        }
        finally
        {
            _subtitleService.Cleanup();
        }
    }
}