using System.ComponentModel;
using System.Diagnostics;
using System.Timers;

namespace Panopticon
{
    public class TurnTrackerWorker
    {
        private FileSystemWatcher? fileSystemWatcher;
        private BackgroundWorker? backgroundWorker;
        private System.Timers.Timer? debounceTimer;

        // Debouncing delay for fileSystemWatcher.Changed (TODO: perhaps implement an Adaptive Debounce Delay?)
        // Confirmed: The longer the game the goes the longer it takes to process a turn.
        // 5000 ms does not work for longer running games (creates 2 commit (S&Q & Turn))
        // Probing the size of 2h file may be a good indicator for increasing debounceDelay
        private readonly double debounceDelay = 8000;

        // Initialize stopWatch for elapsed time tracking
        private Stopwatch stopwatch = new Stopwatch();

        public void Watch()
        {
            // Initialize the debounce timer
            debounceTimer = new System.Timers.Timer(debounceDelay);
            debounceTimer.Elapsed += OnDebounceElapsed;
            debounceTimer.AutoReset = false; // Ensure it only triggers once after the delay

            InitializeBackgroundWorker();
        }

        private void InitializeBackgroundWorker()
        {
            // Initialize the BackgroundWorker
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += BackgroundWorker_DoWork;

            // Start it in the background
            backgroundWorker.RunWorkerAsync();
        }

        private void BackgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            fileSystemWatcher = new FileSystemWatcher();

            fileSystemWatcher.Path = @$"{Game.Path}";

            fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;

            // Dom6 orders for a given turn (if .2h is solely updated without the .trn file then it means save & quit)
            fileSystemWatcher.Filters.Add("*.2h");

            // Dom6 Turn file (this file only update itself once a turn has been completed)
            fileSystemWatcher.Filters.Add("*.trn");

            fileSystemWatcher.IncludeSubdirectories = false;
            fileSystemWatcher.EnableRaisingEvents = true;

            //fileSystemWatcher.InternalBufferSize = 64 * 1024; // 64 KB
            Console.WriteLine($"Current buffer size: {fileSystemWatcher.InternalBufferSize}");

            // Subscribe to events
            fileSystemWatcher.Changed += OnFileChanged;

            // Subscribe to the Error event
            fileSystemWatcher.Error += OnError;
        }

        // Event handler for file changes
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("OnFileChanged");

            // Reset the timer every time the event is triggered
            if (debounceTimer != null)
            {
                Console.WriteLine($"is S&Q: {e.FullPath.Contains(".2h")}");
                Console.WriteLine($"is NEW TURN: {e.FullPath.Contains(".trn")}");
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

        // Debouncing fileSystemWatcher.Changed event is necessary
        // Dom6 currently writes 4 times consecutively to *.2h
        private void OnDebounceElapsed(object? sender, ElapsedEventArgs e)
        {
            Console.WriteLine("OnDebounceElapsed");
            debounceTimer?.Stop();
            stopwatch.Stop();
            var status = Git.Status();
            if (status != null)
            {
                // Check if a turn has been made
                bool maybe_new_turn = Git.CheckIfFileExists(status.Modified, ".trn");

                if (Game.Settings.Auto_commit)
                {
                    // Auto-commit enabled
                    Console.WriteLine($"File changed (auto-commit [enabled])");

                    // Auto update turn | sq_turn | compound_turn
                    Game.Timeline.Update_Turn(maybe_new_turn);

                    // Commit all changes
                    Git.Commit(Game.Path, Git.Commit_title(maybe_new_turn));

                    // Save current commit information to timelines DB
                    DB.SaveTimeline(Git.Commit_title(maybe_new_turn));

                    // Save settings (Turn(s) have been updated)
                    DB.SaveAllSettings();

                    if (!maybe_new_turn)
                    {
                        // Added default timeline notes for S&Q
                        DB.SaveTimelineNotes($"Save & Quit on turn {Game.Settings.Turn}", Git.Commit_title(maybe_new_turn));
                    }

                    Game.UI.TreeViewLeft.Invoke((MethodInvoker)delegate
                    {
                        // Refresh Timeline nodes
                        Timeline.Refresh_Timeline_Nodes();
                    });
                }
                else
                {
                    // Auto-commit disabled
                    Console.WriteLine($"File changed (auto-commit [disabled])");

                    // Auto calculate turn | sq_turn | compound_turn
                    Game.Timeline.Update_Turn(maybe_new_turn);

                    // Refresh Snapshot UI
                    Game.UI.BottomPanel?.Invoke((MethodInvoker)delegate
                    {
                        // Call Snapshot.InitializeComponent() to refresh the UI
                        Snapshot.InitializeComponent();
                    });
                }
            }
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
    }
}