using UnityEngine;
using Fusion;
using TMPro;

namespace Com.MyCompany.MyGame
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Renderer playerRenderer;
        [SerializeField] private Camera playerCamera;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float gravity = 20f;
        [SerializeField] private float crouchHeight = 1.0f;
        [SerializeField] private float normalHeight = 2.0f;
        [SerializeField] private float crouchSpeed = 3f;

        [Networked] public NetworkString<_16> PlayerName { get; set; }
        [Networked] public int PlayerTeamIndex { get; set; }
        [Networked] private NetworkButtons _prevButtons { get; set; }
        
        // We sync vertical velocity for gravity/jumping
        [Networked] private float _verticalVelocity { get; set; }

        private CharacterController _cc;
        private TMP_Text _nameTagText;
        private ChangeDetector _changes;
        
        // Helper for smoothing crouch locally (optional)
        private float _targetHeight;

        public override void Spawned()
        {
            _cc = GetComponent<CharacterController>();
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            
            // Ensure visual consistency on spawn
            _targetHeight = normalHeight;
            if (_cc != null) _cc.height = normalHeight;

            CreateNameTag();

            if (Object.HasInputAuthority)
            {
                if (playerCamera != null) playerCamera.gameObject.SetActive(true);
                if (Camera.main != null && Camera.main != playerCamera) Camera.main.gameObject.SetActive(false);
                
                // Lock Cursor for FPS style control
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                var launcher = FindFirstObjectByType<FusionLauncher>();
                if (launcher != null)
                {
                    RPC_SetDetails(launcher.GetLocalPlayerName(), launcher.GetLocalPlayerTeamIndex());
                }
            }

            // Initial visual update
            UpdateVisuals();
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetworkInputData data))
            {
                // 1. Rotation
                // Apply rotation from input (Mouse X accumulated in Launcher)
                transform.rotation = Quaternion.Euler(0, data.rotationY, 0);

                // 2. Movement
                // Calculate move direction relative to player rotation
                Vector3 moveDir = transform.forward * data.direction.y + transform.right * data.direction.x;
                moveDir.Normalize();
                
                // Determine current speed (Crouch vs Walk)
                bool isCrouching = data.buttons.IsSet(InputButtons.Crouch);
                float currentSpeed = isCrouching ? crouchSpeed : moveSpeed;

                // Handle Crouching (Logic)
                if (isCrouching)
                {
                    _cc.height = crouchHeight;
                    _cc.center = Vector3.up * (crouchHeight * 0.5f);
                }
                else
                {
                    _cc.height = normalHeight;
                    _cc.center = Vector3.up * (normalHeight * 0.5f);
                }

                // 3. Gravity and Jumping
                if (_cc.isGrounded)
                {
                    // Reset vertical velocity when grounded (small downward force to keep grounded)
                    _verticalVelocity = -2f; 

                    // Check Jump
                    if (data.buttons.WasPressed(_prevButtons, InputButtons.Jump))
                    {
                        _verticalVelocity = jumpForce;
                    }
                }
                else
                {
                    // Apply Gravity
                    _verticalVelocity -= gravity * Runner.DeltaTime;
                }
                
                // 4. Final Move
                Vector3 finalMove = moveDir * currentSpeed;
                finalMove.y = _verticalVelocity;
                
                _cc.Move(finalMove * Runner.DeltaTime);
                
                // Update previous buttons for WasPressed checks
                _prevButtons = data.buttons;
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
                    case nameof(PlayerTeamIndex):
                        UpdateVisuals();
                        break;
                }
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SetDetails(string name, int teamIndex)
        {
            PlayerName = name;
            PlayerTeamIndex = teamIndex;
            
            // Teleport to team spawn point
            var launcher = FindFirstObjectByType<FusionLauncher>();
            if (launcher != null)
            {
                Vector3 spawnPos = launcher.GetSpawnPosition(teamIndex);
                // Use CharacterController to warp if enabled
                _cc.enabled = false;
                transform.position = spawnPos;
                _cc.enabled = true;
                
                Debug.Log($"Moved player {name} to Team {teamIndex + 1} spawn at {spawnPos}");
            }
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

            if (playerRenderer != null)
            {
                // Team 0 = Red, Team 1 = Blue
                Color teamColor = (PlayerTeamIndex == 0) ? Color.red : Color.blue;
                playerRenderer.material.color = teamColor;
            }
        }
    }
}