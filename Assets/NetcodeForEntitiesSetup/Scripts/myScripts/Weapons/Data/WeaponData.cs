using Unity.Entities;
using Unity.NetCode;

// Komponent na postaci
[GhostComponent] // <--- DODAJ TO
/*public struct ActiveWeapon : IComponentData
{
    [GhostField] public Entity WeaponEntity;
    public Entity PreviousWeaponEntity;
    [GhostField] public byte SelectedWeaponId;
}*/


public struct PlayerInventory : IComponentData
{
    [GhostField] public byte Slot1_WeaponId;
    [GhostField] public byte Slot2_WeaponId;
    [GhostField] public byte Slot3_HandsId;
    [GhostField] public byte Slot4_GrenadeId;
    [GhostField] public byte ActiveSlotIndex;

    [GhostField] public Entity CurrentWeaponEntity; // Dodaj GhostField tutaj
    public byte CurrentlySpawnedWeaponId;
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
    [GhostField] public int maxAmmo;
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
    public Entity ak47;
    public Entity m4a1;
    public Entity mp5;
    public Entity uzi;
    public Entity gun;
    public Entity awp;
    public Entity PKM;
}

public struct WeaponSocket : IComponentData
{
       public Entity WeaponSocketEntity; // Gniazdo w d³oni postaci
}

public struct WeaponSlot : IComponentData
{
    public byte SlotId; // 1 = Pistol, 2 = Shotgun, itd.
}

public struct WeaponPickup : IComponentData
{
    public byte WeaponId; // 1 = Pistol, 2 = Shotgun, 3 = AK47 itd.
    public int Ammo;      // Opcjonalnie: ile amunicji ma w œrodku
}

public struct ActiveWeaponTag : IComponentData { } // Tag dla aktualnie u¿ywanej