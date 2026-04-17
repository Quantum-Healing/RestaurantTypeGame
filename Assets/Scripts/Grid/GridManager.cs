using UnityEngine;
using System.Collections.Generic;

namespace KitchenEmpire
{
    /// <summary>
    /// Manages the isometric grid: tile creation, coordinate conversion,
    /// and grid expansion.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject tilePrefab;
        public GameObject wallPrefab;

        [Header("Materials")]
        public Material floorMaterialA;
        public Material floorMaterialB;
        public Material wallMaterial;

        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }

        private Dictionary<Vector2Int, GameObject> _tiles = new();
        private List<GameObject> _walls = new();

        public void InitializeGrid(int width, int height)
        {
            ClearGrid();
            GridWidth = width;
            GridHeight = height;
            BuildFloor();
            BuildWalls();
        }

        public void ExpandGrid(int newWidth, int newHeight)
        {
            if (newWidth <= GridWidth && newHeight <= GridHeight) return;
            InitializeGrid(
                Mathf.Max(GridWidth, newWidth),
                Mathf.Max(GridHeight, newHeight)
            );
            GameEvents.FireKitchenExpanded(GridWidth, GridHeight);
        }

        private void BuildFloor()
        {
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    Vector3 worldPos = GridToWorld(x, y);
                    GameObject tile;
                    if (tilePrefab != null)
                    {
                        tile = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                    }
                    else
                    {
                        // Fallback: create a flat quad
                        tile = CreateIsometricTile(worldPos);
                    }

                    tile.name = $"Tile_{x}_{y}";

                    // Checkerboard
                    var renderer = tile.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        bool isAlt = (x + y) % 2 == 0;
                        if (isAlt && floorMaterialA != null)
                            renderer.material = floorMaterialA;
                        else if (!isAlt && floorMaterialB != null)
                            renderer.material = floorMaterialB;
                    }

                    _tiles[new Vector2Int(x, y)] = tile;
                }
            }
        }

        private void BuildWalls()
        {
            float wallH = 1f;
            float wallT = 0.15f;

            // Back wall (far edge, z = GridHeight)
            for (int x = 0; x < GridWidth; x++)
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
                w.transform.parent = transform;
                w.transform.position = new Vector3(x + 0.5f, wallH * 0.5f, GridHeight + wallT * 0.5f);
                w.transform.localScale = new Vector3(1f, wallH, wallT);
                w.name = "Wall";
                ApplyWallMaterial(w);
                _walls.Add(w);
            }

            // Right wall (far edge, x = GridWidth)
            for (int z = 0; z < GridHeight; z++)
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
                w.transform.parent = transform;
                w.transform.position = new Vector3(GridWidth + wallT * 0.5f, wallH * 0.5f, z + 0.5f);
                w.transform.localScale = new Vector3(wallT, wallH, 1f);
                w.name = "Wall";
                ApplyWallMaterial(w);
                _walls.Add(w);
            }
        }

        private void ApplyWallMaterial(GameObject wall)
        {
            if (wallMaterial != null)
                wall.GetComponent<Renderer>().material = wallMaterial;
            else
                wall.GetComponent<Renderer>().material.color = new Color(0.72f, 0.67f, 0.56f);
        }

        private GameObject CreateIsometricTile(Vector3 position)
        {
            // Simple flat square tile, 1x1 in XZ plane
            var tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tile.name = "FloorTile";
            tile.transform.position = position;
            tile.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            tile.transform.localScale = new Vector3(1f, 1f, 1f);
            return tile;
        }

        private void ClearGrid()
        {
            foreach (var tile in _tiles.Values)
            {
                if (tile != null) Destroy(tile);
            }
            _tiles.Clear();

            foreach (var wall in _walls)
            {
                if (wall != null) Destroy(wall);
            }
            _walls.Clear();
        }

        // ===== COORDINATE CONVERSION =====

        /// <summary>
        /// Convert grid coordinates to world position. Grid cell (gx, gy)
        /// maps to world (gx + 0.5, 0, gy + 0.5) so tiles are centered on integers.
        /// </summary>
        public Vector3 GridToWorld(int gx, int gy)
        {
            return new Vector3(gx + 0.5f, 0f, gy + 0.5f);
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return GridToWorld(gridPos.x, gridPos.y);
        }

        /// <summary>
        /// Convert world position back to grid coordinates.
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x),
                Mathf.FloorToInt(worldPos.z));
        }

        public bool IsValidGridPos(Vector2Int pos)
        {
            return pos.x >= 0 && pos.y >= 0 && pos.x < GridWidth && pos.y < GridHeight;
        }

        public bool IsValidGridPos(int x, int y)
        {
            return x >= 0 && y >= 0 && x < GridWidth && y < GridHeight;
        }

        // ===== TILE HIGHLIGHTING =====

        public void HighlightTile(Vector2Int pos, Color color)
        {
            if (_tiles.TryGetValue(pos, out var tile))
            {
                var renderer = tile.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = color;
                }
            }
        }

        public void ClearHighlight(Vector2Int pos)
        {
            if (_tiles.TryGetValue(pos, out var tile))
            {
                var renderer = tile.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    bool isAlt = (pos.x + pos.y) % 2 == 0;
                    renderer.material = isAlt ? floorMaterialA : floorMaterialB;
                }
            }
        }
    }
}
