using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace NetworkingLab.NFE
{
    public struct NfePlayerTag : IComponentData
    {
    }

    public struct NfePlayerState : IComponentData
    {
        [GhostField] public int PlayerNumber;
        [GhostField(Quantization = 1000)] public float2 Position;
        [GhostField(Quantization = 1000)] public float RotationY;
    }

    public struct NfePlayerInput : IInputComponentData
    {
        public float2 Move;
        public InputEvent PowerUp;
    }

    public struct NfePlayerSpawner : IComponentData
    {
        public Entity PlayerPrefab;
    }
}
