using Crest;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class PlanarReflectionFix : MonoBehaviour
{
    [Header("修复设置")]
    [Tooltip("强制限制反射纹理尺寸")]
    [SerializeField] private bool _forceTextureSize = true;
    [SerializeField, Crest.Range(64, 1024)] private int _maxTextureSize = 256;

    [Tooltip("视锥体安全限制")]
    [SerializeField] private bool _clampFrustum = true;
    [SerializeField] private float _minNearClip = 0.1f;
    [SerializeField] private float _maxFarClip = 1000f;

    [Tooltip("错误恢复")]
    [SerializeField] private bool _autoRecovery = true;
    [SerializeField] private float _recoveryDelay = 1f;

    [Header("高级设置")]
    [SerializeField] private bool _forceResetProjection = true;
    [SerializeField] private bool _validateRenderTexture = true;

    private OceanPlanarReflection _targetReflection;
    private float _lastErrorTime;
    private RenderTexture _safetyTexture;
    private bool _isRecovering = false;

    // 移除了静态辅助类，改用实例方法
    private void OnEnable()
    {
        // 直接使用MonoBehaviour的StartCoroutine
        StartCoroutine(DelayedInitialize());
    }

    System.Collections.IEnumerator DelayedInitialize()
    {
        yield return new WaitForEndOfFrame();
        Initialize();
    }

    private void Initialize()
    {
        _targetReflection = GetComponent<OceanPlanarReflection>();
        if (_targetReflection == null)
        {
            Debug.LogWarning($"找不到OceanPlanarReflection组件", this);
            enabled = false;
            return;
        }

        CreateSafetyTexture();
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        Application.logMessageReceived += HandleUnityLog;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        Application.logMessageReceived -= HandleUnityLog;

        if (_safetyTexture != null)
        {
            _safetyTexture.Release();
            DestroyImmediate(_safetyTexture);
        }
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (_targetReflection == null || camera.cameraType != CameraType.Reflection || _isRecovering)
            return;

        // 强制限制纹理尺寸
        if (_forceTextureSize && _validateRenderTexture)
        {
            try
            {
                var textureField = typeof(OceanPlanarReflection)
                    .GetField("_reflectionTexture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (textureField != null)
                {
                    var rt = textureField.GetValue(_targetReflection) as RenderTexture;
                    if (rt != null && (rt.width > _maxTextureSize || rt.height > _maxTextureSize || !rt.IsCreated()))
                    {
                        Debug.Log($"修正反射纹理尺寸从 {rt.width}x{rt.height} 到 {_maxTextureSize}x{_maxTextureSize}");
                        textureField.SetValue(_targetReflection, _safetyTexture);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"纹理尺寸修正失败: {e.Message}");
            }
        }

        // 视锥体安全限制
        if (_clampFrustum)
        {
            camera.nearClipPlane = Mathf.Clamp(camera.nearClipPlane, _minNearClip, _maxFarClip * 0.9f);
            camera.farClipPlane = Mathf.Clamp(camera.farClipPlane, _minNearClip * 1.1f, _maxFarClip);
        }

        // 强制重置投影矩阵
        if (_forceResetProjection)
        {
            camera.ResetProjectionMatrix();
            camera.ResetAspect();
        }
    }

    private void CreateSafetyTexture()
    {
        if (_safetyTexture != null) return;

        _safetyTexture = new RenderTexture(_maxTextureSize, _maxTextureSize, 16, RenderTextureFormat.ARGBHalf)
        {
            name = "SafetyReflectionTexture",
            autoGenerateMips = false,
            useMipMap = false,
            hideFlags = HideFlags.HideAndDontSave
        };

        try
        {
            _safetyTexture.Create();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"安全纹理创建失败: {e.Message}");
            _safetyTexture = new RenderTexture(64, 64, 0);
        }
    }

    private void HandleUnityLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error &&
            (logString.Contains("Screen position out of view frustum") ||
             logString.Contains("Object reference not set")))
        {
            if (!_autoRecovery || Time.time - _lastErrorTime < _recoveryDelay || _isRecovering)
                return;

            _lastErrorTime = Time.time;
            _isRecovering = true;

            Debug.Log("检测到反射错误，开始自动恢复流程...");
            StartCoroutine(SafeRecovery());
        }
    }

    System.Collections.IEnumerator SafeRecovery()
    {
        // 第一步：禁用组件
        if (_targetReflection != null)
        {
            _targetReflection.enabled = false;
            yield return null;
        }

        // 第二步：清理资源
        if (_safetyTexture != null)
        {
            _safetyTexture.Release();
            yield return null;
            CreateSafetyTexture();
            yield return null;
        }

        // 第三步：重新激活
        if (_targetReflection != null)
        {
            _targetReflection.enabled = true;
            yield return null;
        }

        _isRecovering = false;
        Debug.Log("反射系统恢复完成");
    }
}