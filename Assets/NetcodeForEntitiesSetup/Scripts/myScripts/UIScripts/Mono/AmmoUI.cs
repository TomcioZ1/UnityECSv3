using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using TMPro;
using System; // Wymagane dla Array.Find

public class AmmoUI : MonoBehaviour
{
    public TextMeshProUGUI ammoText;

    void Update()
    {
        World clientWorld = null;
        foreach (var world in World.All)
        {
            if (world.IsClient() && !world.IsThinClient())
            {
                clientWorld = world;
                break;
            }
        }

        if (clientWorld == null)
        {
            ammoText.text = "No World";
            return;
        }

        // Teraz kompilator wie, ¿e clientWorld to World, wiêc widzi EntityManager
        EntityManager entityManager = clientWorld.EntityManager;

        // 2. Szukamy lokalnego gracza
        var playerQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<PlayerInventory>(),
            ComponentType.ReadOnly<GhostOwnerIsLocal>()
        );

        // U¿ywamy TryGetSingleton, aby unikn¹æ b³êdów gdy gracz jeszcze siê nie zespawnowa³
        if (playerQuery.TryGetSingleton<PlayerInventory>(out var inventory))
        {
            Entity currentWeapon = inventory.CurrentWeaponEntity;

            if (currentWeapon != Entity.Null && entityManager.HasComponent<WeaponData>(currentWeapon))
            {
                var weaponData = entityManager.GetComponentData<WeaponData>(currentWeapon);

                // Wyœwietlanie amunicji
                ammoText.text = $"{weaponData.currentAmmo} / {weaponData.maxAmmo}";

                // Opcjonalnie: zmiana koloru przy prze³adowaniu
                ammoText.color = weaponData.isReloading ? Color.red : Color.white;
            }
            else
            {
                ammoText.text = "- / -";
            }
        }
    }
}