using System.Security.Cryptography;
using System.Text;
using Lobby.Types;

namespace Lobby.DB;

public class Account
{
    public int PermId { get; set; }
    public string Username { get; set; }
    public byte[] Password { get; set; }
    public byte[] Salt { get; set; }
    public string Name { get; set; }
    public string Nick { get; set; }
    public string Clan { get; set; }
    public string Page { get; set; }
    public string Icq { get; set; }
    public string Text { get; set; }
    public string Email { get; set; }
    public bool ShowEmail { get; set; }

    public Account(
        int PermId,
        string Username,
        byte[] Password,
        byte[] Salt,
        string Name,
        string Nick,
        string Clan,
        string Page,
        string Icq,
        string Text,
        string Email,
        bool ShowEmail
    )
    {
        this.PermId = PermId;
        this.Username = Username;
        this.Password = Password;
        this.Salt = Salt;
        this.Name = Name;
        this.Nick = Nick;
        this.Clan = Clan;
        this.Page = Page;
        this.Icq = Icq;
        this.Text = Text;
        this.Email = Email;
        this.ShowEmail = ShowEmail;
    }


    // Silence "Non nullable field must be non null"
    // We're setting the fields with helper functions
#pragma warning disable CS8618
    public Account(int PermId, string username, string password)
    {
        this.PermId = PermId;
        SetPasswordAndSalt(password);
        SetProfileData(ProfileData.CreateEmpty(PermId) with { Account = username });
    }
#pragma warning restore CS8618

    public static void CreateHashAndSalt(string password, out byte[] hash, out byte[] salt)
    {
        salt = RandomNumberGenerator.GetBytes(128);
        var data = Encoding.UTF8.GetBytes(password).Concat(salt).ToArray();
        hash = SHA256.HashData(data);
    }

    public void SetPasswordAndSalt(string password)
    {
        CreateHashAndSalt(password, out var hash, out var salt);
        Password = hash;
        Salt = salt;
    }

    public void SetProfileData(ProfileData data)
    {
        Username = data.Account;
        Name = data.Name;
        Nick = data.Nick;
        Clan = data.Clan;
        Page = data.Page;
        Icq = data.Icq;
        Text = data.Text;
        Email = data.Email;
        ShowEmail = data.ShowEmail;
    }

    public ProfileData GetProfileData()
    {
        var charNames = new string[8];

        for (int i = 0; i < 8; i++)
        {

            var save = Database.GetSaveFile(PermId, i + 1);
            var name = save?.GetCharacterPreview().Name;

            if (name != null)
            {
                var dot = name.IndexOf('.');
                if (dot != -1)
                    name = name.Substring(dot + 1);
            }
            else
                name = "";

            charNames[i] = name;

        }

        var data = ProfileData.CreateEmpty(PermId) with
        {
            Account = Username,
            Name = Name,
            Nick = Nick,
            Clan = Clan,
            Page = Page,
            Icq = Icq,
            Text = Text,
            Email = Email,
            ShowEmail = ShowEmail,
            CharactersNames = charNames
        };

        return data;
    }

    public bool CheckPassword(string password)
    {
        var data = Encoding.UTF8.GetBytes(password).Concat(Salt).ToArray();
        var tryHash = SHA256.HashData(data);
        return Password.SequenceEqual(tryHash);
    }
}