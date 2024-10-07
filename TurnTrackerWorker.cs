using System;
using System.IO;
using System.ComponentModel;

namespace Panopticon
{
    public class TurnTrackerWorker
    {
        private FileSystemWatcher? fileSystemWatcher;
        private BackgroundWorker? backgroundWorker;

        public void Watch()
        {
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

        private void BackgroundWorker_DoWork(object ?sender, DoWorkEventArgs e)
        {
            fileSystemWatcher = new FileSystemWatcher();

            fileSystemWatcher.Path = Game.Path ?? "ERROR";

            fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;

            // Dom6 orders for a given turn (if .2h is solely updated without the .trn file then it means save & quit)
            fileSystemWatcher.Filters.Add("*.2h");

            // Dom6 Turn file (this file only update itself once a turn has been completed)
            fileSystemWatcher.Filters.Add("*.trn");

            fileSystemWatcher.IncludeSubdirectories = false;
            fileSystemWatcher.EnableRaisingEvents = true;

            // Subscribe to events
            fileSystemWatcher.Created += OnFileCreated;
            fileSystemWatcher.Changed += OnFileChanged;
            fileSystemWatcher.Deleted += OnFileDeleted;
            fileSystemWatcher.Renamed += OnFileRenamed;
        }

        // Event handler for file creation
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"File created: {e.FullPath}");
        }

        // Event handler for file changes
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"File changed: {e.FullPath}");
        }

        // Event handler for file deletion
        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"File deleted: {e.FullPath}");
        }

        // Event handler for file renaming
        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine($"File renamed: {e.OldFullPath} to {e.FullPath}");
        }

    }
}