using PlayerScripts;
using UnityEngine;
using UnityEngine.InputSystem;
using ToolScripts.Base;
using ToolScripts.UI;

namespace ToolScripts
{
    /// <summary>
    /// PDR (Paintless Dent Repair) light handler.
    /// Shines a specialized light to reveal dents through shadows.
    ///
    /// Features:
    /// - Shine light to reveal dents through shadows
    /// - Visual inspection aid for detecting dents
    /// - Directional light that player can aim
    /// - Reveals Dent_Repaired issues (shows shadow patterns)
    /// - Works with IExteriorPart targets
    /// - Toggle on/off with job button
    /// - Light projects grid/shadow pattern on surface
    /// </summary>
    public class PDRLightHandler : ToolHandlerBase
    {
        [Header("PDR Light Settings")]
        [SerializeField] private Light pdrLight;
        [SerializeField] private float lightRange = 5f;
        [SerializeField] private float lightIntensity = 2f;
        [SerializeField] private Color gridLineColor = new Color(0.8f, 0.8f, 0.8f);

        [Header("Pattern Projection")]
        [SerializeField] private Projector gridProjector; // Optional grid projector
        [SerializeField] private Texture2D gridPattern;

        private bool _lightIsOn = false;
        private ExteriorPart _targetExteriorPart;

        protected override void Awake()
        {
            base.Awake();
            toolType = Tool.PdrLight;
            toolName = "PDR Light";
            inspectionDuration = 0f; // Toggle light, no timer
            compatiblePartInterfaces = new string[] { "IExteriorPart" };

            // Setup light if not assigned
            if (pdrLight == null)
            {
                pdrLight = GetComponentInChildren<Light>();
            }

            // Setup projector if not assigned
            if (gridProjector == null)
            {
                gridProjector = GetComponentInChildren<Projector>();
            }
        }

        protected override VehiclePart GetTargetPart()
        {
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                return player.GetTargetPart();
            }
            return base.GetTargetPart();
        }

        protected override bool ValidateTarget()
        {
            if (currentTargetPart == null)
            {
                return false;
            }

            _targetExteriorPart = currentTargetPart as ExteriorPart;

            if (_targetExteriorPart == null)
            {
                _targetExteriorPart = currentTargetPart.GetComponent<ExteriorPart>();
            }

            return _targetExteriorPart != null;
        }

        protected override void BeginInspection()
        {
            // Toggle light on/off
            if (_lightIsOn)
            {
                TurnOffLight();
            }
            else
            {
                TurnOnLight();
            }
        }

        protected override void UpdateInspection()
        {
            // No timer - light stays on until toggled off
            // Update light direction to follow player aim
            if (_lightIsOn)
            {
                UpdateLightDirection();
            }
        }

        protected override ToolInspectionResult PerformInspection()
        {
            // PDR light doesn't produce automatic results
            // It's a visual aid for manual inspection
            return null;
        }

        private void TurnOnLight()
        {
            _lightIsOn = true;

            if (pdrLight != null)
            {
                pdrLight.enabled = true;
                pdrLight.range = lightRange;
                pdrLight.intensity = lightIntensity;
            }

            if (gridProjector != null && gridPattern != null)
            {
                gridProjector.enabled = true;
                gridProjector.material.SetTexture("_ShadowTex", gridPattern);
            }

            ToolUIManager.Instance?.ShowInstruction("PDR Light ON - Look for shadow patterns indicating dents");

            // Check for dent repair issues and log
            if (_targetExteriorPart != null && _targetExteriorPart.IsPartDentRepaired)
            {
                GameLogger.Log("[PDRLight] Dent repair detected on this part - look for shadow patterns");
            }

            GameLogger.Log("[PDRLight] Light turned on");
        }

        private void TurnOffLight()
        {
            _lightIsOn = false;

            if (pdrLight != null)
            {
                pdrLight.enabled = false;
            }

            if (gridProjector != null)
            {
                gridProjector.enabled = false;
            }

            ToolUIManager.Instance?.ClearInstruction();

            GameLogger.Log("[PDRLight] Light turned off");
        }

        private void UpdateLightDirection()
        {
            if (pdrLight == null) return;

            // Point light forward from tool position
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                PlayerCamera playerCamera = FindObjectOfType<PlayerCamera>();
                if (playerCamera != null)
                {
                    // Light direction matches camera forward direction
                    pdrLight.transform.forward = playerCamera.transform.forward;
                }
            }
        }

        protected override void CancelInspection()
        {
            // Turn off light when released
            if (_lightIsOn)
            {
                TurnOffLight();
            }

            base.CancelInspection();
        }

        protected override void OnTargetInvalid()
        {
            // Still allow light to turn on even if no target
            // PDR light can be used for general illumination
            if (!_lightIsOn)
            {
                TurnOnLight();
            }
            else
            {
                TurnOffLight();
            }
        }

        /// <summary>
        /// Gets the current light state
        /// </summary>
        public bool IsLightOn => _lightIsOn;

        /// <summary>
        /// Manually sets light state
        /// </summary>
        public void SetLightState(bool on)
        {
            if (on && !_lightIsOn)
            {
                TurnOnLight();
            }
            else if (!on && _lightIsOn)
            {
                TurnOffLight();
            }
        }
    }
}
