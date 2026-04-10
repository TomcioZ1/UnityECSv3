using Unity.Entities;
using UnityEngine;


// 2. Authoring - to, co widzisz w Inspektorze Unity
public class SpawnWeaponAuthoring : MonoBehaviour
{
    // Tutaj w edytorze przeci¹gasz prefab broni (np. Prefab AK47)
    public GameObject WeaponToDrop;
}

// 3. Baker - most pomiêdzy œwiatem GameObjects a ECS
public class SpawnWeaponAuthoringBaker : Baker<SpawnWeaponAuthoring>
{
    public override void Bake(SpawnWeaponAuthoring authoring)
    {
        // Jeœli pole w Inspektorze jest puste, nie robimy nic
        if (authoring.WeaponToDrop == null) return;

        // Tworzymy encjê dla tego GameObjectu
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        // Konwertujemy GameObject na Entity i zapisujemy w komponencie
        AddComponent(entity, new DropWeapon
        {
            // TransformUsageFlags.Dynamic jest wa¿ne, jeœli broñ ma mieæ w³asn¹ pozycjê/fizykê
            DropWeaponPrefab = GetEntity(authoring.WeaponToDrop, TransformUsageFlags.Dynamic)
        });
    }
}