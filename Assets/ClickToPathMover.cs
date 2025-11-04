// 2025. 11. 04. AI-Tag
// Created with the help of Assistant, a Unity Artificial Intelligence product.

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ClickToPathMover_Raycast : MonoBehaviour
{
    [Header("Movement (1 unit = 1 meter)")]
    [SerializeField] private float speed = 6f;                 // meters per second
    [SerializeField] private float rotationSmoothTime = 0.08f; // seconds; higher = smoother
    [SerializeField] private float maxTurnRateDegPerSec = 360f;// deg/s cap

    [Header("Drawing")]
    [SerializeField] private float minPointDistance = 0.1f;
    [SerializeField] private float zPlane = 0f;
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private LayerMask clickableLayers = ~0;

    [Header("Landing Effects (last X% of path)")]
    [SerializeField] private bool enableLandingSlowdown = true;
    [SerializeField] private bool enableLandingScaling = true;
    [SerializeField, Range(0f, 1f)] private float landingStartPercent = 0.90f;
    [SerializeField] private Vector3 landingScale = Vector3.one * 0.3f;
    [SerializeField] private Transform scalingTarget; // Default = transform

    // Internal state
    private Camera cam;
    private LineRenderer line;
    private readonly List<Vector3> points = new();
    private readonly List<float> segLengths = new();
    private float totalLength;
    private int segIndex;
    private float alongSeg;
    private float traveled;
    private bool isDrawing;
    private bool isMoving;
    private Vector3 startScale;

    // Rotation smoothing
    private float currentHeadingDeg;
    private float headingVel;

    void Awake()
    {
        cam = Camera.main;
        if (cam == null)
            Debug.LogError("No Camera with MainCameraTag");

        line = gameObject.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.widthMultiplier = lineWidth;
        line.numCornerVertices = 8;
        line.numCapVertices = 8;
        line.startColor = Color.yellow;
        line.endColor = Color.yellow;
        line.positionCount = 0;
        line.useWorldSpace = true;

        if (scalingTarget == null)
            scalingTarget = transform; // scales sprite + collider
        startScale = scalingTarget.localScale;

        currentHeadingDeg = transform.eulerAngles.z;
    }

    void Update()
    {
        // Start drawing on click if this object was hit
        if (Input.GetMouseButtonDown(0) && HitThisObjectWithRay())
        {
            isDrawing = true;
            isMoving = false;
            points.Clear();
            segLengths.Clear();
            totalLength = 0f;
            line.positionCount = 0;
            scalingTarget.localScale = startScale;
            headingVel = 0f;
            TryAddPoint(MouseWorldOnZPlane());
        }

        // Add points while holding
        if (isDrawing && Input.GetMouseButton(0))
            TryAddPoint(MouseWorldOnZPlane());

        // On release, bake path
        if (isDrawing && Input.GetMouseButtonUp(0))
        {
            isDrawing = false;
            if (points.Count > 1)
            {
                PreparePath();

                if (GetBlendedDir(out Vector3 initDir))
                {
                    float initAngle = Mathf.Atan2(initDir.y, initDir.x) * Mathf.Rad2Deg - 90f;
                    currentHeadingDeg = initAngle;
                    transform.rotation = Quaternion.Euler(0f, 0f, currentHeadingDeg);
                }

                isMoving = true;
            }
        }

        if (isMoving)
            MoveAlongPath();
    }

    bool HitThisObjectWithRay()
    {
        if (cam == null) return false;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, clickableLayers, QueryTriggerInteraction.Collide))
            return hit.collider != null && hit.collider.gameObject == gameObject;
        return false;
    }

    Vector3 MouseWorldOnZPlane()
    {
        var mp = Input.mousePosition;
        float distance = Mathf.Abs(cam.transform.position.z - zPlane);
        var w = cam.ScreenToWorldPoint(new Vector3(mp.x, mp.y, distance));
        w.z = zPlane;
        return w;
    }

    void TryAddPoint(Vector3 p)
    {
        if (points.Count == 0 || Vector3.Distance(points[^1], p) >= minPointDistance)
        {
            points.Add(p);
            line.positionCount = points.Count;
            line.SetPosition(points.Count - 1, p);
        }
    }

    void PreparePath()
    {
        segLengths.Clear();
        totalLength = 0f;

        for (int i = 0; i < points.Count - 1; i++)
        {
            float d = Vector3.Distance(points[i], points[i + 1]);
            segLengths.Add(d);
            totalLength += d;
        }

        segIndex = 0;
        alongSeg = 0f;
        traveled = 0f;
        scalingTarget.localScale = startScale;
    }

    void MoveAlongPath()
    {
        if (points.Count < 2 || segIndex >= points.Count - 1)
        {
            isMoving = false;
            return;
        }

        float progress = (totalLength <= 1e-6f) ? 1f : (traveled / totalLength);

        // Landing scaling
        if (enableLandingScaling && progress >= landingStartPercent)
        {
            float t = Mathf.InverseLerp(landingStartPercent, 1f, progress);
            float s = t * t * (3f - 2f * t);
            scalingTarget.localScale = Vector3.Lerp(startScale, landingScale, s);
        }

        // Slowdown
        float slowFactor = 2f;
        if (enableLandingSlowdown && progress >= landingStartPercent)
        {
            float t = Mathf.InverseLerp(landingStartPercent, 1f, progress);
            slowFactor = Mathf.Lerp(1f, 0.1f, t);
        }

        float step = speed * slowFactor * Time.deltaTime;

        while (step > 0f && segIndex < points.Count - 1)
        {
            Vector3 a = points[segIndex];
            Vector3 b = points[segIndex + 1];
            float segLen = segLengths[segIndex];

            if (segLen < 1e-6f)
            {
                segIndex++;
                alongSeg = 0f;
                continue;
            }

            Vector3 segDir = (b - a) / segLen;
            float remainOnSeg = segLen - alongSeg;

            if (step < remainOnSeg)
            {
                alongSeg += step;
                traveled += step;
                transform.position = a + segDir * alongSeg;

                Vector3 faceDir = GetCornerBlendedDir(segDir);
                ApplySmoothedHeading(faceDir);
                step = 0f;
            }
            else
            {
                step -= remainOnSeg;
                traveled += remainOnSeg;
                segIndex++;
                alongSeg = 0f;
                transform.position = b;

                if (segIndex >= points.Count - 1)
                {
                    transform.position = points[^1];
                    if (enableLandingScaling)
                        scalingTarget.localScale = landingScale;
                    isMoving = false;
                    return;
                }
                else
                {
                    Vector3 nextSegDir = (points[segIndex + 1] - points[segIndex]).normalized;
                    ApplySmoothedHeading(GetCornerBlendedDir(nextSegDir));
                }
            }
        }
    }

    // --- Rotation helpers ---

    Vector3 GetCornerBlendedDir(Vector3 currentSegDir)
    {
        if (segIndex < points.Count - 2)
        {
            Vector3 nextDir = (points[segIndex + 2] - points[segIndex + 1]).normalized;
            float segLen = Mathf.Max(segLengths[segIndex], 1e-6f);
            float t = Mathf.Clamp01(alongSeg / segLen);
            t = t * t * (3f - 2f * t);
            Vector3 blended = Vector3.Slerp(currentSegDir, nextDir, t);
            if (blended.sqrMagnitude > 1e-10f) return blended.normalized;
        }
        return currentSegDir;
    }

    bool GetBlendedDir(out Vector3 dir)
    {
        dir = Vector3.right;
        if (points.Count < 2) return false;
        float d = Vector3.Distance(points[0], points[1]);
        if (d < 1e-6f) return false;
        Vector3 currentDir = (points[1] - points[0]) / d;
        dir = GetCornerBlendedDir(currentDir);
        return true;
    }

    void ApplySmoothedHeading(Vector3 desiredDir)
    {
        if (desiredDir.sqrMagnitude < 1e-10f) return;

        float desired = Mathf.Atan2(desiredDir.y, desiredDir.x) * Mathf.Rad2Deg - 90f;
        float target = Mathf.SmoothDampAngle(currentHeadingDeg, desired, ref headingVel, rotationSmoothTime);

        float maxStep = maxTurnRateDegPerSec * Time.deltaTime;
        float delta = Mathf.DeltaAngle(currentHeadingDeg, target);
        delta = Mathf.Clamp(delta, -maxStep, maxStep);
        currentHeadingDeg += delta;

        transform.rotation = Quaternion.Euler(0f, 0f, currentHeadingDeg);
    }
}
