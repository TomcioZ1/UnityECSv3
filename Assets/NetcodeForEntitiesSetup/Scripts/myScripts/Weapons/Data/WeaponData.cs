using Unity.Entities;
using Unity.NetCode;

// Komponent na postaci
[GhostComponent] // <--- DODAJ TO
public struct ActiveWeapon : IComponentData
{
    [GhostField] public Entity WeaponEntity;
    public Entity PreviousWeaponEntity;
}

// Komponent na broni (¿eby wiedzia³a do kogo nale¿y)
public struct WeaponOwner : IComponentData
{
    public Entity Entity;
}


public struct WeaponResources : IComponentData
{
    public Entity Pistol;
    // Tutaj mo¿esz dodaæ wiêcej: public Entity Rifle; itp.
}

public struct WeaponSocket : IComponentData
{
       public Entity WeaponSocketEntity; // Gniazdo w d³oni postaci
}