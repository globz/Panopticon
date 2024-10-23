using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Panopticon;

public class FileWatcherManager : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly string _path;
    private readonly CancellationTokenSource _cancellationTokenSource;

    private System.Timers.Timer? _debounceTimer;

    private readonly double _debounceDelay = 500;

    private readonly Stopwatch _stopwatch = new Stopwatch();

    public FileWatcherManager(string watchPath)
    {
        _path = watchPath;
        _cancellationTokenSource = new CancellationTokenSource();

        _watcher = new FileSystemWatcher
        {
            Path = _path,
            NotifyFilter = NotifyFilters.LastWrite
                          | NotifyFilters.Size
                          | NotifyFilters.Attributes,
            EnableRaisingEvents = false,
            IncludeSubdirectories = false
        };

        // Dom6 orders for a given turn (if .2h is solely updated without the .trn file then it means save & quit)
        _watcher.Filters.Add("*.2h");

        // Dom6 Turn file (this file only update itself once a turn has been completed)
        _watcher.Filters.Add("*.trn");

        _watcher.InternalBufferSize = 64 * 1024; // 64 KB
        Console.WriteLine($"Current buffer size: {_watcher.InternalBufferSize}");

        // Setup event handlers
        _watcher.Changed += OnFileChanged;
        _watcher.Error += OnError;
    }

    public void Start()
    {
        try
        {
            Console.WriteLine($"Starting file watcher manager in {_path}");
            // Initialize the debounce timer
            _debounceTimer = new System.Timers.Timer(_debounceDelay);
            _debounceTimer.Elapsed += OnDebounceElapsed;
            _debounceTimer.AutoReset = false; // Ensure it only triggers once after the delay
            _watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting file watcher manager: {ex.Message}");
            throw;
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Handle file changes asynchronously
        Task.Run(() =>
        {
            try
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    return;

                Console.WriteLine($"File {e.ChangeType}: {e.FullPath}");

                if (_debounceTimer != null)
                {

                    Console.WriteLine($"is S&Q: {e.FullPath.EndsWith(".2h")}");
                    Console.WriteLine($"is NEW TURN: {e.FullPath.EndsWith(".trn")}");
                    Console.WriteLine($"Current debounceDelay interval: {_debounceTimer.Interval}");

                    // Reset timers every time the event is triggered
                    _debounceTimer.Stop();
                    Console.WriteLine($"[Timer]~Elapsed time since last event:{GetElapsedTime()}");
                    _debounceTimer.Start();
                    _stopwatch.Restart();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file change for {e.FullPath}: {ex.Message}");
            }
        });
    }

    // Debouncing FileSystemWatcher.Changed event is necessary
    // Dom6 currently writes 4 times consecutively to each files (.2h & .trn)
    private void OnDebounceElapsed(object? sender, ElapsedEventArgs e)
    {
        Console.WriteLine("OnDebounceElapsed");

        // Stop timers
        _debounceTimer?.Stop();
        _stopwatch.Stop();

        TurnTracker.Process();
    }

    public double GetElapsedTime()
    {
        var elapsed = _stopwatch.Elapsed.TotalMilliseconds;
        double remainingTime = _debounceDelay - elapsed;
        double remaingTime_to_subtract = remainingTime > 0 ? remainingTime : 0;
        return _debounceDelay - remaingTime_to_subtract;
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        Console.WriteLine("An error occurred: " + e.GetException().Message);

        if (e.GetException() is InternalBufferOverflowException)
        {
            Console.WriteLine("The file system watcher buffer overflowed. Consider increasing the buffer size.");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellationTokenSource.Cancel();
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}