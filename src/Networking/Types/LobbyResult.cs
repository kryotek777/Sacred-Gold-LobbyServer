using System.Runtime.InteropServices;
using Sacred.Networking.Structs;

namespace Sacred.Networking.Types;

public class LobbyResult
{
    public const int DataSize = SacredHeaderData.DataSize;

    public LobbyResultData Data;

    public LobbyResults Result
    {
        get => (LobbyResults)Data.result;
        set => Data.result = (int)value;
    }

    public SacredMsgType Last
    {
        get => (SacredMsgType)Data.last;
        set => Data.last = (int)value;
    }

    public LobbyResult(LobbyResults result, SacredMsgType last)
    {
        Result = result;
        Last = last;
    }


    public LobbyResult(in LobbyResultData data)
    {
        Data = data;
    }

    public LobbyResult(ReadOnlySpan<byte> data)
    {
        Data = MemoryMarshal.Read<LobbyResultData>(data);
    }

    public byte[] ToArray()
    {
        unsafe
        {
            fixed (LobbyResultData* data = &Data)
            {
                var arr = new byte[DataSize];
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] = data->rawData[i];
                }
                return arr;
            }
        }
    }

    public override string ToString() =>
        $"Result: {Result}, Last: {Last}";
}