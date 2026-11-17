using UnityEngine;

/// <summary>
/// Shared navigation for steering-mode enemies. A single Dijkstra pass from the
/// player gives every zombie a shortest-route direction at once, including around
/// whole buildings that the 1.4 m reactive probes can never see past. The grid is
/// built from physics queries at runtime, sliced over frames, and follows the
/// player; it needs no authored NavMesh and no scene/prefab changes.
///
/// Local steering (Enemy.ChooseWalkDirection) still owns the final step: doors,
/// props and other moving colliders are handled there, while this field supplies
/// the global "which way around the block" decision.
/// </summary>
public sealed class EnemyFlowField : MonoBehaviour
{
    private const float CellSize = 1f;
    private const int GridWidth = 161;
    private const int GridHeight = 161;
    private const int CellCount = GridWidth * GridHeight;
    private const int BuildBudget = 1024;
    private const float RepathDistance = CellSize * .75f;
    private const float MaxSlopeStep = .45f;

    private static readonly int[] NeighborX = { -1, 0, 1, -1, 1, -1, 0, 1 };
    private static readonly int[] NeighborZ = { -1, -1, -1, 0, 0, 1, 1, 1 };
    private static readonly bool[] NeighborDiagonal = { true, false, true, false, false, true, true, true };
    private static readonly float[] NeighborCost =
    {
        CellSize * 1.41421f, CellSize, CellSize * 1.41421f, CellSize,
        CellSize, CellSize * 1.41421f, CellSize * 1.41421f, CellSize
    };

    public static EnemyFlowField Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    public static EnemyFlowField Ensure()
    {
        if (Instance != null) return Instance;
        return new GameObject("[Enemy Flow Field]").AddComponent<EnemyFlowField>();
    }

    private readonly byte[] walkable = new byte[CellCount];
    private readonly float[] costs = new float[CellCount];
    private readonly int[] nextCell = new int[CellCount];
    private readonly float[] floors = new float[CellCount];
    private readonly RaycastHit[] floorHits = new RaycastHit[16];
    private readonly Collider[] blockers = new Collider[16];

    private struct HeapNode
    {
        public float cost;
        public int index;
    }

    // Extra capacity covers repeated improvements of the same cell during Dijkstra.
    private readonly HeapNode[] heap = new HeapNode[CellCount * 4];
    private int heapCount;

