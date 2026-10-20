using Unity.Entities;
using Unity.NetCode;
using NetworkingLab.UI;

namespace NetworkingLab.NFE
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct NfeLivesClientSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (PlayerLivesUI.Instance == null) return;

            foreach (var (playerState, lives) in SystemAPI.Query<RefRO<NfePlayerState>, RefRO<NfePlayerLives>>().WithAll<NfePlayerTag, GhostOwnerIsLocal>())
            {
                PlayerLivesUI.Instance.SetLocalPlayer(playerState.ValueRO.PlayerNumber);
                PlayerLivesUI.Instance.SetLives(lives.ValueRO.Lives);
                break;
            }
        }
    }
}