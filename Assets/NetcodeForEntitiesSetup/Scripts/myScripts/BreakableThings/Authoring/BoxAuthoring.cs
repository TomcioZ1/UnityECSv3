using Unity.Entities;
using UnityEngine;

public class BoxAuthoring : MonoBehaviour
{
    public int StartHealth = 100;

    class BoxBaker : Baker<BoxAuthoring>
    {
        public override void Bake(BoxAuthoring authoring)
        {
            // WA¯NE: TransformUsageFlags.Dynamic jest kluczowe
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new HealthComponent
            {
                HealthPoints = authoring.StartHealth,
                MaxHealthPoints = authoring.StartHealth // Ustawiamy MaxHP na startowe HP
            });

            // Pobieramy lokaln¹ skalê GameObjectu - jeœli to 127, to zapisze 127
            AddComponent(entity, new BoxComponent
            {
                InitialScale = authoring.transform.localScale.x,
                InitialY = authoring.transform.position.y
            });
        }
    }
}