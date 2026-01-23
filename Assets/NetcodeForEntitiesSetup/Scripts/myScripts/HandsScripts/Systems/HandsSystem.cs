using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderFirst = false)]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
public partial struct HandsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>())
            return;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // Potrzebujemy dostêpu do komponentów transformacji broni
        var transformLookup = state.GetComponentLookup<LocalTransform>(true);

        foreach (var (activeHands, socket, entity) in
                 SystemAPI.Query<RefRW<ActiveHands>, HandsSocket>()
                 .WithEntityAccess())
        {
            Entity leftHand = activeHands.ValueRO.LeftHandEntity;
            Entity rightHand = activeHands.ValueRO.RightHandEntity;

            if ((leftHand != Entity.Null && leftHand.Index < 0) 
                || (rightHand != Entity.Null && rightHand.Index < 0))
                continue;

            if ((leftHand != activeHands.ValueRO.PrevLeftHand) 
                || (rightHand != activeHands.ValueRO.PrevRightHand))
            {
                if ((leftHand != Entity.Null && transformLookup.HasComponent(leftHand))
                    || (rightHand != Entity.Null && transformLookup.HasComponent(rightHand)))
                {
                    // 1. Pobieramy aktualny transform broni (z prefaba)
                    var leftHandTransform = transformLookup[leftHand];
                    var rightHandTransform = transformLookup[rightHand];

                    // 2. Zerujemy TYLKO pozycjê, zostawiaj¹c rotacjê i skalê z prefaba
                    leftHandTransform.Position = float3.zero;
                    rightHandTransform.Position = float3.zero;

                    // 3. Aplikujemy do ECB
                    ecb.AddComponent(leftHand, new Parent { Value = socket.LeftHandSocket });
                    ecb.SetComponent(leftHand, leftHandTransform);
                    ecb.AddComponent(rightHand, new Parent { Value = socket.RightHandSocket });
                    ecb.SetComponent(rightHand, rightHandTransform);

                    if (state.WorldUnmanaged.IsServer())
                    {
                        ecb.AppendToBuffer(entity, new LinkedEntityGroup { Value = leftHand });
                        ecb.AppendToBuffer(entity, new LinkedEntityGroup { Value = rightHand });
                    }
                }

                activeHands.ValueRW.PrevLeftHand = leftHand;
                activeHands.ValueRW.PrevRightHand = rightHand;
            }
        }
    }
}