    private Transform player;
    private Vector3 gridCenter;
    private Vector3 origin;
    private Vector3 lastFieldPosition;
    private float nextFieldUpdate;
    private float lastQueryAt = -100f;
    private bool initialized, building, fieldReady;
    private int buildCursor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        for (int i = 0; i < CellCount; i++) nextCell[i] = -1;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        // Only steering-mode chases pay for the field; NavMesh scenes never query it.
        if (Time.time - lastQueryAt > 2f) return;
        if (building)
        {
            BuildStep();
            return;
        }
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            var seeker = GameManager.SessionPlayer;
            player = seeker != null ? seeker.transform : GameObject.FindWithTag("Player")?.transform;
        }
        if (player == null) { fieldReady = false; return; }

        Vector3 position = player.position;
        if (!initialized)
        {
            BeginBuild(position);
            return;
        }

        // The play area is larger than the grid: re-centre it when the player walks
        // toward an edge so distant zombies are covered too.
        float limitX = GridWidth * CellSize * .3f;
        float limitZ = GridHeight * CellSize * .3f;
        if (Mathf.Abs(position.x - gridCenter.x) > limitX ||
            Mathf.Abs(position.z - gridCenter.z) > limitZ)
        {
            BeginBuild(position);
            return;
        }

        // Idle players do not need a fresh field; chasing zombies keep it warm.
        if (Time.time - lastQueryAt > 2f) return;
        bool moved = (position.x - lastFieldPosition.x) * (position.x - lastFieldPosition.x) +
                     (position.z - lastFieldPosition.z) * (position.z - lastFieldPosition.z)
                     >= RepathDistance * RepathDistance;
        if (fieldReady && !moved && Time.time < nextFieldUpdate) return;
        RecomputeField(position);
    }

    private void BeginBuild(Vector3 center)
    {
        gridCenter = new Vector3(center.x, center.y, center.z);
        origin = new Vector3(center.x - (GridWidth - 1) * CellSize * .5f, center.y,
            center.z - (GridHeight - 1) * CellSize * .5f);
        initialized = true;
        building = true;
        fieldReady = false;
        buildCursor = 0;
        System.Array.Clear(walkable, 0, walkable.Length);
    }

    private void BuildStep()
    {
        int end = Mathf.Min(buildCursor + BuildBudget, CellCount);
        for (; buildCursor < end; buildCursor++) SampleCell(buildCursor);
        if (buildCursor < CellCount) return;
        building = false;
        if (player != null) RecomputeField(player.position);
    }

    private void SampleCell(int index)
    {
        int x = index % GridWidth;
        int z = index / GridWidth;
        float px = origin.x + x * CellSize;
        float pz = origin.z + z * CellSize;

        // Sample the floor on the player's level, so multi-floor maps do not send
        // enemies through the ceiling of the room they are actually in.
        Vector3 rayOrigin = new Vector3(px, origin.y + 3f, pz);
        int count = Physics.RaycastNonAlloc(rayOrigin, Vector3.down, floorHits, 9f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float floorY = float.NaN;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var hit = floorHits[i];
            if (hit.normal.y < .65f || hit.collider.isTrigger) continue;
            if (hit.collider.GetComponentInParent<IDamageable>() != null ||
                hit.collider.GetComponentInParent<PlayerMovement>() != null) continue;
            if (Mathf.Abs(hit.point.y - origin.y) > 2f || hit.distance >= nearest) continue;
            nearest = hit.distance;
            floorY = hit.point.y;
        }
        if (float.IsNaN(floorY))
        {
            floors[index] = origin.y;
            return; // no support here: not walkable
        }
        floors[index] = floorY;

        // A full enemy capsule must fit without touching scenery. Low curbs are
        // steppable and actors do not block the route (separation handles crowds).
        var bottom = new Vector3(px, floorY + .5f, pz);
        var top = new Vector3(px, floorY + 1.5f, pz);
        int blockersCount = Physics.OverlapCapsuleNonAlloc(bottom, top, .48f, blockers,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < blockersCount; i++)
        {
            var collider = blockers[i];
            if (collider.isTrigger) continue;
            if (collider.GetComponentInParent<IDamageable>() != null ||
                collider.GetComponentInParent<PlayerMovement>() != null) continue;
            if (collider.bounds.max.y <= floorY + .2f) continue;
            return; // blocked: walkable stays 0
        }
        walkable[index] = 1;
    }

    private void RecomputeField(Vector3 goal)
    {
        if (!TryCell(goal, out int gx, out int gz)) return;
        int start = NearestWalkable(gz * GridWidth + gx, 3);
        if (start < 0) return;

        for (int i = 0; i < CellCount; i++)
        {
            costs[i] = float.PositiveInfinity;
            nextCell[i] = -1;
        }
        heapCount = 0;
        costs[start] = 0f;
        HeapPush(0f, start);

        while (HeapPop(out float currentCost, out int index))
        {
            // A stale heap entry after a better route was found for this cell.
            if (currentCost > costs[index] + .0001f) continue;
            int cx = index % GridWidth;
            int cz = index / GridWidth;
            for (int n = 0; n < 8; n++)
            {
                int nx = cx + NeighborX[n];
                int nz = cz + NeighborZ[n];
                if (nx < 0 || nx >= GridWidth || nz < 0 || nz >= GridHeight) continue;
                int neighbor = nz * GridWidth + nx;
                if (walkable[neighbor] == 0) continue;
                // Do not cut diagonally across a blocked corner.
                if (NeighborDiagonal[n] &&
                    (walkable[cz * GridWidth + nx] == 0 || walkable[nz * GridWidth + cx] == 0)) continue;
                if (Mathf.Abs(floors[neighbor] - floors[index]) > MaxSlopeStep) continue;
                float candidate = currentCost + NeighborCost[n];
                if (candidate >= costs[neighbor]) continue;
                costs[neighbor] = candidate;
                nextCell[neighbor] = index;
                HeapPush(candidate, neighbor);
            }
        }

        lastFieldPosition = goal;
        nextFieldUpdate = Time.time + .3f;
        fieldReady = true;
    }

    private void HeapPush(float cost, int index)
    {
        if (heapCount >= heap.Length) return;
        int i = heapCount++;
        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            if (heap[parent].cost <= cost) break;
            heap[i] = heap[parent];
            i = parent;
        }
        heap[i].cost = cost;
        heap[i].index = index;
    }

    private bool HeapPop(out float cost, out int index)
    {
        if (heapCount == 0)
        {
            cost = 0f;
            index = -1;
            return false;
        }
        HeapNode root = heap[0];
        cost = root.cost;
        index = root.index;
        heapCount--;
        if (heapCount > 0)
        {
            HeapNode last = heap[heapCount];
            int i = 0;
            while (true)
            {
                int left = i * 2 + 1;
                if (left >= heapCount) break;
                int right = left + 1;
                int child = right < heapCount && heap[right].cost < heap[left].cost ? right : left;
                if (heap[child].cost >= last.cost) break;
                heap[i] = heap[child];
                i = child;
            }
            heap[i] = last;
        }
        return true;
    }

    private bool TryCell(Vector3 position, out int x, out int z)
    {
        x = Mathf.FloorToInt((position.x - origin.x) / CellSize + .5f);
        z = Mathf.FloorToInt((position.z - origin.z) / CellSize + .5f);
        return x >= 0 && x < GridWidth && z >= 0 && z < GridHeight;
    }

    private int NearestWalkable(int index, int radius)
    {
        if (walkable[index] != 0) return index;
        int cx = index % GridWidth;
        int cz = index / GridWidth;
        for (int r = 1; r <= radius; r++)
        {
            for (int dz = -r; dz <= r; dz++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r) continue;
                    int x = cx + dx;
                    int z = cz + dz;
                    if (x < 0 || x >= GridWidth || z < 0 || z >= GridHeight) continue;
                    int candidate = z * GridWidth + x;
                    if (walkable[candidate] != 0) return candidate;
                }
            }
        }
        return -1;
    }

    /// <summary>Direction of the shortest grid route from a world position to the player.</summary>
    public static bool TryGetDirection(Vector3 from, out Vector3 direction)
    {
        direction = Vector3.zero;
        var field = Instance;
        if (field == null) return false;
        // Register the query even while the grid is still building: that is what
        // keeps the sliced build alive until a direction can be supplied.
        field.lastQueryAt = Time.time;
        if (!field.fieldReady || field.building || field.player == null) return false;
        if (!field.TryCell(from, out int x, out int z)) return false;

        int index = z * GridWidth + x;
        if (field.walkable[index] == 0)
        {
            index = field.NearestWalkable(index, 3);
            if (index < 0) return false;
        }
        // Two cells of look-ahead smooth the stair-step path without extra casts;
        // reactive steering still refuses any segment that hits real geometry.
        for (int step = 0; step < 2; step++)
        {
            int next = field.nextCell[index];
            if (next < 0) break;
            index = next;
        }
        Vector3 target = new Vector3(field.origin.x + (index % GridWidth) * CellSize,
            field.floors[index], field.origin.z + (index / GridWidth) * CellSize);
        Vector3 delta = target - from;
        delta.y = 0f;
        if (delta.sqrMagnitude < .0001f) return false;
        direction = delta.normalized;
        return true;
    }
}
