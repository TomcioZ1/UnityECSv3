using Unity.Entities;
using Unity.NetCode;

public struct PerformanceStats : IComponentData
{
    public float FPS;
    public float Ping;
}


public struct TimeToStopTheGame : IComponentData
{
    // GhostField pozwala na synchronizacjê tego pola z klientami
    public float ExactTimeOfGameStop;
}

public struct GameStartTimeResponse : IRpcCommand
{
    public float ExactTimeOfGameStop;
}