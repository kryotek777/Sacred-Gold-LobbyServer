using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using Lobby.Types;

namespace Lobby.DB;

public static class Database
{
    private static SQLiteConnection connection = null!;
    private static ReaderWriterLockSlim rwLock = new(LockRecursionPolicy.SupportsRecursion);

    public static void Load()
    {
        if (!Config.Instance.StorePersistentData)
            return;

        rwLock.EnterWriteLock();
        try
        {
            var dbPath = Config.Instance.DatabasePath;

            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(Config.Instance.TemplatePath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(Config.Instance.SavesPath)!);

            if (!File.Exists(dbPath))
                SQLiteConnection.CreateFile(dbPath);

            connection = new SQLiteConnection($"Data Source={dbPath}");

            connection.Open();

            CreateTables();
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    public static void Close()
    {
        rwLock.EnterWriteLock();
        try
        {
            connection.Close();
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    public static Account CreateAccount(string username, string password)
    {
        rwLock.EnterWriteLock();
        try
        {
            string idQuery = @"SELECT MAX(PermId) FROM Accounts";

            using var command = new SQLiteCommand(idQuery, connection);

            var result = command.ExecuteScalar();

            var permId = result != DBNull.Value ? (int)(long)result : 0;

            ++permId;

            var account = new Account(permId, username, password);

            CreateAccount(account);

            return account;
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    public static void CreateAccount(Account account)
    {
        rwLock.EnterWriteLock();
        try
        {
            string insertQuery = @"
                    INSERT INTO Accounts 
                    (PermId, Username, Password, Salt, Name, Nick, Clan, Page, Icq, Text, Email, ShowEmail) 
                    VALUES (@PermId, @Username, @Password, @Salt, @Name, @Nick, @Clan, @Page, @Icq, @Text, @Email, @ShowEmail)";

            using var command = new SQLiteCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@PermId", account.PermId);
            command.Parameters.AddWithValue("@Username", account.Username);
            command.Parameters.AddWithValue("@Password", account.Password);
            command.Parameters.AddWithValue("@Salt", account.Salt);
            command.Parameters.AddWithValue("@Name", account.Name);
            command.Parameters.AddWithValue("@Nick", account.Nick);
            command.Parameters.AddWithValue("@Clan", account.Clan);
            command.Parameters.AddWithValue("@Page", account.Page);
            command.Parameters.AddWithValue("@Icq", account.Icq);
            command.Parameters.AddWithValue("@Text", account.Text);
            command.Parameters.AddWithValue("@Email", account.Email);
            command.Parameters.AddWithValue("@ShowEmail", account.ShowEmail ? 1 : 0);

            command.ExecuteNonQuery();
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    public static bool TryGetAccount(string username, [NotNullWhen(true)] out Account? account)
    {
        rwLock.EnterReadLock();
        try
        {
            var query = "SELECT * FROM Accounts WHERE Username = @Username LIMIT 1";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);

            using var reader = command.ExecuteReader(System.Data.CommandBehavior.KeyInfo);

            if (reader.Read())
            {
                account = ReadAccount(reader);
                return true;
            }
            else
            {
                account = null;
                return false;
            }
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    public static bool TryGetAccount(int permId, [NotNullWhen(true)] out Account? account)
    {
        rwLock.EnterReadLock();
        try
        {
            var query = "SELECT * FROM Accounts WHERE PermId = @PermId LIMIT 1";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@PermId", permId);

            using var reader = command.ExecuteReader(System.Data.CommandBehavior.KeyInfo);
            if (reader.Read())
            {
                account = ReadAccount(reader);
                return true;
            }
            else
            {
                account = null;
                return false;
            }
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    public static bool TryLogin(string username, string password, [NotNullWhen(true)] out Account? account)
    {
        rwLock.EnterReadLock();
        try
        {
            if (TryGetAccount(username, out account))
            {
                return account.CheckPassword(password);
            }
            else
            {
                account = null;
                return false;
            }
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    public static void SetProfile(int permid, ProfileData profileData)
    {
        rwLock.EnterWriteLock();
        try
        {
            string updateQuery = @"
        UPDATE Accounts SET
            Name = @Name,
            Nick = @Nick,
            Clan = @Clan,
            Page = @Page,
            Icq = @Icq,
            Text = @Text,
            Email = @Email,
            ShowEmail = @ShowEmail
        WHERE PermId = @PermId";

            using var command = new SQLiteCommand(updateQuery, connection);
            command.Parameters.AddWithValue("@PermId", permid);
            command.Parameters.AddWithValue("@Name", profileData.Name);
            command.Parameters.AddWithValue("@Nick", profileData.Nick);
            command.Parameters.AddWithValue("@Clan", profileData.Clan);
            command.Parameters.AddWithValue("@Page", profileData.Page);
            command.Parameters.AddWithValue("@Icq", profileData.Icq);
            command.Parameters.AddWithValue("@Text", profileData.Text);
            command.Parameters.AddWithValue("@Email", profileData.Email);
            command.Parameters.AddWithValue("@ShowEmail", profileData.ShowEmail ? 1 : 0);

            command.ExecuteNonQuery();
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }


    public static SaveFile? GetSaveFile(int permId, int blockId)
    {
        if (!Config.Instance.StorePersistentData)
            return null;

        rwLock.EnterReadLock();
        try
        {
            var path = Path.Combine(Config.Instance.SavesPath, permId.ToString(), $"Hero{blockId - 1:D2}.pax");

            if (!Path.Exists(path))
                return null;

            return new SaveFile(path);
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    public static void SetSaveFile(int permId, int blockId, SaveFile saveFile)
    {
        if (!Config.Instance.StorePersistentData)
            return;
    
        rwLock.EnterWriteLock();
        try
        {
            var path = Path.Combine(Config.Instance.SavesPath, permId.ToString(), $"Hero{blockId - 1:D2}.pax");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            saveFile.Save(path);
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    public static void InitSaveFile(int permId, int blockId, int templateId, string name)
    {
        if (!Config.Instance.StorePersistentData)
            return;

        rwLock.EnterWriteLock();
        try
        {
            var path = Path.Combine(Config.Instance.SavesPath, permId.ToString(), $"Hero{blockId - 1:D2}.pax");

            if (templateId == 16)
            {
                File.Delete(path);
            }
            else
            {
                var templatePath = Path.Combine(Config.Instance.TemplatePath, $"Hero{templateId:D2}.ptx");
                var saveFile = new SaveFile(templatePath);

                var preview = saveFile.GetCharacterPreview();

                preview = preview with
                {
                    Name = name
                };

                saveFile.SetCharacterPreview(preview);

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                if (File.Exists(path))
                    File.Delete(path);

                saveFile.Save(path);
            }
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    private static void CreateTables()
    {
        // SQL query to create the Accounts table
        string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Accounts (
                        PermId INTEGER PRIMARY KEY,
                        Username TEXT NOT NULL,
                        Password BLOB NOT NULL,
                        Salt BLOB NOT NULL,
                        Name TEXT,
                        Nick TEXT,
                        Clan TEXT,
                        Page TEXT,
                        Icq TEXT,
                        Text TEXT,
                        Email TEXT,
                        ShowEmail INTEGER NOT NULL
                    );";

        // Execute the query
        using var command = new SQLiteCommand(createTableQuery, connection);

        command.ExecuteNonQuery(System.Data.CommandBehavior.KeyInfo);
    }

    private static Account ReadAccount(SQLiteDataReader reader)
    {
        int PermId = (int)reader.GetWithName<long>("PermId");
        string Username = reader.GetWithName<string>("Username");
        byte[] Password = reader.GetBlobValue("Password");
        byte[] Salt = reader.GetBlobValue("Salt");
        string Name = reader.GetWithName<string>("Name");
        string Nick = reader.GetWithName<string>("Nick");
        string Clan = reader.GetWithName<string>("Clan");
        string Page = reader.GetWithName<string>("Page");
        string Icq = reader.GetWithName<string>("Icq");
        string Text = reader.GetWithName<string>("Text");
        string Email = reader.GetWithName<string>("Email");
        bool ShowEmail = reader.GetWithName<bool>("ShowEmail");

        var account = new Account(
            PermId,
            Username,
            Password,
            Salt,
            Name,
            Nick,
            Clan,
            Page,
            Icq,
            Text,
            Email,
            ShowEmail
        );

        return account;
    }
}
