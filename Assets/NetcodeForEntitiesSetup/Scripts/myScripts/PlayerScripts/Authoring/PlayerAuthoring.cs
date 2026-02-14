using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

public struct PlayerTag : IComponentData { }


[DisallowMultipleComponent]
public class PlayerAuthoring : MonoBehaviour
{
    public GameObject WeaponSocket;
    public GameObject LeftHandSocket;
    public GameObject RightHandSocket;
    public int InitialHealth = 100;

    class PlayerAuthoringBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            // KLUCZ: U¿ywamy TransformUsageFlags.Dynamic. 
            // Jest to wymagane, aby Physics Body mog³o poruszaæ encj¹.
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Rejestrujemy komponenty tagowe i logiczne
            AddComponent<PlayerTag>(entity);

            AddComponent(entity, new HealthComponent { HealthPoints = authoring.InitialHealth });
            AddComponent(entity, new HealthComponentHistory { HealthPoints = authoring.InitialHealth });

            AddComponent(entity, new PlayerInventory { });

            // Socket broni
            AddComponent(entity, new WeaponSocket
            {
                WeaponSocketEntity = GetEntity(authoring.WeaponSocket, TransformUsageFlags.Dynamic)
            });

            // Dane ataku
            AddComponent(entity, new HandAttackData
            {
                AttackDamage = 20
            });

            AddComponent(entity, new ActiveHands { });

            // Sochety r¹k
            AddComponent(entity, new HandsSocket
            {
                LeftHandSocket = GetEntity(authoring.LeftHandSocket, TransformUsageFlags.Dynamic),
                RightHandSocket = GetEntity(authoring.RightHandSocket, TransformUsageFlags.Dynamic)
            });

            // Skala
            AddComponent(entity, new BaseScale { Value = authoring.transform.localScale });


            // footprint
            AddComponent(entity, new PlayerFootprintState
            {
                LastSpawnPosition = float3.zero,
                LeftFoot = false,
                IsInitialized = false,
                distanceBetweenLegs = 0.07f,
                distanceBetweenSteps = 0.5f
            });


        }
    }
}
