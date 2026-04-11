using Unity.Entities;
using UnityEngine;

// Klasa, któr¹ przeci¹gasz na GameObject w Inspektorze
public class GameTimerAuthoring : MonoBehaviour
{
    public float InitialTime = 100f;

    // Baker przekszta³ca dane z Inspektor na komponent ECS
    public class GameTimerBaker : Baker<GameTimerAuthoring>
    {
        public override void Bake(GameTimerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            // Dodajemy komponent z wartoœci¹ pocz¹tkow¹
            AddComponent(entity, new GameTimer
            {
                TimeRemaining = authoring.InitialTime
            });
        }
    }
}