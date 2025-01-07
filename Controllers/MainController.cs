using VideoTranslator.Services;

namespace VideoTranslator.Controllers;

public class MainController
{
    private readonly MainForm _view;
    private readonly SubtitleService _subtitleService;

    public MainController(MainForm view)
    {
        _view = view;
        string apiKey = FileManager.LoadApiKey();
        _subtitleService = new SubtitleService(apiKey, Log);
    }

    public void SaveApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _view.ShowError("API Key cannot be empty.");
            return;
        }

        FileManager.SaveApiKey(apiKey);
        _view.ShowMessage("API Key saved successfully!", "Success");
        Log("API Key saved.", false);
    }

    public void SelectFile()
    {
        using (var openFileDialog = new OpenFileDialog())
        {
            openFileDialog.Filter = "Video Files|*.mp4;*.mkv;*.avi";
            openFileDialog.InitialDirectory = FileManager.GetLastDirectory();

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string videoPath = openFileDialog.FileName;
                FileManager.SaveLastDirectory(Path.GetDirectoryName(videoPath));
                _view.FilePathTextBox.Text = videoPath;
                Log($"File selected: {videoPath}", false);
            }
        }
    }

    public void ProcessSelectedFile()
    {
        string filePath = _view.FilePathTextBox.Text;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            _view.ShowError("Please select a file to process.");
            return;
        }

        ProcessVideo(filePath);
    }

    private async void ProcessVideo(string videoPath)
    {
        string outputSrtPath = Path.ChangeExtension(videoPath, ".srt");

        try
        {
            _view.AppendToLog("Processing started.");
            _view.UpdateProgress(0);

            await _subtitleService.ExtractAndTranslateAudio(videoPath, outputSrtPath, progress =>
            {
                InvokeOnMainThread(() =>
                {
                    _view.UpdateProgress((int)progress);
                });
            });

            InvokeOnMainThread(() =>
            {
                _view.UpdateProgress(100);
                _view.AppendToLog($"Processing completed. Subtitle file saved at: {outputSrtPath}");
                _view.ShowMessage($"Subtitle file created: {outputSrtPath}", "Success");
            });
        }
        catch (Exception ex)
        {
            InvokeOnMainThread(() =>
            {
                _view.AppendToLog($"Error during processing: {ex.Message}", isError: true);
                _view.ShowError("Error during processing. See log for details.");
            });
        }
    }

    private void Log(string message, bool isError)
    {
        _view.AppendToLog(message, isError);
    }

    private void InvokeOnMainThread(Action action)
    {
        if (_view.InvokeRequired)
        {
            _view.Invoke(action);
        }
        else
        {
            action();
        }
    }
}