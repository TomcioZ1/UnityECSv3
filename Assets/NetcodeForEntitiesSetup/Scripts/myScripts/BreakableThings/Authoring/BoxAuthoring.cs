using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class BoxAuthoring : MonoBehaviour
{
    [Header("Ustawienia Skrzynki")]
    public int StartHealth = 100;

    // Klasa Baker przekszta³ca dane z GameObjectu na dane ECS
    class BoxBaker : Baker<BoxAuthoring>
    {
        public override void Bake(BoxAuthoring authoring)
        {
            // TransformUsageFlags.Dynamic jest konieczne, bo bêdziemy zmieniaæ skalê i pozycjê w czasie gry
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            // 1. Dodajemy HealthComponent (synchronizowany przez sieæ)
            AddComponent(entity, new HealthComponent
            {
                HealthPoints = authoring.StartHealth,
                // Mo¿esz tu te¿ dodaæ MaxHealth, jeœli chcesz go u¿ywaæ w systemie wizualnym
            });

            // 2. Dodajemy BoxComponent (dane statyczne do obliczeñ wizualnych)
            // Pobieramy aktualn¹ skalê i wysokoœæ prosto z transformu w edytorze
            AddComponent(entity, new BoxComponent
            {
                InitialScale = authoring.transform.localScale.x,
                InitialY = authoring.transform.position.y
            });

            // UWAGA: Upewnij siê, ¿e Twój prefab ma te¿ komponenty LocalTransform 
            // i GhostOwner (jeœli jest spawnowany dynamicznie). 
            // Ghost Authoring Component doda resztê automatycznie.
        }
    }
}