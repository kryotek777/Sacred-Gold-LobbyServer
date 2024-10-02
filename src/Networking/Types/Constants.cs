namespace Sacred.Networking.Types;

public static class Constants
{
    public const uint TincatMagic = 0xDABAFBEF;
    public const uint ServerId = 0xEFFFFFCC;
    public const uint UnconnectedClientId = 0xEFFFFFCC;

    public const ushort ModuleId = 9910;

    public const int TincatHeaderSize = 28;
    public const int ChatMessageMaxLength = 256;
    public const int UsernameMaxLength = 80;
    public const int PasswordMaxLength = 32;
    public const int CdKeyLength = 21;

    public const int Utf16ProfileStringLength = 160;
    public const int Utf16ProfileTextLength = 512;

    public const int ProfileBlockId = 10;
}