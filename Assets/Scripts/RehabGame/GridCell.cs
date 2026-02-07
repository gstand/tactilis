using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Represents a single cell in the 2x3 rehab grid.
/// Handles visual state (active/inactive) and hit detection.
/// </summary>
public class GridCell : MonoBehaviour
{
    [Header("Cell Identity")]
    public int row;
    public int column;

    [Header("Visual Settings")]
    public MeshRenderer meshRenderer;
    public Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    public Color activeColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color hitColor = new Color(0.2f, 1f, 0.2f, 1f);
    public Color missColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("Animation")]
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.2f;
    public float hitFlashDuration = 0.3f;

    [Header("Audio")]
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip spawnSound;

    [Header("Events")]
    public UnityEvent onActivated;
    public UnityEvent onDeactivated;
    public UnityEvent onHit;
    public UnityEvent onMissed;

    public bool IsActive => _isActive;
    public int CellIndex => row * 3 + column; // 0-5 for 2x3 grid

    bool _isActive;
    float _activationTime;
    Material _material;
    AudioSource _audioSource;
    Color _targetColor;
    bool _isFlashing;
    float _flashEndTime;
    Color _flashColor;

    void Awake()
    {
        if (!meshRenderer)
            meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer)
        {
            _material = meshRenderer.material; // Instance material
            _material.color = inactiveColor;
        }

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1f; // 3D sound
    }

    void Update()
    {
        if (_material == null) return;

        if (_isFlashing)
        {
            if (Time.time >= _flashEndTime)
            {
                _isFlashing = false;
                _material.color = _isActive ? activeColor : inactiveColor;
            }
            else
            {
                float t = (Time.time - (_flashEndTime - hitFlashDuration)) / hitFlashDuration;
                _material.color = Color.Lerp(_flashColor, _isActive ? activeColor : inactiveColor, t);
            }
        }
        else if (_isActive)
        {
            // Pulse animation when active
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            Color pulsedColor = activeColor + new Color(pulse, pulse, pulse, 0);
            _material.color = pulsedColor;
        }
    }

    /// <summary>
    /// Activate this cell as a target to hit
    /// </summary>
    public void Activate()
    {
        if (_isActive) return;

        _isActive = true;
        _activationTime = Time.time;

        if (_material)
            _material.color = activeColor;

        PlaySound(spawnSound);
        onActivated?.Invoke();
    }

    /// <summary>
    /// Deactivate this cell (either hit or timed out)
    /// </summary>
    public void Deactivate()
    {
        if (!_isActive) return;

        _isActive = false;

        if (_material)
            _material.color = inactiveColor;

        onDeactivated?.Invoke();
    }

    /// <summary>
    /// Register a successful hit on this cell
    /// </summary>
    /// <returns>Time taken to hit (seconds since activation)</returns>
    public float RegisterHit()
    {
        if (!_isActive) return -1f;

        float reactionTime = Time.time - _activationTime;

        Flash(hitColor);
        PlaySound(hitSound);
        onHit?.Invoke();

        Deactivate();

        return reactionTime;
    }

    /// <summary>
    /// Register a miss (cell timed out without being hit)
    /// </summary>
    public void RegisterMiss()
    {
        if (!_isActive) return;

        Flash(missColor);
        PlaySound(missSound);
        onMissed?.Invoke();

        Deactivate();
    }

    void Flash(Color color)
    {
        _isFlashing = true;
        _flashColor = color;
        _flashEndTime = Time.time + hitFlashDuration;

        if (_material)
            _material.color = color;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip && _audioSource)
        {
            _audioSource.clip = clip;
            _audioSource.Play();
        }
    }

    /// <summary>
    /// Check if a world position is within this cell's bounds
    /// </summary>
    public bool ContainsPoint(Vector3 worldPoint)
    {
        Collider col = GetComponent<Collider>();
        if (col)
            return col.bounds.Contains(worldPoint);

        // Fallback: simple distance check
        float cellSize = transform.localScale.x;
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        return Mathf.Abs(localPoint.x) <= 0.5f && Mathf.Abs(localPoint.z) <= 0.5f;
    }
}
