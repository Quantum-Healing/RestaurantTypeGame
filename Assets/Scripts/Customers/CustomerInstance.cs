using UnityEngine;

namespace KitchenEmpire
{
    /// <summary>
    /// Runtime data and visual for a single customer.
    /// Customers enter through the door, walk to a table,
    /// wait for their order, eat, then leave.
    /// </summary>
    public class CustomerInstance : MonoBehaviour
    {
        public int CustomerId { get; private set; }
        public CustomerState State { get; private set; }
        public RecipeDefinition Order { get; private set; }
        public float PatiencePercent => Mathf.Clamp01(_patienceTimer / _maxPatience);
        public bool IsSatisfied { get; private set; }
        public Vector2Int? AssignedTable { get; private set; }

        [Header("Visual")]
        public Renderer bodyRenderer;
        public Renderer headRenderer;
        public SpriteRenderer orderBubble;
        public SpriteRenderer orderIcon;
        public Transform progressBarFill;

        private float _maxPatience;
        private float _patienceTimer;
        private float _eatTimer;
        private float _leaveTimer;
        private Color _bodyColor;

        // Movement
        private Vector3 _targetPos;
        private float _moveSpeed = 2f;

        // Bob animation
        private float _bobPhase;
        private Vector3 _basePos;

        private static readonly Color[] CustomerColors = {
            new Color(0.376f, 0.647f, 0.980f), // blue
            new Color(0.957f, 0.447f, 0.737f), // pink
            new Color(0.984f, 0.749f, 0.149f), // yellow
            new Color(0.290f, 0.851f, 0.502f), // green
            new Color(0.655f, 0.545f, 0.980f), // purple
            new Color(0.976f, 0.451f, 0.086f), // orange
            new Color(0.078f, 0.722f, 0.651f), // teal
        };

        public void Initialize(int id, RecipeDefinition order, float patience, Vector3 spawnPos)
        {
            CustomerId = id;
            Order = order;
            _maxPatience = patience;
            _patienceTimer = patience;
            State = CustomerState.Entering;
            IsSatisfied = false;

            _bodyColor = CustomerColors[id % CustomerColors.Length];
            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = _bodyColor;
            }

            transform.position = spawnPos;
            _basePos = spawnPos;
            _targetPos = spawnPos;
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);
        }

        public void AssignTable(Vector2Int tablePos, Vector3 worldPos)
        {
            AssignedTable = tablePos;
            _targetPos = worldPos;
            State = CustomerState.WalkingToTable;
        }

        public void UpdateCustomer(float dt)
        {
            _bobPhase += dt * 2f;

            switch (State)
            {
                case CustomerState.Entering:
                case CustomerState.WalkingToTable:
                    // Move toward target
                    Vector3 dir = _targetPos - transform.position;
                    if (dir.sqrMagnitude > 0.01f)
                    {
                        transform.position += dir.normalized * _moveSpeed * dt;
                    }
                    else
                    {
                        transform.position = _targetPos;
                        _basePos = _targetPos;
                        if (State == CustomerState.WalkingToTable)
                        {
                            State = CustomerState.WaitingForFood;
                        }
                    }
                    break;

                case CustomerState.WaitingForFood:
                    _patienceTimer -= dt;
                    UpdatePatienceVisual();

                    // Bob animation
                    float bob = Mathf.Sin(_bobPhase) * 0.05f;
                    transform.position = _basePos + new Vector3(0, bob, 0);

                    if (_patienceTimer <= 0f)
                    {
                        TimeOut();
                    }
                    break;

                case CustomerState.Eating:
                    _eatTimer -= dt;
                    if (_eatTimer <= 0f)
                    {
                        State = CustomerState.Leaving;
                        _leaveTimer = 1.5f;
                        IsSatisfied = true;
                    }
                    break;

                case CustomerState.Leaving:
                case CustomerState.LeavingAngry:
                    _leaveTimer -= dt;
                    // Move away (fade out)
                    transform.position += Vector3.up * dt * 0.5f;
                    break;
            }
        }

        public bool IsReadyToRemove()
        {
            return (State == CustomerState.Leaving || State == CustomerState.LeavingAngry) && _leaveTimer <= 0f;
        }

        /// <summary>
        /// Serve this customer their order.
        /// </summary>
        public void Serve()
        {
            if (State != CustomerState.WaitingForFood) return;

            IsSatisfied = true;
            State = CustomerState.Eating;
            _eatTimer = 5f;

            // Hide order bubble
            if (orderBubble != null) orderBubble.gameObject.SetActive(false);
        }

        private void TimeOut()
        {
            State = CustomerState.LeavingAngry;
            _leaveTimer = 1.5f;
            IsSatisfied = false;
            GameEvents.FireCustomerLeft(CustomerId, false);
        }

        private void UpdatePatienceVisual()
        {
            if (progressBarFill != null)
            {
                float pct = PatiencePercent;
                progressBarFill.localScale = new Vector3(pct, 1, 1);

                // Color: green -> yellow -> red
                Color barColor = pct > 0.5f
                    ? Color.Lerp(Color.yellow, Color.green, (pct - 0.5f) * 2f)
                    : Color.Lerp(Color.red, Color.yellow, pct * 2f);

                var barRenderer = progressBarFill.GetComponent<Renderer>();
                if (barRenderer != null) barRenderer.material.color = barColor;
            }
        }
    }
}
