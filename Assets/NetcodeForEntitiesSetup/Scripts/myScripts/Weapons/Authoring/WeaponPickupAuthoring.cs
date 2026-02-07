using Unity.Entities;
using UnityEngine;
using Unity.NetCode;

// Definiujemy Enum, ¿eby ³atwo wybieraæ broñ w Inspektorze
public enum WeaponType : byte
{
    None = 0,
    Pistol = 1,
    Shotgun = 2,
    AK47 = 3,
    M4A1 = 4,
    MP5 = 5,
    Uzi = 6,
    Gun = 7,
    AWP = 8,
    PKM = 9,
    Grenade = 10
}

public class WeaponPickupAuthoring : MonoBehaviour
{
    public WeaponType Type;
    public int AmmoCount = 30;

    public class Baker : Baker<WeaponPickupAuthoring>
    {
        public override void Bake(WeaponPickupAuthoring authoring)
        {
            // U¿ywamy TransformUsageFlags.Dynamic, bo pickup mo¿e siê poruszaæ lub zostaæ zniszczony
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new WeaponPickup
            {
                WeaponId = (byte)authoring.Type,
                Ammo = authoring.AmmoCount
            });

            //AddComponent(entity, new GhostInstance());

        }
    }
}