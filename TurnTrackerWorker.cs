using System.ComponentModel;
using System.Timers;

namespace Panopticon
{
    public class TurnTrackerWorker
    {
        private FileSystemWatcher? fileSystemWatcher;
        private BackgroundWorker? backgroundWorker;
        private System.Timers.Timer? debounceTimer;
        private readonly double debounceDelay = 500; // 500 milliseconds delay

        public void Watch()
        {
            InitializeBackgroundWorker();

            // Initialize the debounce timer
            debounceTimer = new System.Timers.Timer(debounceDelay);
            debounceTimer.Elapsed += OnDebounceElapsed;
            debounceTimer.AutoReset = false; // Ensure it only triggers once after the delay
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

            // Subscribe to events
            fileSystemWatcher.Changed += OnFileChanged;
        }

        // Event handler for file changes
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Reset the timer every time the event is triggered
            if (debounceTimer != null)
            {
                debounceTimer.Stop();
                debounceTimer.Start();
            }
        }

        private void OnDebounceElapsed(object? sender, ElapsedEventArgs e)
        {
            // Deboucing fileSystemWatcher.Changed event is necessary
            // Dom6 currently writes 4 times consecutively to *.2h

            var status = Git.Status();
            if (status != null)
            {
                // Check if a turn has been made
                bool maybe_new_turn = Git.CheckIfFileExists(status.Modified, ".trn");

                if (Game.Settings.Auto_commit)
                {
                    Console.WriteLine($"File changed (auto-commit [enabled])");

                    // Auto calculate turn | sq_turn | compound_turn
                    Game.Timeline.Calculate_Turn(maybe_new_turn);

                    // Commit all changes
                    Git.Commit(Game.Path, Git.Commit_title());

                    // Save current commit information to timelines DB
                    DB.SaveTimeline();

                    // Save settings (Turn(s) have been updated)
                    DB.SaveAllSettings();

                    // Refresh Timeline nodes
                    Timeline.Refresh_Timeline_Nodes();

                }
                else
                {
                    Console.WriteLine($"File changed (auto-commit [disabled])");

                    // Auto calculate turn | sq_turn | compound_turn
                    Game.Timeline.Calculate_Turn(maybe_new_turn);

                    // Refresh Snapshot UI
                    Game.UI.BottomPanel?.Invoke((MethodInvoker)delegate
                    {
                        // Call Snapshot.InitializeComponent() to refresh the UI
                        Snapshot.InitializeComponent();
                    });
                }
            }
        }
    }
}