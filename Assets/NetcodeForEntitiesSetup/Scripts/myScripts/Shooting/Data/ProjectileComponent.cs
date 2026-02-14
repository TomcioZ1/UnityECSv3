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

public struct  BulletSpawnTimer : IComponentData
{
    public double Timer;
    public double AddedTime;
}

public struct ProjectileComponentNoScary : IComponentData
{
    public float3 Velocity;
    public Entity Owner;
 
}

[GhostComponent]
public struct ShotEvent : IComponentData
{
    [GhostField] public int ShotCount;      // Zwiêkszamy przy ka¿dym strzale
    [GhostField] public float3 Direction;   // Kierunek strza³u
    [GhostField] public float3 TargetPos;   // Gdzie raycast trafi³ (punkt koñcowy)
}

// Pomocniczy komponent na prefabie gracza, ¿eby system wiedzia³, gdzie jest lufa
public struct MuzzleOffset : IComponentData
{
    public float3 Value;
}

// Lokalny (nie-sieciowy) komponent dla wizualnego pocisku
public struct VisualProjectile : IComponentData
{
    public float3 Velocity;
    public float3 TargetPos;
    public float DeathTime;
}