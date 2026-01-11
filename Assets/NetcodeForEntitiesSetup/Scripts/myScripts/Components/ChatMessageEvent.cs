using Unity.Entities;
using Unity.Collections;

// Ka¿da encja tego typu reprezentuje jedn¹ wiadomoœæ chatu do wyœwietlenia w UI
public struct ChatMessageEvent : IComponentData
{
    public FixedString128Bytes Message;
}
