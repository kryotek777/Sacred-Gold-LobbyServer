namespace Lobby.Api;

internal record Account(
    int Id,
    DateTime CreationDate,
    string Username,
    string Name,
    string Nick,
    string Clan,
    string Page,
    string Icq,
    string Text
)
{
    public Account(DB.Account dbAccount)
        : this(
            dbAccount.PermId,
            dbAccount.CreationDate,
            dbAccount.Username,
            dbAccount.Name,
            dbAccount.Nick,
            dbAccount.Clan,
            dbAccount.Page,
            dbAccount.Icq,
            dbAccount.Text
        )
    {
    }
}