using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

// Stamps a flat quad onto scene geometry, like a sticker pressed onto the
// surface it's resting against. Point the object's up axis at the wall/floor
// you want to decal, then press Build in the Inspector.
//
// Scatter Mode reuses the same raycast/placement logic to stamp many decals
// at once, from random points inside a box gizmo instead of a single point.
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Decal : MonoBehaviour
{
    public Material material;
    public Sprite sprite;
    public LayerMask layerMask = ~0;
    [Range(0f, 90f)] public float maxAngle = 90f;
    [Range(0f, 0.2f)] public float offset = 0.04f;

    public bool scatterMode = false;
    public Vector3 scatterCenter = Vector3.zero;
    public Vector3 scatterBounds = Vector3.one;
    public int decalCount = 10;
    public int randomSeed = 0;

    struct ScatterRayDebug
    {
        public Vector3 start;
        public Vector3 end;
        public bool hit;
    }

    [System.NonSerialized] List<ScatterRayDebug> scatterDebugRays = new List<ScatterRayDebug>();

    // Always drawn (not OnDrawGizmosSelected) so the scatter volume and the
    // last Build's raycasts stay visible while working on other objects.
    void OnDrawGizmos()
    {
        if (!scatterMode)
            return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(scatterCenter, scatterBounds);
        Gizmos.matrix = previousMatrix;

        foreach (ScatterRayDebug ray in scatterDebugRays)
        {
            Gizmos.color = ray.hit ? Color.green : Color.red;
            Gizmos.DrawLine(ray.start, ray.end);
        }
    }

    public void Build()
    {
#if UNITY_EDITOR
        if (sprite == null)
        {
            Debug.LogWarning($"Decal '{name}': no Sprite assigned, nothing to build.", this);
            return;
        }

        Undo.SetCurrentGroupName("Build Decal");
        int undoGroup = Undo.GetCurrentGroup();

        if (scatterMode)
            BuildScatter();
        else
            BuildSingle();

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
#else
        Debug.LogWarning("Decal.Build() only runs in the Editor.", this);
#endif
    }

#if UNITY_EDITOR
    struct DecalPlacement
    {
        public bool success;
        public Vector3 point;
        public Vector3 normal;
    }

    void BuildSingle()
    {
        Vector3 origin = transform.position;
        Vector3 direction = -transform.up;

        DecalPlacement placement = RaycastPlacement(origin, direction);
        if (!placement.success)
        {
            Debug.LogWarning($"Decal '{name}': raycast along -up didn't hit a valid surface (no collider on the Layer Mask, or the hit angle exceeded Max Angle).", this);
            return;
        }

        Quaternion rotation = ComputeRotation(placement.normal, transform.forward, 0f);
        ApplyDecal(transform, GetComponent<MeshFilter>(), GetComponent<MeshRenderer>(), placement.point, placement.normal, rotation);
    }

    void BuildScatter()
    {
        ClearScatterChildren();
        scatterDebugRays.Clear();

        System.Random rng = new System.Random(randomSeed);
        Vector3 halfSize = scatterBounds * 0.5f;
        int placed = 0;

        for (int i = 0; i < decalCount; i++)
        {
            Vector3 localPoint = scatterCenter + new Vector3(
                Mathf.Lerp(-halfSize.x, halfSize.x, (float)rng.NextDouble()),
                Mathf.Lerp(-halfSize.y, halfSize.y, (float)rng.NextDouble()),
                Mathf.Lerp(-halfSize.z, halfSize.z, (float)rng.NextDouble()));

            Vector3 origin = transform.TransformPoint(localPoint);
            Vector3 direction = -transform.up;

            DecalPlacement placement = RaycastPlacement(origin, direction);

            Vector3 debugEnd = placement.success ? placement.point : origin + direction * 5f;
            scatterDebugRays.Add(new ScatterRayDebug { start = origin, end = debugEnd, hit = placement.success });

            if (!placement.success)
                continue;

            GameObject child = new GameObject($"Decal_Scatter_{i}");
            Undo.RegisterCreatedObjectUndo(child, "Build Decal");
            child.transform.SetParent(transform, false);

            MeshFilter childMeshFilter = child.AddComponent<MeshFilter>();
            MeshRenderer childMeshRenderer = child.AddComponent<MeshRenderer>();

            float spinDegrees = (float)rng.NextDouble() * 360f;
            Quaternion rotation = ComputeRotation(placement.normal, transform.forward, spinDegrees);
            ApplyDecal(child.transform, childMeshFilter, childMeshRenderer, placement.point, placement.normal, rotation);
            placed++;
        }

        if (placed == 0)
        {
            Debug.LogWarning($"Decal '{name}': scatter placed 0 decals out of {decalCount} attempts. Check Scatter Bounds/Layer Mask/Max Angle.", this);
        }
    }

    void ClearScatterChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("Decal_Scatter_"))
                Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    DecalPlacement RaycastPlacement(Vector3 origin, Vector3 direction)
    {
        DecalPlacement result = default;

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, Mathf.Infinity, layerMask))
            return result;

        float angle = Vector3.Angle(hit.normal, transform.up);
        if (angle > maxAngle)
            return result;

        result.success = true;
        result.point = hit.point;
        result.normal = hit.normal;
        return result;
    }

    // Aligns local up to the hit normal while keeping forwardHint's facing
    // (projected flat onto the surface), then applies an optional spin around
    // the normal on top - used by scatter mode for per-decal rotation variety.
    static Quaternion ComputeRotation(Vector3 normal, Vector3 forwardHint, float extraSpinDegrees)
    {
        Vector3 projectedForward = ProjectOnPlane(forwardHint, normal);
        Quaternion rotation = Quaternion.LookRotation(projectedForward, normal);
        if (extraSpinDegrees != 0f)
            rotation = Quaternion.AngleAxis(extraSpinDegrees, normal) * rotation;
        return rotation;
    }

    void ApplyDecal(Transform target, MeshFilter meshFilter, MeshRenderer meshRenderer, Vector3 point, Vector3 normal, Quaternion rotation)
    {
        Undo.RecordObject(target, "Build Decal");
        target.position = point + normal * offset;
        target.rotation = rotation;

        Undo.RecordObject(meshFilter, "Build Decal");
        meshFilter.sharedMesh = BuildQuadMesh(sprite);

        if (material != null)
        {
            Undo.RecordObject(meshRenderer, "Build Decal");
            meshRenderer.sharedMaterial = ResolveMaterial(material, sprite);
        }
    }

    // Reuses the assigned material when it already points at the sprite's texture
    // (e.g. a shared atlas material), otherwise makes a per-instance copy so the
    // sprite's texture can be swapped in without touching the source asset.
    static Material ResolveMaterial(Material source, Sprite sprite)
    {
        if (source.mainTexture == sprite.texture)
            return source;

        Material instance = new Material(source);
        instance.name = $"{source.name} ({sprite.name})";
        instance.mainTexture = sprite.texture;
        return instance;
    }

    static Vector3 ProjectOnPlane(Vector3 direction, Vector3 planeNormal)
    {
        Vector3 projected = Vector3.ProjectOnPlane(direction, planeNormal);
        if (projected.sqrMagnitude < 0.0001f)
        {
            projected = Vector3.ProjectOnPlane(Vector3.forward, planeNormal);
            if (projected.sqrMagnitude < 0.0001f)
                projected = Vector3.ProjectOnPlane(Vector3.right, planeNormal);
        }
        return projected.normalized;
    }

    // Unit quad in local space, lying flat on the XZ plane with its normal
    // along local up. The Transform's rotation/scale (set in ApplyDecal) does
    // the rest of the work of matching the hit surface and sizing the decal.
    static Mesh BuildQuadMesh(Sprite sprite)
    {
        Vector3[] vertices =
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f, -0.5f),
        };

        Vector3 normal = Vector3.up;
        Vector3[] normals = { normal, normal, normal, normal };
        int[] triangles = { 0, 1, 2, 0, 2, 3 };

        Texture2D texture = sprite.texture;
        Rect spriteRect = sprite.rect;
        Vector2 uvMin = new Vector2(spriteRect.xMin / texture.width, spriteRect.yMin / texture.height);
        Vector2 uvMax = new Vector2(spriteRect.xMax / texture.width, spriteRect.yMax / texture.height);
        Vector2[] uvs =
        {
            new Vector2(uvMin.x, uvMin.y),
            new Vector2(uvMin.x, uvMax.y),
            new Vector2(uvMax.x, uvMax.y),
            new Vector2(uvMax.x, uvMin.y),
        };

        Mesh mesh = new Mesh { name = $"Decal_{sprite.name}" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }
#endif
}
