using Unity.Entities;
using UnityEngine;


// 2. Authoring - to, co widzisz w Inspektorze Unity
public class SpawnWeaponAuthoring : MonoBehaviour
{
    public int DropChance = 50; // Szansa na drop w procentach (0-100)
}

// 3. Baker - most pomiêdzy œwiatem GameObjects a ECS
public class SpawnWeaponAuthoringBaker : Baker<SpawnWeaponAuthoring>
{
    public override void Bake(SpawnWeaponAuthoring authoring)
    {
        // Tworzymy encjê dla tego GameObjectu
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        // Konwertujemy GameObject na Entity i zapisujemy w komponencie
        AddComponent(entity, new DropWeapon
        {
            // TransformUsageFlags.Dynamic jest wa¿ne, jeœli broñ ma mieæ w³asn¹ pozycjê/fizykê
            //DropWeaponPrefab = GetEntity(authoring.WeaponToDrop, TransformUsageFlags.Dynamic)
            DropChance = authoring.DropChance
        });
    }
}