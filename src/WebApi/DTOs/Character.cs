using Lobby.Types;

namespace Lobby.Api;

internal record Character(
    string Name,
    CharacterTypes Type,
    uint Level,
    uint Experience,
    uint Gold,
    bool IsHardcore,
    bool HasDied
)
{
    public static Character FromSaveFile(SaveFile save)
    {
        ArgumentNullException.ThrowIfNull(save);

        var preview = save.GetCharacterPreview();

        return new Character(
            Name: preview.Name,
            Type: preview.Type,
            Level: preview.Level,
            Experience: preview.Experience,
            Gold: preview.Gold,
            IsHardcore: preview.Hardcore,
            HasDied: preview.HasDied
        );
    }
}