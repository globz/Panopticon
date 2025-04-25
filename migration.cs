using System;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Reflection;

namespace Panopticon;
public class Migration
{
    public static void Process()
    {
        // Create migration table with default configuration
        Initialize_Migration_Table();

        // Retrieve current migration values from migration table (app_version, upgrade_count)                
        Retrieve_Values();

        // Future migration steps to follow:             
        // 1. Add new migration precedure below
        // 2. Make use of Game.Migration.App_version||Game.Migration.Upgrade_count to determine if a migration procedure must be applied
        // 3. At the end of the procedure, increase Game.Migration.Upgrade_count by 1
        // 4. if (Game.Migration.App_version != Game.Settings.App_version && Game.Migration.Upgrade_count == 0) { }

        // Migration procedures may be added below this line:
        // TODO change -1 to procedure_position 0 when a real procedure is ready to be applied
        Maybe_apply(-1, My_migration_procedure);

        // Workflow:
        // First make sure you did bump the version of <InformationalVersion> else you will never trigger a new procedure!

        // Here's a good way to detect if a migration must be applied
        // Call you procedure unto Maybe_apply(int procedure_position,  Lambda fn) along with the position # of your procedure
        // Apply migration procedure
        // increase Game.Migration.Upgrade_count by 1 via Increase_Upgrade_Count()
        // Game.Migration.Upgrade_count is now 1

        // Next we have another procedure (2nd)
        // This will now check for the following:
        // IF Game.Migration.App_version != Game.Settings.App_version
        // AND Game.Migration.Upgrade_count == 1
        // Apply migration procedure
        // increase Game.Migration.Upgrade_count by 1 via Increase_Upgrade_Count()
        // Game.Migration.Upgrade_count is now 2

        // There is no more procedure to apply, lets update Game.Migration.App_version to match Game.Settings.App_version
        Update_App_Version();

        Console.WriteLine("Game.Migration.App_version: " + Game.Migration.App_version);
        Console.WriteLine("Game.Migration.Upgrade_count: " + Game.Migration.Upgrade_count);
    }

    private static void Initialize_Migration_Table()
    {
        using (var statement = DB.Query("CREATE TABLE IF NOT EXISTS migration (app_version TEXT, upgrade_count INT, PRIMARY KEY (app_version))"))
        {
            statement.ExecuteNonQuery();
        }

        using (var statement = DB.Query("SELECT app_version FROM migration"))
        {
            var data = statement.ExecuteScalar();
            if (data != null)
            {
                Console.WriteLine("Migration table is already initialized & configured.");
                return;
            }
        }

        Console.WriteLine("Migration table is initialized but requires default configuration.");
        using (var statement = DB.Query("INSERT INTO migration (app_version, upgrade_count) VALUES (@app_version, @upgrade_count)"))
        {
            statement.Parameters.Add("@app_version", SqliteType.Text).Value = Game.Migration.App_version; // Default value
            statement.Parameters.Add("@upgrade_count", SqliteType.Integer).Value = Game.Migration.Upgrade_count; // Default value
            statement.ExecuteNonQuery();
        }
    }

    private static void Retrieve_Values()
    {
        using var statement = DB.Query("SELECT app_version, upgrade_count FROM migration");
        DB.ReadData(statement, DB.LoadMigrationData);
    }

    private static void Update_App_Version()
    {
        using var statement = DB.Query("UPDATE migration SET app_version = @app_version");
        statement.Parameters.Add("@app_version", SqliteType.Text).Value = Game.Settings.App_version; // Current App version
        statement.ExecuteNonQuery();
    }

    private static void Increase_Upgrade_Count()
    {
        using var statement = DB.Query("UPDATE migration SET upgrade_count = @upgrade_count");
        Game.Migration.Upgrade_count++;
        statement.Parameters.Add("@upgrade_count", SqliteType.Integer).Value = Game.Migration.Upgrade_count; // +1
        statement.ExecuteNonQuery();
    }

    private delegate void Lambda();
    private static void Maybe_apply(int procedure_position,  Lambda fn)
    {
        if (Game.Migration.App_version != Game.Settings.App_version && Game.Migration.Upgrade_count == procedure_position) 
        { 
            fn();
        }
    }

    private static void My_migration_procedure()
    {
        Console.WriteLine("Template migration procedure - #0");

        // TODO add extra validation if needed

        // TODO add procedure

        // Increase upgrade_count
        Increase_Upgrade_Count();
    }
}
