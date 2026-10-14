using NetworkingLab.PowerUp;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NetworkingLab.NGO
{
    /// <summary>NGO starter: identity and ownership are ready; students complete gameplay networking.</summary>
    public sealed class NgoPlayerNetwork : NetworkBehaviour
    {
        private static int nextPlayerNumber;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player 1";
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private TextMesh playerLabel;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(1f)] private float turnSpeed = 720f;
        [SerializeField] private Vector2 horizontalBounds = new(10.5f, 7.5f);

        private readonly NetworkVariable<int> playerNumber = new(0,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private InputActionMap actionMap;
        private InputAction moveAction;
        private InputAction powerUpAction;
        private Vector2 pendingServerInput;

        public int PlayerNumber => playerNumber.Value;
        public static void ResetSequence() => nextPlayerNumber = 0;

        public void Configure(InputActionAsset actions, string mapName, Renderer renderer, TextMesh label, float speed, Vector2 bounds)
        {
            inputActions = actions;
            actionMapName = mapName;
            bodyRenderer = renderer;
            playerLabel = label;
            moveSpeed = speed;
            horizontalBounds = bounds;
        }

        public override void OnNetworkSpawn()
        {
            playerNumber.OnValueChanged += OnPlayerNumberChanged;
            if (IsServer)
            {
                int assigned = ++nextPlayerNumber;
                playerNumber.Value = assigned;
                transform.position = SpawnPosition(assigned);
            }
            ApplyIdentity(playerNumber.Value);
            if (IsOwner && IsClient)
            {
                ResolveInput();
                powerUpAction.performed += OnPowerUpPerformed;
                actionMap.Enable();
            }
        }

        public override void OnNetworkDespawn()
        {
            playerNumber.OnValueChanged -= OnPlayerNumberChanged;
            if (powerUpAction != null) powerUpAction.performed -= OnPowerUpPerformed;
            actionMap?.Disable();
        }

        private void Update()
        {
            if (!IsOwner || !IsClient || moveAction == null) return;
            Vector2 move = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            ApplyFacing(move, Time.deltaTime);
            ApplyLocalPreview(move);
            // STUDENT TODO NGO-01: send the owning client's movement input to the server.
            // Suggested next step: call SubmitMoveRpc(move) and compare preview vs. authority.
            SubmitMoveRpc(move);
        }

        private void FixedUpdate()
        {

            Vector2 move = Vector2.ClampMagnitude(pendingServerInput, 1f);
            Vector3 next = transform.position +
                new Vector3(move.x, 0f, move.y) * (moveSpeed * Time.fixedDeltaTime);
            next.x = Mathf.Clamp(next.x, -horizontalBounds.x, horizontalBounds.x);
            next.z = Mathf.Clamp(next.z, -horizontalBounds.y, horizontalBounds.y);
            transform.position = next;
            ApplyFacing(move, Time.fixedDeltaTime);
            // STUDENT TODO NGO-02: validate pendingServerInput and move only on the server.
            // NetworkTransform is already attached and will replicate the authoritative result.
        }

        private void LateUpdate()
        {
            if (playerLabel != null)
            {
                playerLabel.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
            }
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitMoveRpc(Vector2 move)
        {
            pendingServerInput = Vector2.ClampMagnitude(move, 1f);
        }

        private void OnPowerUpPerformed(InputAction.CallbackContext context)
        {
            RequestPowerUpRpc();
            // STUDENT TODO NGO-03: replace this local preview with a Server RPC that validates
            // the request and a Client RPC that raises PowerUpEvents on all clients.
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestPowerUpRpc()
        {
            if (!IsSpawned || playerNumber.Value <= 0) return;
            AnnouncePowerUpRpc(playerNumber.Value);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AnnouncePowerUpRpc(int number)
        {
            PowerUpEvents.RaiseActivated($"Player {number}");
        }


        private void ApplyFacing(Vector2 move, float deltaTime)
        {
            if (move.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion target = Quaternion.LookRotation(new Vector3(move.x, 0f, move.y), Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * deltaTime);
        }

        private void ApplyLocalPreview(Vector2 move)
        {
            Vector3 next = transform.position + new Vector3(move.x, 0f, move.y) * (moveSpeed * Time.deltaTime);
            next.x = Mathf.Clamp(next.x, -horizontalBounds.x, horizontalBounds.x);
            next.z = Mathf.Clamp(next.z, -horizontalBounds.y, horizontalBounds.y);
            transform.position = next;
        }

        private void ResolveInput()
        {
            actionMap = inputActions.FindActionMap(actionMapName, true);
            moveAction = actionMap.FindAction("Move", true);
            powerUpAction = actionMap.FindAction("PowerUp", true);
        }

        private void OnPlayerNumberChanged(int previous, int current) => ApplyIdentity(current);

        private void ApplyIdentity(int number)
        {
            if (number <= 0) return;
            gameObject.name = $"NGO Player {number}";
            if (playerLabel != null) playerLabel.text = $"Player {number}";
            if (bodyRenderer != null) bodyRenderer.material.color = PlayerColor(number);
        }

        private static Vector3 SpawnPosition(int number)
        {
            Vector3[] positions = { new(-5f, .75f, 3f), new(5f, .75f, 3f), new(-5f, .75f, -3f), new(5f, .75f, -3f) };
            return positions[(Mathf.Max(1, number) - 1) % positions.Length];
        }

        private static Color PlayerColor(int number)
        {
            Color[] colors = { new(.1f, .55f, 1f), new(1f, .25f, .2f), new(.25f, .85f, .35f), new(1f, .72f, .12f) };
            return colors[(Mathf.Max(1, number) - 1) % colors.Length];
        }
    }
}
