using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

namespace Unity.Multiplayer.Center.NetcodeForEntitiesSetup
{
    public struct PlayerTag : IComponentData { }

    [DisallowMultipleComponent]
    public class CubeAuthoring : MonoBehaviour
    {
        public GameObject WeaponSocket;
        public GameObject LeftHandSocket;
        public GameObject RightHandSocket;

        class CubeBaker : Baker<CubeAuthoring>
        {
            public override void Bake(CubeAuthoring authoring)
            {
                // KLUCZ: U¿ywamy TransformUsageFlags.Dynamic. 
                // Jest to wymagane, aby Physics Body mog³o poruszaæ encj¹.
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // Rejestrujemy komponenty tagowe i logiczne
                AddComponent<PlayerTag>(entity);

                AddComponent(entity, new HealthComponent
                {
                    HealthPoints = 100
                });

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

                /* UWAGA: Nie musisz dodawaæ tutaj AddComponent<PhysicsVelocity> itp.
                   Unity Physics automatycznie doda potrzebne komponenty (PhysicsVelocity, PhysicsMass),
                   poniewa¿ masz na tym samym GameObject komponenty Physics Body i Physics Shape.
                   Bakerzy z pakietu Unity.Physics zrobi¹ to za Ciebie "pod spodem".
                */
            }
        }
    }
}