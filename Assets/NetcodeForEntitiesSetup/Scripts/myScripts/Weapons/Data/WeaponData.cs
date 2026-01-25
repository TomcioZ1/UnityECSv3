using Unity.Entities;
using Unity.NetCode;

// Komponent na postaci
[GhostComponent] // <--- DODAJ TO
public struct ActiveWeapon : IComponentData
{
    [GhostField] public Entity WeaponEntity;
    public Entity PreviousWeaponEntity;
    [GhostField] public byte SelectedWeaponId;
}

// Komponent na broni (¿eby wiedzia³a do kogo nale¿y)
public struct WeaponOwner : IComponentData
{
    public Entity Entity;
}

[GhostComponent]
public struct WeaponData : IComponentData
{
    [GhostField] public Entity ProjectileSpawner;

    [GhostField] public int magSize;
    [GhostField] public int currentAmmo;
    [GhostField] public float fireRate;
    [GhostField] public float reloadTime;
    [GhostField] public float projectileSpeed;
    [GhostField] public int damage;

}

public struct WeaponWorkState : IComponentData
{
    public float NextShotTime;
    public float ReloadTimer;
    public bool IsReloading;
}


public struct WeaponResources : IComponentData
{
    public Entity Pistol;
    public Entity Shotgun;
    public Entity Sniper;
    public Entity ak47;
    // Tutaj mo¿esz dodaæ wiêcej: public Entity Rifle; itp.
}

public struct WeaponSocket : IComponentData
{
       public Entity WeaponSocketEntity; // Gniazdo w d³oni postaci
}

public struct WeaponSlot : IComponentData
{
    public byte SlotId; // 1 = Pistol, 2 = Shotgun, itd.
}

public struct ActiveWeaponTag : IComponentData { } // Tag dla aktualnie u¿ywanej