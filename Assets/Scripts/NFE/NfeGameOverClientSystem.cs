using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using NetworkingLab.UI;

namespace NetworkingLab.NFE
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct NfeGameOverClientSystem : ISystem
    {
        private bool gameOverShown;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            gameOverShown = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            if (gameOverShown) return;
            if (PlayerLivesUI.Instance == null) return;

            foreach (var (over, rpcEntity) in SystemAPI
                .Query<RefRO<NfeGameOverRpc>>()
                .WithAll<ReceiveRpcCommandRequest>()
                .WithEntityAccess())
            {
                if (!gameOverShown && PlayerLivesUI.Instance != null)
                {
                    PlayerLivesUI.Instance.ShowWinner(over.ValueRO.WinnerPlayerNumber);
                    gameOverShown = true;
                }
                break;
            }

            if (gameOverShown)
            {
                return;
            }

            if (SystemAPI.HasSingleton<NfeGameState>())
            {
                NfeGameState gs = SystemAPI.GetSingleton<NfeGameState>();
                if (gs.GameOver == 1 && PlayerLivesUI.Instance != null)
                {
                    PlayerLivesUI.Instance.ShowWinner(gs.WinnerPlayerNumber);
                    gameOverShown = true;
                }
            }
        }
    }
}
