using Unity.NetCode;
using Unity.Collections;

public struct ChatMessageRpc : IRpcCommand
{
    public FixedString64Bytes Sender;
    public FixedString128Bytes Message;
}
