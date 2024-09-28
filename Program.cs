using Microsoft.Data.Sqlite;

namespace Panopticon;

/* public static class Helper
{
    public static object? GetPropertyValue(this object T, string PropName)
    {
        return T.GetType().GetProperty(PropName)?.GetValue(T, null);

    }

} */
static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        // Set UI theme        
        Game.UI.Theme = ColorTranslator.FromHtml("#313338");
        Game.UI.ForeColor = Color.GhostWhite;

        // Set default Settings values
        Game.Settings.Turn = 0;
        Game.Settings.Prefix = "";
        Game.Settings.Suffix = "_TURN_";
        Game.Settings.Auto_commit = false;
        Game.Settings.Auto_commit_on_save_and_quit = false;

        Application.Run(new Home());
    }

}

public static class Game
{
    public static String? Path { get; set; }
    public static String? Name { get; set; }

    public static class UI
    {
        public static SplitContainer? VerticalSplitContainer { get; set; }
        public static SplitContainer? HorizontalSplitContainer { get; set; }
        public static TreeView? TreeViewLeft { get; set; }
        public static Panel? TopPanel { get; set; }
        public static Panel? BottomPanel { get; set; }
        public static TreeNode? Timeline_settings { get; set; }
        public static TreeNode? Timeline_history { get; set; }
        public static Color Theme { get; set; }
        public static Color ForeColor { get; set; }
    }

    public static class Settings
    {
        public static Boolean Auto_commit { get; set; }
        public static Boolean Auto_commit_on_save_and_quit { get; set; }
        public static String? Prefix { get; set; }
        public static String? Suffix { get; set; }
        public static decimal Turn { get; set; }
    }

}

public static class DB
{
    // Types : https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/types

    private static readonly string DatabasePath = AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\db\panopticon.db";
    private static readonly string DatabaseSource = $"Data Source={DatabasePath}";
    private static readonly SqliteConnection Connection = new(DatabaseSource);

    public static void Open()
    {
        DB.Connection.Open();
    }

    public static void Close()
    {
        DB.Connection.Close();
    }

    public static SqliteCommand Query(string text)
    {
        var statement = DB.Connection.CreateCommand();
        statement.CommandText = text;
        return statement;
    }

}

