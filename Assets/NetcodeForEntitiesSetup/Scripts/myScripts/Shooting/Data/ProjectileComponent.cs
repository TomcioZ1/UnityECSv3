using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public struct ProjectileComponent : IComponentData
{
    [GhostField] /*(Quantization = 100)] */public float3 Velocity;
    [GhostField] public double SpawnTime;
    [GhostField] public double DeathTime;
    [GhostField] public Entity Owner;
    [GhostField] public int Damage;
    [GhostField] public float3 TargetPosition;
}




[GhostComponent]
public struct ShotEvent : IComponentData
{
    [GhostField] public int ShotCount;      // Zwiêkszamy przy ka¿dym strzale
    [GhostField] public float3 Direction;   // Kierunek strza³u
    [GhostField] public float3 TargetPos;   // Gdzie raycast trafi³ (punkt koñcowy)
}



// Lokalny (nie-sieciowy) komponent dla wizualnego pocisku
public struct VisualProjectile : IComponentData
{
    public float3 Velocity;
    public float3 TargetPos;
    public float3 Scale;
    public bool IsNew; // Flaga do oznaczania nowo utworzonych pociskówp
    public bool IsExplosive;
}


public struct LastProcessedShot : IComponentData { public int Count; }


public struct ExplosionPrefab : IComponentData
{
    public Entity Value;
}


public struct Lifetime : IComponentData
{
    public float RemainingTime;
}