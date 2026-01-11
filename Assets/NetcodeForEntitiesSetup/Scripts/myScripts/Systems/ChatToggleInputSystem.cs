/*using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ChatToggleInputSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        bool enterPressed = keyboard != null && keyboard.enterKey.wasPressedThisFrame;
#else
        bool enterPressed = UnityEngine.Input.GetKeyDown(KeyCode.Return);
#endif

        if (!enterPressed) return;

        // Tworzymy encjê ToggleChatUI w ClientWorld
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        var e = ecb.CreateEntity();
        ecb.AddComponent<ToggleChatUI>(e);
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
*/