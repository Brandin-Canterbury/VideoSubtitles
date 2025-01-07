using System.Diagnostics;

namespace VideoTranslator.Services;

public class TimeService
{
    private readonly Stopwatch _stopwatch;
    private long _totalBytesProcessed;
    private readonly long _totalFileSizeInBytes;

    public TimeService(long fileSizeInBytes)
    {
        _stopwatch = new Stopwatch();
        _totalBytesProcessed = 0;
        _totalFileSizeInBytes = fileSizeInBytes;
    }

    public void Start()
    {
        _stopwatch.Start();
    }

    public void UpdateProgress(long bytesProcessed)
    {
        _totalBytesProcessed += bytesProcessed;
    }

    public double GetProcessingRateMBps()
    {
        double elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
        return elapsedSeconds > 0 ? (_totalBytesProcessed / (1024.0 * 1024.0)) / elapsedSeconds : 0.0;
    }

    public TimeSpan GetEstimatedRemainingTime()
    {
        double rate = GetProcessingRateMBps();
        double remainingBytes = _totalFileSizeInBytes - _totalBytesProcessed;
        return rate > 0 ? TimeSpan.FromSeconds(remainingBytes / (rate * 1024.0 * 1024.0)) : TimeSpan.MaxValue;
    }

    public TimeSpan GetElapsedTime()
    {
        return _stopwatch.Elapsed;
    }

    public DateTime GetEstimatedCompletionTime()
    {
        return DateTime.Now.Add(GetEstimatedRemainingTime());
    }

    public void Stop()
    {
        _stopwatch.Stop();
    }

    public void Reset()
    {
        _stopwatch.Reset();
        _totalBytesProcessed = 0;
    }
}