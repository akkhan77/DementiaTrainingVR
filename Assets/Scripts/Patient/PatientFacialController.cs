using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ExpressionType
{
    Neutral,
    Sleeping,
    Aggressive,
    Confusion
}

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class PatientFacialController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private List<ExpressionConfig> _expressionConfigs;
    [SerializeField] private ExpressionType _initialExpression = ExpressionType.Neutral;

    private SkinnedMeshRenderer _targetRenderer;
    private Dictionary<ExpressionType, ExpressionConfig> _configMap;
    private int _blendShapeCount;

    private Coroutine _activeTransition;

    private void Awake()
    {
        // Validate and Cache SMR
        _targetRenderer = GetComponent<SkinnedMeshRenderer>();
        if (_targetRenderer == null || _targetRenderer.sharedMesh == null)
        {
            Debug.LogError($"[PatientFacialController] Missing SkinnedMeshRenderer or Mesh on {gameObject.name}. Disabling script.", this);
            enabled = false;
            return;
        }

        _blendShapeCount = _targetRenderer.sharedMesh.blendShapeCount;

        // Initialize runtime map for fast O(1) lookup instead of iterating lists
        _configMap = new Dictionary<ExpressionType, ExpressionConfig>();
        foreach (var config in _expressionConfigs)
        {
            if (!_configMap.ContainsKey(config.Type))
            {
                _configMap.Add(config.Type, config);
            }
            else
            {
                Debug.LogWarning($"[PatientFacialController] Duplicate expression config found for {config.Type}. Ignoring duplicates.", this);
            }
        }
    }

    private void Start()
    {
        ApplyExpressionInstant(_initialExpression);
    }

    // ============================================================
    // PUBLIC API - Call these from other scripts (e.g., ScenarioManager)
    // ============================================================

    /// <summary>
    /// Smoothly transitions to the specified facial expression over its configured duration.
    /// </summary>
    public void SetExpression(ExpressionType newType)
    {
        ResetAllBlendShapes();
        if (!_configMap.ContainsKey(newType) && newType != ExpressionType.Neutral)
        {
            Debug.LogWarning($"[PatientFacialController] Expression '{newType}' not configured. Reverting to Neutral.", this);
            newType = ExpressionType.Neutral;
        }

        // Stop any ongoing transition to prevent "expression fighting"
        if (_activeTransition != null)
        {
            StopCoroutine(_activeTransition);
        }

        // Handle Neutral specifically (reset all to 0)
        if (newType == ExpressionType.Neutral)
        {
            _activeTransition = StartCoroutine(TransitionToNeutral(0.3f)); // Default smooth return
        }
        else
        {
            _activeTransition = StartCoroutine(TransitionToExpressionCoroutine(_configMap[newType]));
        }

        _initialExpression = newType; // Update current state
    }

    // ============================================================
    // INTERNAL LOGIC
    // ============================================================

    private void ApplyExpressionInstant(ExpressionType type)
    {
        ResetAllBlendShapes(); // Start clean

        if (type == ExpressionType.Neutral || !_configMap.ContainsKey(type)) return;

        ExpressionConfig config = _configMap[type];
        foreach (var target in config.Targets)
        {
            int index = _targetRenderer.sharedMesh.GetBlendShapeIndex(target.BlendShapeName);
            if (index != -1)
            {
                _targetRenderer.SetBlendShapeWeight(index, target.targetWeight);
            }
        }
    }

    private IEnumerator TransitionToExpressionCoroutine(ExpressionConfig config)
    {
        float elapsedTime = 0f;

        // 1. Capture starting weights for all affected shapes
        Dictionary<int, float> startWeights = new Dictionary<int, float>();

        // Validate targets and capture start states
        List<KeyValuePair<int, float>> validTargets = new List<KeyValuePair<int, float>>();
        foreach (var target in config.Targets)
        {
            int index = _targetRenderer.sharedMesh.GetBlendShapeIndex(target.BlendShapeName);
            if (index != -1)
            {
                startWeights[index] = _targetRenderer.GetBlendShapeWeight(index);
                validTargets.Add(new KeyValuePair<int, float>(index, target.targetWeight));
            }
            else
            {
                Debug.LogWarning($"[PatientFacialController] BlendShape '{target.BlendShapeName}' not found on mesh '{_targetRenderer.sharedMesh.name}'. Check Inspector spelling against circled image.", this);
            }
        }

        // 2. The Lerp Loop
        while (elapsedTime < config.TransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / config.TransitionDuration);
            // Optional: Use an AnimationCurve here for ease-in/ease-out control
            float t = normalizedTime;

            foreach (var target in validTargets)
            {
                int index = target.Key;
                float finalWeight = target.Value;
                float newWeight = Mathf.Lerp(startWeights[index], finalWeight, t);
                _targetRenderer.SetBlendShapeWeight(index, newWeight);
            }

            yield return null; // Wait for next frame
        }

        // 3. Ensure final precision
        foreach (var target in validTargets)
        {
            _targetRenderer.SetBlendShapeWeight(target.Key, target.Value);
        }

        _activeTransition = null;
    }

    private IEnumerator TransitionToNeutral(float duration)
    {
        float elapsedTime = 0f;
        float[] startWeights = new float[_blendShapeCount];

        // Capture ALL current weights
        for (int i = 0; i < _blendShapeCount; i++)
        {
            startWeights[i] = _targetRenderer.GetBlendShapeWeight(i);
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            // Lerp every active shape back to 0
            for (int i = 0; i < _blendShapeCount; i++)
            {
                if (startWeights[i] > 0.01f) // Optimization: only update shapes that are active
                {
                    _targetRenderer.SetBlendShapeWeight(i, Mathf.Lerp(startWeights[i], 0f, t));
                }
            }
            yield return null;
        }

        ResetAllBlendShapes();
        _activeTransition = null;
    }

    private void ResetAllBlendShapes()
    {
        for (int i = 0; i < _blendShapeCount; i++)
        {
            _targetRenderer.SetBlendShapeWeight(i, 0f);
        }
    }
}

[Serializable]
public struct BlendShapeTarget
{
    [Tooltip("Must match the name in the image exactly (e.g., 'shout')")]
    public string BlendShapeName;
    [Range(0f, 100f)] public float targetWeight;
}

[Serializable]
public class ExpressionConfig
{
    public ExpressionType Type;
    public float TransitionDuration = 0.25f;
    public List<BlendShapeTarget> Targets;
}