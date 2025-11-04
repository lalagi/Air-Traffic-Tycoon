// 2025. 11. 04. AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;

[ExecuteInEditMode] // This ensures the script runs in the editor
public class ReplaceSphereWithSpriteAnimation : MonoBehaviour
{
    public Texture2D planeTexture; // Assign the PlaneAnimation texture here
    public float frameDuration = 2f; // Duration for each frame

    private SpriteRenderer spriteRenderer;
    private Sprite[] animationFrames;
    private int currentFrameIndex = 0;
    private float timer = 0f;

    void Awake()
    {
        SetupSpriteRenderer();
    }

    void Start()
    {
        SetupSpriteRenderer();
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            // Update animation frames during Play mode
            timer += Time.deltaTime;
            if (timer >= frameDuration)
            {
                timer = 0f;
                currentFrameIndex = (currentFrameIndex + 1) % animationFrames.Length;
                spriteRenderer.sprite = animationFrames[currentFrameIndex];
            }
        }
    }

    private void SetupSpriteRenderer()
    {
        // Remove unnecessary components
        DestroyImmediate(GetComponent<MeshFilter>());
        DestroyImmediate(GetComponent<MeshRenderer>());
        DestroyImmediate(GetComponent<SphereCollider>());

        // Add SpriteRenderer if not already added
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        // Generate sprites from texture
        GenerateSpritesFromTexture();

        // Set the first sprite
        if (animationFrames != null && animationFrames.Length > 0)
        {
            spriteRenderer.sprite = animationFrames[currentFrameIndex];
        }
    }

    private void GenerateSpritesFromTexture()
    {
        if (planeTexture == null)
        {
            Debug.LogError("PlaneTexture is not assigned!");
            return;
        }

        animationFrames = new Sprite[4];
        int frameWidth = planeTexture.width / 2;
        int frameHeight = planeTexture.height / 2;

        animationFrames[0] = Sprite.Create(planeTexture, new Rect(0, frameHeight, frameWidth, frameHeight), new Vector2(0.5f, 0.5f));
        animationFrames[1] = Sprite.Create(planeTexture, new Rect(frameWidth, frameHeight, frameWidth, frameHeight), new Vector2(0.5f, 0.5f));
        animationFrames[2] = Sprite.Create(planeTexture, new Rect(0, 0, frameWidth, frameHeight), new Vector2(0.5f, 0.5f));
        animationFrames[3] = Sprite.Create(planeTexture, new Rect(frameWidth, 0, frameWidth, frameHeight), new Vector2(0.5f, 0.5f));
    }
}