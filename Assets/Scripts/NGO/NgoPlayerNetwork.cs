using System.Collections;
using System.Collections.Generic;
using NetworkingLab.PowerUp;
using NetworkingLab.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NetworkingLab.NGO
{
    /// <summary>NGO player: identity, owner input, authoritative movement, PowerUp and the shared lives system.</summary>
    public sealed class NgoPlayerNetwork : NetworkBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color HitEmission = new Color(1f, 0f, 0f) * 3f;
        private const string EmissionKeyword = "_EMISSION";

        private static int nextPlayerNumber;
        private static readonly List<NgoPlayerNetwork> allPlayers = new();
        private static bool gameOver;
        private static int winnerNumber;
        private static Coroutine restartRoutine;

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player 1";
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private TextMesh playerLabel;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float turnSpeed = 720f;
        [SerializeField] private Vector2 horizontalBounds = new(10.5f, 7.5f);
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField, Min(0f)] private float fireCooldown = 0.3f;
        [SerializeField, Min(0f)] private float muzzleSideOffset = 0.6f;
        [SerializeField, Min(0f)] private float muzzleHeightOffset = 1.5f;
        [SerializeField, Min(0f)] private float flashDuration = 0.15f;
        [SerializeField, Min(1f)] private float roundRestartDelay = 4f;

        private readonly NetworkVariable<int> playerNumber = new NetworkVariable<int>(0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> lives = new NetworkVariable<int>(3,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> isDead = new NetworkVariable<bool>(false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private InputActionMap actionMap;
        private InputAction moveAction;
        private InputAction powerUpAction;
        private Vector2 pendingServerInput;
        private float nextFireTime;
        private Coroutine flashRoutine;
        private bool inputRegistered;

        public int PlayerNumber => playerNumber.Value;

        public static void ResetSequence()
        {
            nextPlayerNumber = 0;
            allPlayers.Clear();
            gameOver = false;
            winnerNumber = 0;
            restartRoutine = null;
        }

        public void Configure(InputActionAsset actions, string mapName, Renderer renderer, TextMesh label, float speed, Vector2 bounds, GameObject bullet = null)
        {
            inputActions = actions;
            actionMapName = mapName;
            bodyRenderer = renderer;
            playerLabel = label;
            moveSpeed = speed;
            horizontalBounds = bounds;
            bulletPrefab = bullet;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                int assigned = ++nextPlayerNumber;
                playerNumber.Value = assigned;
                transform.position = SpawnPosition(assigned);
                lives.Value = 3;
                isDead.Value = false;
                allPlayers.Add(this);
            }
            else
            {
                allPlayers.Add(this);
            }

            ApplyIdentity(playerNumber.Value);

            if (IsOwner)
            {
                if (PlayerLivesUI.Instance != null)
                {
                    PlayerLivesUI.Instance.SetLocalPlayer(playerNumber.Value);
                    PlayerLivesUI.Instance.SetLives(lives.Value);
                }

                RegisterInput();
            }

            playerNumber.OnValueChanged += OnPlayerNumberChanged;
            lives.OnValueChanged += OnLivesChanged;
        }

        public override void OnNetworkDespawn()
        {
            playerNumber.OnValueChanged -= OnPlayerNumberChanged;
            lives.OnValueChanged -= OnLivesChanged;
            allPlayers.Remove(this);

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
                RestoreEmission();
            }

            if (IsOwner)
            {
                UnregisterInput();
            }
        }

        private void RegisterInput()
        {
            if (!Application.isPlaying || inputRegistered || inputActions == null || string.IsNullOrWhiteSpace(actionMapName))
            {
                return;
            }

            actionMap = inputActions.FindActionMap(actionMapName, true);
            moveAction = actionMap.FindAction("Move", true);
            powerUpAction = actionMap.FindAction("PowerUp", true);
            powerUpAction.performed += OnPowerUpPerformed;
            actionMap.Enable();
            inputRegistered = true;
        }

        private void UnregisterInput()
        {
            if (!inputRegistered)
            {
                return;
            }

            powerUpAction.performed -= OnPowerUpPerformed;
            actionMap.Disable();
            inputRegistered = false;
        }

        private void Update()
        {
            if (!IsOwner || !IsSpawned)
            {
                return;
            }

            if (gameOver || isDead.Value || lives.Value <= 0)
            {
                return;
            }

            Vector2 move = moveAction.ReadValue<Vector2>();
            ApplyLocalPreview(move);
            ApplyFacing(move, Time.deltaTime);
            SubmitMoveRpc(move);
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitMoveRpc(Vector2 move)
        {
            if (!IsServer || gameOver || isDead.Value || lives.Value <= 0)
            {
                return;
            }

            pendingServerInput = Vector2.ClampMagnitude(move, 1f);
        }

        private void FixedUpdate()
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            if (gameOver || isDead.Value || lives.Value <= 0)
            {
                return;
            }

            Vector2 move = Vector2.ClampMagnitude(pendingServerInput, 1f);
            Vector3 next = transform.position +
                new Vector3(move.x, 0f, move.y) * (moveSpeed * Time.fixedDeltaTime);
            next.x = Mathf.Clamp(next.x, -horizontalBounds.x, horizontalBounds.x);
            next.z = Mathf.Clamp(next.z, -horizontalBounds.y, horizontalBounds.y);
            transform.position = next;
            ApplyFacing(move, Time.fixedDeltaTime);
        }

        private void OnPowerUpPerformed(InputAction.CallbackContext context)
        {
            RequestPowerUpRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestPowerUpRpc()
        {
            if (!IsServer) return;
            if (gameOver || isDead.Value) return;
            if (playerNumber.Value <= 0) return;
            AnnouncePowerUpRpc(playerNumber.Value);
            TryFire();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AnnouncePowerUpRpc(int number)
        {
            PowerUpEvents.RaiseActivated($"Player {number}");
        }

        private void TryFire()
        {
            if (bulletPrefab == null || Time.time < nextFireTime) return;
            nextFireTime = Time.time + fireCooldown;

            Vector3 spawnPosition = transform.position
                + transform.right * muzzleSideOffset
                + Vector3.up * muzzleHeightOffset;
            Quaternion bulletRotation = Quaternion.LookRotation(transform.right, Vector3.up);
            GameObject instance = Instantiate(bulletPrefab, spawnPosition, bulletRotation);
            if (!instance.TryGetComponent(out NetworkObject networkObject))
            {
                Destroy(instance);
                return;
            }

            networkObject.Spawn(true);
            if (instance.TryGetComponent(out NgoBulletProjectile projectile))
            {
                projectile.Configure(playerNumber.Value);
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        internal void FlashHitRpc()
        {
            if (flashRoutine != null) return;
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            List<(Material material, Color color, bool emission)> saved = new();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (playerLabel != null && renderer.gameObject == playerLabel.gameObject) continue;
                Material material = renderer.material;
                saved.Add((material, material.GetColor(EmissionColorId), material.IsKeywordEnabled(EmissionKeyword)));
                material.EnableKeyword(EmissionKeyword);
                material.SetColor(EmissionColorId, HitEmission);
            }

            yield return new WaitForSeconds(flashDuration);

            foreach ((Material material, Color color, bool emission) in saved)
            {
                material.SetColor(EmissionColorId, color);
                if (!emission) material.DisableKeyword(EmissionKeyword);
            }

            flashRoutine = null;
        }

        private void RestoreEmission()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (playerLabel != null && renderer.gameObject == playerLabel.gameObject) continue;
                renderer.material.SetColor(EmissionColorId, Color.black);
                renderer.material.DisableKeyword(EmissionKeyword);
            }
        }

        private void OnPlayerNumberChanged(int oldValue, int newValue)
        {
            ApplyIdentity(newValue);
            if (IsOwner && PlayerLivesUI.Instance != null)
            {
                PlayerLivesUI.Instance.SetLocalPlayer(newValue);
                PlayerLivesUI.Instance.SetLives(lives.Value);
            }
        }

        private void OnLivesChanged(int oldValue, int newValue)
        {
            if (IsOwner && PlayerLivesUI.Instance != null)
            {
                PlayerLivesUI.Instance.SetLives(newValue);
            }
        }

        public void ServerRegisterHit()
        {
            if (!IsServer) return;
            if (gameOver || isDead.Value) return;

            int newLives = lives.Value - 1;
            if (newLives < 0) newLives = 0;
            lives.Value = newLives;
            FlashHitRpc();

            if (newLives <= 0)
            {
                isDead.Value = true;
            }

            int alive = 0;
            int lastAlive = 0;
            for (int index = 0; index < allPlayers.Count; index++)
            {
                NgoPlayerNetwork player = allPlayers[index];
                if (player == null || player.isDead.Value || player.lives.Value <= 0) continue;
                alive++;
                lastAlive = player.playerNumber.Value;
            }

            if (alive <= 1)
            {
                gameOver = true;
                winnerNumber = alive == 1 ? lastAlive : 0;
                GameOverRpc(winnerNumber);
                if (restartRoutine == null)
                {
                    restartRoutine = StartCoroutine(ServerRestartRoundRoutine());
                }
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void GameOverRpc(int winner)
        {
            gameOver = true;
            winnerNumber = winner;
            if (PlayerLivesUI.Instance != null)
            {
                PlayerLivesUI.Instance.ShowWinner(winner);
            }
        }

        private IEnumerator ServerRestartRoundRoutine()
        {
            yield return new WaitForSeconds(roundRestartDelay);

            // 1. Despawn active bullets on server
            NgoBulletProjectile[] bullets = FindObjectsByType<NgoBulletProjectile>(FindObjectsInactive.Exclude);
            for (int i = 0; i < bullets.Length; i++)
            {
                if (bullets[i] != null && bullets[i].NetworkObject != null && bullets[i].NetworkObject.IsSpawned)
                {
                    bullets[i].NetworkObject.Despawn(true);
                }
            }

            // 2. Restore all players
            for (int index = 0; index < allPlayers.Count; index++)
            {
                NgoPlayerNetwork player = allPlayers[index];
                if (player == null || !player.IsSpawned) continue;

                player.lives.Value = 3;
                player.isDead.Value = false;
                player.transform.position = SpawnPosition(player.playerNumber.Value);
                player.transform.rotation = Quaternion.identity;
                player.pendingServerInput = Vector2.zero;
                player.nextFireTime = 0f;
            }

            // 3. Reset game-over flags
            gameOver = false;
            winnerNumber = 0;
            restartRoutine = null;

            // 4. Notify clients and host to reset UI
            RoundRestartedRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void RoundRestartedRpc()
        {
            gameOver = false;
            winnerNumber = 0;

            if (PlayerLivesUI.Instance != null)
            {
                PlayerLivesUI.Instance.ResetRound();
                PlayerLivesUI.Instance.SetLocalPlayer(playerNumber.Value);
                PlayerLivesUI.Instance.SetLives(lives.Value);
            }
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

        private void ApplyIdentity(int number)
        {
            if (number <= 0) return;
            gameObject.name = $"NGO Player {number}";
            if (playerLabel != null) playerLabel.text = $"Player {number}";
            Color color = PlayerColor(number);
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (playerLabel != null && renderer.gameObject == playerLabel.gameObject) continue;
                renderer.material.color = color;
            }
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
