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

    private OceanPlanarReflection _targetReflection;
    private float _lastErrorTime;
    private RenderTexture _safetyTexture;

    private void OnEnable()
    {
        // 延迟初始化以避免执行顺序问题
        Invoke(nameof(Initialize), 0.1f);
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

        // 创建安全备用纹理
        CreateSafetyTexture();

        // 订阅渲染回调
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

        if (_safetyTexture != null)
        {
            _safetyTexture.Release();
            DestroyImmediate(_safetyTexture);
        }
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (_targetReflection == null || camera.cameraType != CameraType.Reflection)
            return;

        // 强制限制纹理尺寸
        if (_forceTextureSize)
        {
            var textureField = typeof(OceanPlanarReflection)
                .GetField("_reflectionTexture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (textureField != null && textureField.GetValue(_targetReflection) is RenderTexture rt &&
                (rt.width > _maxTextureSize || rt.height > _maxTextureSize))
            {
                Debug.Log($"修正反射纹理尺寸从 {rt.width}x{rt.height} 到 {_maxTextureSize}x{_maxTextureSize}");
                textureField.SetValue(_targetReflection, _safetyTexture);
            }
        }

        // 视锥体安全限制
        if (_clampFrustum)
        {
            camera.nearClipPlane = Mathf.Max(_minNearClip, camera.nearClipPlane);
            camera.farClipPlane = Mathf.Min(_maxFarClip, camera.farClipPlane);
        }
    }

    private void CreateSafetyTexture()
    {
        if (_safetyTexture != null) return;

        _safetyTexture = new RenderTexture(_maxTextureSize, _maxTextureSize, 16)
        {
            name = "SafetyReflectionTexture",
            autoGenerateMips = false,
            useMipMap = false
        };
        _safetyTexture.Create();
    }

    // 错误捕获（通过全局日志捕获）
    private void OnValidate()
    {
        Application.logMessageReceived += HandleUnityLog;
    }

    private void HandleUnityLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error && logString.Contains("Screen position out of view frustum"))
        {
            if (!_autoRecovery || Time.time - _lastErrorTime < _recoveryDelay)
                return;

            _lastErrorTime = Time.time;
            Debug.Log("检测到反射错误，尝试自动恢复...");

            // 重置反射组件
            if (_targetReflection != null)
            {
                _targetReflection.enabled = false;
                _targetReflection.enabled = true;
            }
        }
    }
}