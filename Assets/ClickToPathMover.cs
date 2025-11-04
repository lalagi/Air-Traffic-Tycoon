// 2025. 11. 04. AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ClickToPathMover_Raycast : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;

    [Header("Drawing")]
    [SerializeField] private float minPointDistance = 0.1f;
    [SerializeField] private float zPlane = 0f;
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private LayerMask clickableLayers = ~0;

    private Camera cam;
    private LineRenderer line;
    private readonly List<Vector3> points = new();
    private bool isDrawing = false;
    private bool isMoving = false;
    private int currentIdx = 0;

    private Vector3 currentVelocity = Vector3.zero; // Used for SmoothDamp

    void Awake()
    {
        cam = Camera.main; // Camera with MainCamera tag
        if (cam == null) Debug.LogError("No Camera with MainCameraTag");

        line = gameObject.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.widthMultiplier = lineWidth;
        line.numCornerVertices = 8;
        line.numCapVertices = 8;
        line.startColor = Color.yellow;
        line.endColor = Color.yellow;
        line.positionCount = 0;
    }

    void Update()
    {
        // 1) Click start: only begin drawing if we actually hit THIS object
        if (Input.GetMouseButtonDown(0))
        {
            if (HitThisObjectWithRay())
            {
                Debug.Log("Click: you hit the object, starting drawing.");
                isDrawing = true;
                isMoving = false;
                points.Clear();
                line.positionCount = 0;
                TryAddPoint(MouseWorldOnZPlane());
            }
        }

        // 2) While drawing, add points
        if (isDrawing && Input.GetMouseButton(0))
        {
            TryAddPoint(MouseWorldOnZPlane());
        }

        // 3) Mouse release: start moving
        if (isDrawing && Input.GetMouseButtonUp(0))
        {
            isDrawing = false;
            if (points.Count > 1)
            {
                isMoving = true;
                currentIdx = 0;
            }
        }

        // 4) Movement along the path
        if (isMoving)
        {
            MoveAlongPath();
        }
    }

    bool HitThisObjectWithRay()
    {
        if (cam == null) return false;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, clickableLayers, QueryTriggerInteraction.Collide))
        {
            return hit.collider != null && hit.collider.gameObject == gameObject;
        }
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

    void MoveAlongPath()
    {
        Vector3 target = points[currentIdx];
        
        // Smoothly move towards the target point
        transform.position = Vector3.SmoothDamp(transform.position, target, ref currentVelocity, 0.1f);

        // Rotate towards the next point (2D rotation with 90-degree offset)
        Vector3 direction = target - transform.position;
        if (direction.sqrMagnitude > 0.001f) // Avoid jitter when very close to the target
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f); // Subtract 90 degrees to align the nose
        }

        // Check if the object has reached the target point
        if (Vector3.Distance(transform.position, target) <= 0.05f) // Slightly increase the threshold
        {
            currentIdx++;
            if (currentIdx >= points.Count)
            {
                isMoving = false;
                currentVelocity = Vector3.zero; // Reset velocity to avoid overshooting
            }
        }
    }
}