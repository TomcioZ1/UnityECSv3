using Unity.Entities;
using Unity.Mathematics;

public struct BoxComponent : IComponentData
{
    public float InitialScale;
    public float InitialY;
    public float MeshHeight;
    public float CenterOffset;
}