using VideoTranslator.Enums;

namespace VideoTranslator.Services;

public class FormUpdateService(MainForm view)
{
    public void UpdateApiKey(string value)
    {
        InvokeOnMainThread(() =>
        {
            view.ApiKeyTextBox.Text = value;
        });
    }
    
    public void UpdateFilePath(string value)
    {
        InvokeOnMainThread(() =>
        {
            view.FilePathTextBox.Text = value;
        });
    }
    
    public void UpdateMainProcess(int value)
    {
        InvokeOnMainThread(() =>
        {
            view.UpdateMainProgress(value);
        });
    }
    
    public void UpdateSecondaryProcess(int value)
    {
        InvokeOnMainThread(() =>
        {
            view.UpdateSecondaryProgress(value);
        });
    }
    
    public void LogMessage(string message, LogType logType = LogType.Default)
    {
        InvokeOnMainThread(() =>
        {
            view.AppendToLog(message, logType);
        });
    }

    private void InvokeOnMainThread(Action action)
    {
        if (view.IsHandleCreated)
        {
            view.BeginInvoke(action);
        }
    }
}