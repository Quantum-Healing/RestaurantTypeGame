using UnityEngine;

namespace KitchenEmpire
{
    /// <summary>
    /// Top-down orthographic camera. Pans on XZ, scroll wheel zooms.
    /// </summary>
    public class IsometricCamera : MonoBehaviour
    {
        [Header("Settings")]
        public float panSpeed = 10f;
        public float zoomSpeed = 2f;
        public float minZoom = 2f;
        public float maxZoom = 20f;
        public float smoothSpeed = 8f;
        public float cameraHeight = 30f;

        private Vector2 _targetXZ;   // camera look-at point in XZ
        private float _targetZoom;
        private Camera _cam;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;
            _targetZoom = _cam != null ? _cam.orthographicSize : 6f;
            _targetXZ = new Vector2(transform.position.x, transform.position.z);
        }

        void Start()
        {
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            ApplyPosition(true);
        }

        public void CenterOnGrid(GridManager grid)
        {
            float cx = grid.GridWidth  * 0.5f;
            float cz = grid.GridHeight * 0.5f;
            _targetXZ = new Vector2(cx, cz);
            // Fit the grid in view: use the larger dimension as the zoom base
            _targetZoom = Mathf.Max(grid.GridWidth, grid.GridHeight) * 0.6f;
            _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
            ApplyPosition(true);
        }

        void Update()
        {
            HandlePanning();
            HandleZoom();
            ApplyPosition(false);
        }

        private void HandlePanning()
        {
            Vector2 input = Vector2.zero;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    input.y += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  input.y -= 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  input.x -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1;

            if (input.sqrMagnitude > 0.01f)
                _targetXZ += input.normalized * panSpeed * Time.unscaledDeltaTime;
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

        private void ApplyPosition(bool instant)
        {
            Vector3 target = new Vector3(_targetXZ.x, cameraHeight, _targetXZ.y);

            if (instant)
            {
                transform.position = target;
                if (_cam != null) _cam.orthographicSize = _targetZoom;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, target,
                    Time.unscaledDeltaTime * smoothSpeed);
                if (_cam != null)
                    _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom,
                        Time.unscaledDeltaTime * smoothSpeed);
            }
        }

        /// <summary>
        /// Project screen point onto the ground plane (Y=0).
        /// </summary>
        public bool ScreenToGroundPoint(Vector3 screenPos, out Vector3 worldPos)
        {
            if (_cam == null) { worldPos = Vector3.zero; return false; }

            Ray ray = _cam.ScreenPointToRay(screenPos);
            var ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float dist))
            {
                worldPos = ray.GetPoint(dist);
                return true;
            }
            worldPos = Vector3.zero;
            return false;
        }
    }
}
