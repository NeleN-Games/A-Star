using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// GridManager handles creating a chessboard-like grid, selecting random start/end tiles,
/// and finding a path between them using the A* algorithm.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")] [SerializeField]
    private int size; // Size of the grid (size x size)

    [SerializeField] private GameObject tilePrefab; // Tile prefab to instantiate
    [SerializeField] private Transform startPosition; // Starting position for the grid
    [SerializeField] private float xTileGap = 1f; // Gap between tiles in X axis
    [SerializeField] private float yTileGap = 1f; // Gap between tiles in Y axis

    private Tile[,] _grid; // 2D array to store all tiles
    private Tile _firstTile; // Start tile (green)
    private Tile _finalTile; // End tile (red)

    #region Unity Callbacks

    private void Start()
    {
        CreateGrid();
        GetRandomTile();
        FindPath(_firstTile, _finalTile, _grid, size);
    }

    public void Randomize()
    {
        ResetGridColors();
        GetRandomTile();
        FindPath(_firstTile, _finalTile, _grid, size);
    }

    #endregion

    #region Grid Creation

    /// <summary>
    /// Creates a chessboard-like grid of tiles.
    /// </summary>
    private void CreateGrid()
    {
        _grid = new Tile[size, size];
        Vector3 position = startPosition.position;

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                // Instantiate tile and add Tile component
                GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity);
                tileObj.transform.SetParent(transform);
                Tile tile = tileObj.AddComponent<Tile>();
                tile.Renderer = tileObj.GetComponent<Renderer>();
                tile.gridPosition = new Vector2Int(i, j);
                tile.isWalkable = true;

                // Set chessboard colors
                tile.Renderer.material.color = (i + j) % 2 == 0 ? Color.white : Color.black;

                _grid[i, j] = tile;

                // Move to next column
                position.x += xTileGap;
            }

            // Reset X and move to next row
            position.x = startPosition.position.x;
            position.y += yTileGap;
        }
    }

    /// <summary>
    /// Resets all tiles to original chessboard colors and clears path data.
    /// Keeps the final tile color red.
    /// </summary>
    private void ResetGridColors()
    {
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                Tile tile = _grid[i, j];

                // Skip final tile to keep it red
                //if (tile == _finalTile) continue;

                tile.Renderer.material.color = (i + j) % 2 == 0 ? Color.white : Color.black;

                // Reset A* data
                tile.gCost = 0;
                tile.hCost = 0;
                tile.parent = null;
            }
        }
    }

    /// <summary>
    /// Selects random start (green) and end (red) tiles.
    /// </summary>
    private void GetRandomTile()
    {
        int randomFirstIndex = Random.Range(0, size * size);
        int x1 = randomFirstIndex / size;
        int y1 = randomFirstIndex % size;
        _firstTile = _grid[x1, y1];
        _firstTile.Renderer.material.color = Color.green;

        int randomFinalIndex = Random.Range(0, size * size);
        while (randomFinalIndex == randomFirstIndex)
            randomFinalIndex = Random.Range(0, size * size);

        int x2 = randomFinalIndex / size;
        int y2 = randomFinalIndex % size;
        _finalTile = _grid[x2, y2];
        _finalTile.Renderer.material.color = Color.red;
    }

    #endregion

    #region A* Pathfinding

    /// <summary>
    /// Tile class for A* algorithm
    /// </summary>
    public class Tile : MonoBehaviour
    {
        public Vector2Int gridPosition;
        public bool isWalkable = true;

        public int gCost;
        public int hCost;
        public int fCost => gCost + hCost;
        public Tile parent;
        public Renderer Renderer;
    }

    /// <summary>
    /// Returns neighbors (up, down, left, right) of a tile.
    /// </summary>
    private List<Tile> GetNeighbors(Tile tile, Tile[,] grid, int size)
    {
        List<Tile> neighbors = new List<Tile>();
        int x = tile.gridPosition.x;
        int y = tile.gridPosition.y;
        int[] dx = { -1, 0, 1, 0 };
        int[] dy = { 0, 1, 0, -1 };

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];
            if (nx >= 0 && nx < size && ny >= 0 && ny < size)
                neighbors.Add(grid[nx, ny]);
        }

        return neighbors;
    }

    /// <summary>
    /// Retraces the path from end to start and colors the path tiles cyan.
    /// Keeps start green and end red.
    /// </summary>
    private List<Tile> RetracePath(Tile startTile, Tile endTile)
    {
        List<Tile> path = new List<Tile>();
        Tile current = endTile;

        // Build path by following parents
        while (current != startTile)
        {
            path.Add(current);
            current = current.parent;
        }

        path.Reverse(); // Make path from start to end

        // Color path tiles, excluding end tile
        foreach (Tile t in path)
        {
            if (t != endTile)
                t.Renderer.material.color = Color.cyan;
        }

        startTile.Renderer.material.color = Color.green;
        endTile.Renderer.material.color = Color.red;

        return path;
    }

    /// <summary>
    /// Implements A* pathfinding algorithm
    /// </summary>
    private List<Tile> FindPath(Tile startTile, Tile targetTile, Tile[,] grid, int size)
    {
        List<Tile> openSet = new List<Tile> { startTile };
        HashSet<Tile> closedSet = new HashSet<Tile>();

        while (openSet.Count > 0)
        {
            // Get tile with lowest fCost (and lowest hCost if tie)
            Tile currentTile = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentTile.fCost ||
                    (openSet[i].fCost == currentTile.fCost && openSet[i].hCost < currentTile.hCost))
                {
                    currentTile = openSet[i];
                }
            }

            openSet.Remove(currentTile);
            closedSet.Add(currentTile);

            // Path found
            if (currentTile == targetTile)
                return RetracePath(startTile, targetTile);

            // Check neighbors
            foreach (Tile neighbor in GetNeighbors(currentTile, grid, size))
            {
                if (!neighbor.isWalkable || closedSet.Contains(neighbor))
                    continue;

                int newGCost = currentTile.gCost + 1;
                if (newGCost < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newGCost;
                    neighbor.hCost = Mathf.Abs(neighbor.gridPosition.x - targetTile.gridPosition.x) +
                                     Mathf.Abs(neighbor.gridPosition.y - targetTile.gridPosition.y);
                    neighbor.parent = currentTile;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null; // No path found
    }

    #endregion
}