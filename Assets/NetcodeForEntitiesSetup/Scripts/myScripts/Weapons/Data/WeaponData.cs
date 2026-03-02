using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;




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
    [GhostField] public float3 ProjectileSpawner;
    [GhostField] public int magSize;
    [GhostField] public int currentAmmo;
    [GhostField] public int maxAmmo;
    [GhostField] public float fireRate;
    [GhostField] public float reloadTime;
    [GhostField] public float projectileSpeed;
    [GhostField] public int damage;
    [GhostField] public bool isNormalGun;
    [GhostField] public bool isShotgun;
    [GhostField] public bool isGranadeLauncher;
    [GhostField] public float maxRange;
    /*[GhostField]*/
    public float cameraOffset;
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

public struct  ShotgunTag : IComponentData{}
public struct NormalGunTag : IComponentData { }
public struct GranadeLauncherTag : IComponentData { }





public struct ReloadUIRequest : IComponentData
{
    public float StartTime;    // Czas rozpoczêcia (SystemAPI.Time.ElapsedTime)
    public float Duration;     // Czas trwania (z WeaponData.reloadTime)
}











public struct PlayerInventoryv2 : IComponentData
{
    [GhostField] public byte ActiveSlotIndex; // 1 = Broñ, 2 = Rêce
    [GhostField] public byte WeaponId;       // ID aktualnie posiadanej broni w Slocie 1
    [GhostField] public Entity CurrentWeaponEntity;

    // Pomocnicze do detekcji zmian (nie synchronizowane)
    public byte LastSpawnedId;
    public byte LastActiveSlot;
    public static PlayerInventoryv2 Default => new PlayerInventoryv2
    {
        ActiveSlotIndex = 2, // Zaczynamy z rêkami
        WeaponId = 0,
        LastActiveSlot = 0, // Inne ni¿ ActiveSlotIndex, ¿eby wymusiæ spawn na starcie
        LastSpawnedId = 255
    };
}

public struct WeaponResourcesv2 : IComponentData
{
    // Mo¿esz u¿yæ FixedLayout lub po prostu listy, 
    // ale dla czytelnoœci zostawmy te najpotrzebniejsze:
    public Entity Pistol;
    public Entity Shotgun;
    public Entity AK47;
    public Entity Hands; // Rêce jako prefab te¿ s¹ przydatne
}


