using Unity.NetCode;
using Unity.Collections;

public struct ChatMessageRpc : IRpcCommand
{
    public FixedString128Bytes Message;
}
