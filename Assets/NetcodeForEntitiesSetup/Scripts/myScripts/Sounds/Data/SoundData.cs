using Unity.Entities;
using Unity.Mathematics;

public struct PlaySoundRequest : IComponentData
{
    public int SoundID; // 0 = strza³, 1 = wybuch, itp.
    public float3 Position;
    public bool IsLoop;
}