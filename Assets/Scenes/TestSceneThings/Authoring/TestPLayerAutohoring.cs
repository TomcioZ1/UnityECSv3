using UnityEngine;
using Unity.Entities;

public class TestPlayerAuthoring : MonoBehaviour
{
    public float moveSpeed = 5f;

    public class TestPlayerAuthoringBaker : Baker<TestPlayerAuthoring>
    {
        public override void Bake(TestPlayerAuthoring authoring)
        {
            // Kluczowe: TransformUsageFlags.Dynamic pozwala fizyce przesuwaæ obiekt
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new TestPlayerTag());
            AddComponent(entity, new PlayerMoveInput
            {
                Speed = authoring.moveSpeed
            });

            // Upewnij siê, ¿e na GameObjectu s¹ komponenty:
            // 1. Physics Shape
            // 2. Physics Body (ustawiony na Dynamic!)
        }
    }
}