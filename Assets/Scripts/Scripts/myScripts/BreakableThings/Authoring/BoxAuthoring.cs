using Unity.Entities;
using UnityEngine;

public class BoxAuthoring : MonoBehaviour
{
    public int StartHealth = 100;

    class BoxBaker : Baker<BoxAuthoring>
    {
        public override void Bake(BoxAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new HealthComponent
            {
                HealthPoints = authoring.StartHealth,
                MaxHealthPoints = authoring.StartHealth
            });

            float detectedHeight = 1.0f;
            float detectedCenterY = 0.0f; // NOWE

            var renderer = authoring.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = authoring.GetComponentInChildren<MeshRenderer>();

            if (renderer != null)
            {
                detectedHeight = renderer.localBounds.size.y;
                detectedCenterY = renderer.localBounds.center.y; // Pobieramy offset œrodka
            }

            AddComponent(entity, new BoxComponent
            {
                isDestoryed = false,
                InitialScale = authoring.transform.localScale.y,
                InitialY = authoring.transform.position.y,
                MeshHeight = detectedHeight,
                CenterOffset = detectedCenterY // Zapisujemy do komponentu
            });
            AddComponent(entity, new GhostState
            {
                IsDestroyed = false
            });


        }
    }
}