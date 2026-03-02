using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct ReloadUISystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (ReloadUIController.Instance == null) return;

        float currentTime = (float)SystemAPI.Time.ElapsedTime;
        bool anyActiveReload = false;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (request, entity) in SystemAPI.Query<RefRO<ReloadUIRequest>>().WithEntityAccess())
        {
            float elapsed = currentTime - request.ValueRO.StartTime;
            float progress = math.saturate(elapsed / request.ValueRO.Duration);

            if (progress < 1.0f)
            {
                ReloadUIController.Instance.UpdateProgressFromData(progress, true);
                anyActiveReload = true;
            }
            else
            {
                // Prze³adowanie wizualne zakoñczone - usuwamy encjê pomocnicz¹
                ecb.DestroyEntity(entity);
            }
        }

        if (!anyActiveReload)
        {
            ReloadUIController.Instance.UpdateProgressFromData(0, false);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}