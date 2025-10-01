using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreDisplayUI : MonoBehaviour
{
    [Header("UI设置")]
    [SerializeField] private GameObject scoreUIPrefab; // 分数UI预制体
    [SerializeField] private Vector3 uiOffset = new Vector3(0, 0.3f, 0); // UI相对于相机的位置偏移
    [SerializeField] private float uiScale = 0.002f; // UI缩放比例

    private Camera vrCamera;
    private GameObject scoreUIInstance;
    private TextMeshProUGUI scoreText;
    private int lastDisplayedScore = -1;

    void Start()
    {
        // 获取主相机（VR相机）
        vrCamera = Camera.main;
        if (vrCamera == null)
        {
            Debug.LogError("Main Camera not found");
            return;
        }

        // 创建分数UI
        CreateScoreUI();

        // 初始更新一次分数显示
        UpdateScoreDisplay();
    }

    void Update()
    {
        // 确保UI跟随相机移动
        if (scoreUIInstance != null && vrCamera != null)
        {
            // 更新UI位置，使其始终在相机正上方
            scoreUIInstance.transform.position = vrCamera.transform.position + vrCamera.transform.TransformVector(uiOffset);

            // 使UI始终面向相机
            scoreUIInstance.transform.rotation = Quaternion.LookRotation(
                scoreUIInstance.transform.position - vrCamera.transform.position);
        }

        // 检查分数是否有变化，如有变化则更新显示
        if (CastleGate.ScoreSystem.CurrentScore != lastDisplayedScore)
        {
            UpdateScoreDisplay();
        }
    }

    private void CreateScoreUI()
    {
        // 实例化UI预制体
        scoreUIInstance = Instantiate(
            scoreUIPrefab,
            vrCamera.transform.position + vrCamera.transform.TransformVector(uiOffset),
            Quaternion.identity,
            vrCamera.transform // 设置为相机的子对象，使其跟随相机移动
        );

        // 设置UI缩放
        scoreUIInstance.transform.localScale = Vector3.one * uiScale;

        // 获取文本组件
        scoreText = scoreUIInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (scoreText == null)
        {
            Debug.LogError("Score text component not found in UI prefab");
        }
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            int currentScore = CastleGate.ScoreSystem.CurrentScore;
            scoreText.text = $"分数: {currentScore}";
            lastDisplayedScore = currentScore;
        }
    }

    // 可选：提供外部调用的更新方法
    public void RefreshScoreDisplay()
    {
        UpdateScoreDisplay();
    }
}