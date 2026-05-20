using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI comboText;
    [SerializeField] TextMeshProUGUI gradeText;
    [SerializeField] Slider          healthSlider;

    [Header("Follow Settings")]
    [SerializeField] float distanceFromCamera = 2.0f;
    [SerializeField] float heightOffset = -0.3f;
    [SerializeField] float followSpeed = 5.0f;
    [SerializeField] bool  smoothFollow = true;

    Transform mainCamera;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        
        mainCamera = Camera.main?.transform;
    }

    void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main?.transform;
        if (mainCamera == null) return;

        // Calculate target position
        Vector3 targetPos = mainCamera.position + (mainCamera.forward * distanceFromCamera);
        targetPos.y += heightOffset;

        // Calculate target rotation (look at camera)
        Quaternion targetRot = Quaternion.LookRotation(transform.position - mainCamera.position);

        if (smoothFollow)
        {
            // Smoothly move and rotate
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);
        }
        else
        {
            // Instant snap
            transform.position = targetPos;
            transform.rotation = targetRot;
        }
    }

    public void OnHit(HitGrade grade, int score, int combo)
    {
        UpdateScore(score);
        if (comboText != null) comboText.text = combo > 1 ? $"x{combo}" : "";
        if (gradeText  != null)
        {
            gradeText.text  = grade.ToString().ToUpper();
            gradeText.color = grade == HitGrade.Perfect ? Color.yellow :
                              grade == HitGrade.Great   ? Color.cyan   : Color.white;
        }
    }

    public void OnMiss(int score, int combo)
    {
        UpdateScore(score);
        if (comboText != null) comboText.text = "";
        if (gradeText  != null) { gradeText.text = "MISS"; gradeText.color = Color.red; }
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = score.ToString("N0");
    }

    public void UpdateHealth(float normalized)
    {
        if (healthSlider != null) healthSlider.value = normalized;
    }
}
