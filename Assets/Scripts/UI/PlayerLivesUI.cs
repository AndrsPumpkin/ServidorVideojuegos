using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NetworkingLab.UI
{
    /// <summary>Tracks player's lives, swaps hearts for skulls on hit and drives the game over state.</summary>
    public sealed class PlayerLivesUI : MonoBehaviour
    {
        [SerializeField] private Image[] hearts = new Image[3];
        [SerializeField] private Image[] heartsP1 = new Image[3];
        [SerializeField] private Image[] heartsP2 = new Image[3];
        [SerializeField] private Sprite deadSprite;
        [SerializeField] private Text winnerLabel;
        [SerializeField, Min(1)] private int livesPerPlayer = 3;
        [SerializeField, Min(1f)] private float restartDelay = 5f;
        [SerializeField] private bool autoReloadScene = false;

        private const int MaxPlayers = 2;

        private readonly int[] remaining = new int[MaxPlayers];
        private readonly int[] lostHearts = new int[MaxPlayers];
        private Sprite[] defaultHeartSprites;

        private int localPlayerNumber = 0;
        private int currentLives = 3;

        public static PlayerLivesUI Instance { get; private set; }

        public static bool GameOver { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Time.timeScale = 1f;
            GameOver = false;

            if (hearts != null)
            {
                defaultHeartSprites = new Sprite[hearts.Length];
                for (int i = 0; i < hearts.Length; i++)
                {
                    if (hearts[i] != null)
                    {
                        defaultHeartSprites[i] = hearts[i].sprite;
                    }
                }
            }

            for (int index = 0; index < MaxPlayers; index++)
            {
                remaining[index] = livesPerPlayer;
                lostHearts[index] = 0;
            }

            currentLives = livesPerPlayer;
            EnsureWinnerLabel();
            ResetHearts();
        }

        private void EnsureWinnerLabel()
        {
            if (winnerLabel != null)
            {
                // Ensure it's attached to canvas
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null && winnerLabel.transform.parent != canvas.transform)
                {
                    winnerLabel.transform.SetParent(canvas.transform, false);
                }
                return;
            }

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            Transform parent = parentCanvas != null ? parentCanvas.transform : transform;

            GameObject labelObject = new GameObject("WinnerLabel", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);

            Text text = labelObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 72;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = string.Empty;
            text.raycastTarget = false;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(900f, 160f);

            labelObject.SetActive(false);
            winnerLabel = text;
        }

        private void ResetHearts()
        {
            if (hearts != null)
            {
                for (int i = 0; i < hearts.Length; i++)
                {
                    if (hearts[i] != null)
                    {
                        hearts[i].enabled = true;
                        if (defaultHeartSprites != null && i < defaultHeartSprites.Length && defaultHeartSprites[i] != null)
                        {
                            hearts[i].sprite = defaultHeartSprites[i];
                        }
                    }
                }
            }

            for (int p = 0; p < MaxPlayers; p++)
            {
                Image[] playerHearts = p == 0 ? heartsP1 : heartsP2;
                if (playerHearts == null) continue;
                for (int i = 0; i < playerHearts.Length; i++)
                {
                    if (playerHearts[i] != null) playerHearts[i].enabled = true;
                }
            }
        }

        public void ResetRound()
        {
            GameOver = false;
            Time.timeScale = 1f;

            if (winnerLabel != null)
            {
                winnerLabel.text = string.Empty;
                winnerLabel.gameObject.SetActive(false);
            }

            for (int index = 0; index < MaxPlayers; index++)
            {
                remaining[index] = livesPerPlayer;
                lostHearts[index] = 0;
            }

            currentLives = livesPerPlayer;
            ResetHearts();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetLocalPlayer(int playerNumber)
        {
            localPlayerNumber = playerNumber;
            if (playerNumber >= 1 && playerNumber <= MaxPlayers)
            {
                int idx = playerNumber - 1;
                remaining[idx] = livesPerPlayer;
                currentLives = livesPerPlayer;
                lostHearts[idx] = 0;
            }
            else
            {
                currentLives = livesPerPlayer;
            }
            ResetHearts();
        }

        public void SetLives(int lives)
        {
            if (GameOver) return;
            int clamped = Mathf.Max(0, lives);
            currentLives = clamped;
            ApplyHeartsForLocal(clamped);
        }

        private void ApplyHeartsForLocal(int livesRemaining)
        {
            if (hearts != null && hearts.Length > 0)
            {
                int lost = hearts.Length - livesRemaining;
                if (lost < 0) lost = 0;
                for (int i = 0; i < hearts.Length; i++)
                {
                    if (hearts[i] == null) continue;
                    if (deadSprite != null && i < lost)
                    {
                        hearts[i].sprite = deadSprite;
                    }
                }
                return;
            }
        }

        public void RegisterHit(int playerNumber)
        {
            if (GameOver) return;

            if (hearts != null && hearts.Length > 0 && (localPlayerNumber == 0 || localPlayerNumber == playerNumber))
            {
                currentLives = Mathf.Max(0, currentLives - 1);
                ApplyHeartsForLocal(currentLives);
                return;
            }
        }

        public void ShowWinner(int winnerPlayerNumber)
        {
            if (GameOver) return;
            EndGame(winnerPlayerNumber);
        }

        private void EndGame(int winner)
        {
            GameOver = true;

            EnsureWinnerLabel();
            if (winnerLabel != null)
            {
                if (winner > 0)
                {
                    winnerLabel.text = $"Jugador {winner} ganó!";
                }
                else
                {
                    winnerLabel.text = "Empate!";
                }
                winnerLabel.gameObject.SetActive(true);
            }

            if (autoReloadScene)
            {
                Time.timeScale = 0f;
                StartCoroutine(RestartAfterDelay());
            }
        }

        private IEnumerator RestartAfterDelay()
        {
            yield return new WaitForSecondsRealtime(restartDelay);
            Time.timeScale = 1f;
            GameOver = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
