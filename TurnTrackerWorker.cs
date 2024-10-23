using System.ComponentModel;
using System.Diagnostics;
using System.Timers;

namespace Panopticon
{
    public class TurnTrackerWorker
    {
        private FileSystemWatcher? fileSystemWatcher;
        public static BackgroundWorker backgroundWorker = new BackgroundWorker();
        private System.Timers.Timer? debounceTimer;

        private readonly double debounceDelay = 500;

        // Initialize stopWatch for elapsed time tracking
        private Stopwatch stopwatch = new Stopwatch();

        public void Watch()
        {
            // Initialize the debounce timer
            debounceTimer = new System.Timers.Timer(debounceDelay);
            debounceTimer.Elapsed += OnDebounceElapsed;
            debounceTimer.AutoReset = false; // Ensure it only triggers once after the delay

            // Initialize the BackgroundWorker
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerSupportsCancellation = true;

            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += TurnTrackerWorker_RunWorkerCompleted;

            // Start it in the background
            backgroundWorker.RunWorkerAsync();
        }

        private void BackgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            fileSystemWatcher = new FileSystemWatcher();

            fileSystemWatcher.Path = @$"{Game.Path}";

            fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes;

            // Dom6 orders for a given turn (if .2h is solely updated without the .trn file then it means save & quit)
            fileSystemWatcher.Filters.Add("*.2h");

            // Dom6 Turn file (this file only update itself once a turn has been completed)
            fileSystemWatcher.Filters.Add("*.trn");

            fileSystemWatcher.IncludeSubdirectories = false;
            fileSystemWatcher.EnableRaisingEvents = true;

            fileSystemWatcher.InternalBufferSize = 64 * 1024; // 64 KB
            Console.WriteLine($"Current buffer size: {fileSystemWatcher.InternalBufferSize}");

            // Subscribe to events
            fileSystemWatcher.Changed += OnFileChanged;

            // Subscribe to the Error event
            fileSystemWatcher.Error += OnError;

            try
            {
                // Keep the worker alive until cancellation is requested
                while (!backgroundWorker.CancellationPending)
                {
                    Thread.Sleep(1000);  // Keep loop responsive and low on CPU usage
                }
            }
            finally
            {
                // Clean up FileSystemWatcher on exit
                fileSystemWatcher.Dispose();
            }

        }

        // Event handler for file changes
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"OnFileChanged: {e.ChangeType} => {e.FullPath}");

            if (debounceTimer != null)
            {

                Console.WriteLine($"is S&Q: {e.FullPath.EndsWith(".2h")}");
                Console.WriteLine($"is NEW TURN: {e.FullPath.EndsWith(".trn")}");
                Console.WriteLine($"Current debounceDelay interval: {debounceTimer.Interval}");

                // Reset the timers every time the event is triggered
                debounceTimer.Stop();
                Console.WriteLine($"[Timer]~Elapsed time since last event:{GetElapsedTime()}");
                debounceTimer.Start();
                stopwatch.Restart();
            }
        }

        public double GetElapsedTime()
        {
            var elapsed = stopwatch.Elapsed.TotalMilliseconds;
            double remainingTime = debounceDelay - elapsed;
            double remaingTime_to_subtract = remainingTime > 0 ? remainingTime : 0;
            return debounceDelay - remaingTime_to_subtract;
        }

        // Debouncing FileSystemWatcher.Changed event is necessary
        // Dom6 currently writes 4 times consecutively to each files (.2h & .trn)
        private void OnDebounceElapsed(object? sender, ElapsedEventArgs e)
        {
            Console.WriteLine("OnDebounceElapsed");

            // Stop timers
            debounceTimer?.Stop();
            stopwatch.Stop();

            TurnTracker.Process();
        }

        // Error event handler
        private static void OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("An error occurred: " + e.GetException().Message);

            if (e.GetException() is InternalBufferOverflowException)
            {
                Console.WriteLine("The file system watcher buffer overflowed. Consider increasing the buffer size.");
            }
        }

        // Clean up logic when the worker completes
        private static void TurnTrackerWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Console.WriteLine("TurnTrackerWorker stopped gracefully.");
            }
            else if (e.Error != null)
            {
                Console.WriteLine($"TurnTrackerWorker encountered an error: {e.Error.Message}");
            }
        }

        // Handle application shutdown and stop the worker gracefully
        public bool Cancel()
        {
            if (backgroundWorker.IsBusy)
            {
                backgroundWorker.CancelAsync();  // Request cancellation
                Console.WriteLine("Stopping FileSystemWatcher...");
                Thread.Sleep(500);  // Give it time to clean up (optional)
            }
            return true;
        }
    }
}