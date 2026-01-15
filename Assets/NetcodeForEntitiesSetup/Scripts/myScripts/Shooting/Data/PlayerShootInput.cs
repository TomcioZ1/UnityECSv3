using Unity.Mathematics;
using Unity.NetCode;

public struct PlayerShootInput : IInputComponentData
{
    [GhostField] public byte ShootPrimary;
    [GhostField] public float3 AimDirection;
}