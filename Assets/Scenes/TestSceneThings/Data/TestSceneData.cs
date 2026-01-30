using Unity.Entities;
using Unity.Mathematics;

public struct TestPlayerTag : IComponentData
{
    
}
public struct  BoxTag:IComponentData
{
    
}
public struct PlayerMoveInput : IComponentData
{
    public float2 Value;
    public float Speed;
}
