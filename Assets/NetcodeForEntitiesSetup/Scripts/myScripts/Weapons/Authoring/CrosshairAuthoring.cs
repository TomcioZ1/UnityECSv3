/*using UnityEngine;
using Unity.Entities;
using Unity.NetCode;

public class CrosshairAuthoring : MonoBehaviour
{
    public RectTransform crosshairRect;
    private bool _entityCreated = false;

    void LateUpdate() // LateUpdate daje pewnoœæ, ¿e œwiaty Netcode ju¿ istniej¹
    {
        if (_entityCreated) return;

        if (World.All.Count > 0)
        {
            foreach (var world in World.All)
            {
                // Sprawdzamy czy to œwiat klienta i czy nie jest to œwiat "Baking"
                if (world.IsClient() && !world.Flags.HasFlag(WorldFlags.Editor))
                {
                    CreateEntityInWorld(world);
                    _entityCreated = true;
                    break;
                }
            }
        }
    }

    private void CreateEntityInWorld(World world)
    {
        var entityManager = world.EntityManager;
        var entity = entityManager.CreateEntity();

#if UNITY_EDITOR
        entityManager.SetName(entity, "UI_Crosshair_Entity_Client");
#endif

        // WA¯NE: Dodajemy Managed Component
        entityManager.AddComponentObject(entity, new CrosshairTag
        {
            Transform = crosshairRect
        });

        Debug.Log($"[ECS] Crosshair zainicjalizowany w œwiecie: {world.Name}");
    }
}
public class CrosshairTag : IComponentData
{
    public RectTransform Transform;
}*/