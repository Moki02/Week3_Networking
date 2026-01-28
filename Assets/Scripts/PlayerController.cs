using UnityEngine;
using Fusion;
using TMPro;

namespace Com.MyCompany.MyGame
{
    public class PlayerController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Renderer playerRenderer;
        [SerializeField] private Camera playerCamera;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Visuals")]
        [SerializeField]
        private Color[] availableColors = {
            Color.red, Color.blue, Color.green, Color.yellow,
            Color.cyan, Color.magenta, Color.white, Color.black
        };

        [Networked] public NetworkString<_16> PlayerName { get; set; }
        [Networked] public int PlayerColorIndex { get; set; }

        private TMP_Text _nameTagText;
        private ChangeDetector _changes;

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            CreateNameTag();

            if (Object.HasInputAuthority)
            {
                if (playerCamera != null) playerCamera.gameObject.SetActive(true);
                if (Camera.main != null && Camera.main != playerCamera) Camera.main.gameObject.SetActive(false);

                var launcher = FindFirstObjectByType<FusionLauncher>();
                if (launcher != null)
                {
                    RPC_SetDetails(launcher.GetLocalPlayerName(), launcher.GetLocalPlayerColorIndex());
                }
            }

            // Initial visual update
            UpdateVisuals();
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetworkInputData data))
            {
                Vector3 move = new Vector3(data.direction.x, 0, data.direction.y).normalized;
                transform.position += move * moveSpeed * Runner.DeltaTime;
            }
        }

        public override void Render()
        {
            // Only update visuals if the networked properties changed
            foreach (var change in _changes.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(PlayerName):
                    case nameof(PlayerColorIndex):
                        UpdateVisuals();
                        break;
                }
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SetDetails(string name, int colorIndex)
        {
            PlayerName = name;
            PlayerColorIndex = colorIndex;
        }

        private void CreateNameTag()
        {
            GameObject textObj = new GameObject("NameTag");
            textObj.transform.SetParent(transform);
            textObj.transform.localPosition = Vector3.up * 2.2f;

            _nameTagText = textObj.AddComponent<TextMeshPro>();
            _nameTagText.alignment = TextAlignmentOptions.Center;
            _nameTagText.fontSize = 3;
            _nameTagText.rectTransform.sizeDelta = new Vector2(5, 1);
        }

        private void UpdateVisuals()
        {
            if (_nameTagText != null) _nameTagText.text = PlayerName.ToString();

            if (playerRenderer != null && PlayerColorIndex >= 0 && PlayerColorIndex < availableColors.Length)
            {
                playerRenderer.material.color = availableColors[PlayerColorIndex];
            }
        }
    }
}