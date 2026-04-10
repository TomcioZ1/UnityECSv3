using Unity.Collections;
using Unity.Entities;

public struct ChatMessageEvent : IComponentData
{
    public FixedString64Bytes Sender;
    public FixedString128Bytes Message;
}
