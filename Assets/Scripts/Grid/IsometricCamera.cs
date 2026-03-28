using UnityEngine;

namespace KitchenEmpire
{
    /// <summary>
    /// Isometric camera controller. Fixed angle, with pan and zoom.
    /// PlateUp!-style top-down isometric view.
    /// </summary>
    public class IsometricCamera : MonoBehaviour
    {
        [Header("Settings")]
        public float panSpeed = 10f;
        public float zoomSpeed = 3f;
        public float minZoom = 3f;
        public float maxZoom = 15f;
        public float smoothSpeed = 8f;

        [Header("Isometric Angle")]
        public float cameraAngleX = 35f;
        public float cameraAngleY = 0f;
        public float cameraDistance = 10f;

        private Vector3 _targetPosition;
        private float _targetZoom;
        private Camera _cam;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;
            _targetZoom = cameraDistance;
        }

        void Start()
        {
            // Set isometric rotation
            transform.rotation = Quaternion.Euler(cameraAngleX, cameraAngleY, 0);
        }

        /// <summary>
        /// Center camera on the middle of the grid.
        /// </summary>
        public void CenterOnGrid(GridManager grid)
        {
            Vector3 center = grid.GridToWorld(grid.GridWidth / 2, grid.GridHeight / 2);
            _targetPosition = center;
            UpdateCameraPosition(true);
        }

        void Update()
        {
            HandlePanning();
            HandleZoom();
            UpdateCameraPosition(false);
        }

        private void HandlePanning()
        {
            Vector3 input = Vector3.zero;

            // WASD / Arrow keys
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.z += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.z -= 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1;

            // Middle mouse drag
            if (Input.GetMouseButton(2))
            {
                float dx = -Input.GetAxis("Mouse X");
                float dy = -Input.GetAxis("Mouse Y");
                input.x += dx * 3f;
                input.z += dy * 3f;
            }

            // Edge scrolling
            Vector3 mousePos = Input.mousePosition;
            float edgeThreshold = 20f;
            if (mousePos.x < edgeThreshold) input.x -= 1;
            if (mousePos.x > Screen.width - edgeThreshold) input.x += 1;
            if (mousePos.y < edgeThreshold) input.z -= 1;
            if (mousePos.y > Screen.height - edgeThreshold) input.z += 1;

            if (input.sqrMagnitude > 0)
            {
                // Adjust for camera rotation so panning feels correct
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
                Vector3 move = (right * input.x + forward * input.z) * panSpeed * Time.unscaledDeltaTime;
                _targetPosition += move;
            }
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _targetZoom -= scroll * zoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
            }
        }

        private void UpdateCameraPosition(bool instant)
        {
            cameraDistance = instant ? _targetZoom
                : Mathf.Lerp(cameraDistance, _targetZoom, Time.unscaledDeltaTime * smoothSpeed);

            Vector3 offset = -transform.forward * cameraDistance;
            Vector3 desiredPos = _targetPosition + offset;

            transform.position = instant ? desiredPos
                : Vector3.Lerp(transform.position, desiredPos, Time.unscaledDeltaTime * smoothSpeed);
        }

        /// <summary>
        /// Get world position on the ground plane from mouse position.
        /// </summary>
        public bool ScreenToGroundPoint(Vector3 screenPos, out Vector3 worldPos)
        {
            Ray ray = _cam.ScreenPointToRay(screenPos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                worldPos = ray.GetPoint(distance);
                return true;
            }
            worldPos = Vector3.zero;
            return false;
        }
    }
}
