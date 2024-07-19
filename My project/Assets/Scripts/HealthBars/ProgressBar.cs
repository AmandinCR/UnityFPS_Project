using System.Collections;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    
#region Custom
    [SerializeField] private TextMeshProUGUI damageNumberText;

    public void SetDamage(float damage, float progress)
    {
        damageNumberText.text = damage.ToString();
        SetProgress(progress, DefaultSpeed);
    }
#endregion

    [SerializeField]
    private Image InstantFillImage;
    [SerializeField]
    private Image SlowFillImage;
    [SerializeField]
    private float DefaultSpeed = 1f;
    [SerializeField]
    private Gradient ColorGradient;
    [SerializeField]
    private UnityEvent<float> OnProgress;
    [SerializeField]
    private UnityEvent OnCompleted;

    private Coroutine AnimationCoroutine;

    private void Start()
    {
        if (SlowFillImage.type != Image.Type.Filled)
        {
            Debug.LogError($"{name}'s ProgressImage is not of type \"Filled\" so it cannot be used as a progress bar. Disabling this Progress Bar.");
            enabled = false;
#if UNITY_EDITOR
            EditorGUIUtility.PingObject(this.gameObject);
#endif
        }
    }

    public void SetProgress(float Progress)
    {
        SetProgress(Progress, DefaultSpeed);
    }

    public void SetProgress(float Progress, float Speed)
    {
        if (Progress < 0 || Progress > 1)
        {
            Debug.LogWarning($"Invalid progress passed, expected value is between 0 and 1, got {Progress}. Clamping.");
            Progress = Mathf.Clamp01(Progress);
        }
        if (Progress != SlowFillImage.fillAmount)
        {
            if (AnimationCoroutine != null)
            {
                StopCoroutine(AnimationCoroutine);
            }

            AnimationCoroutine = StartCoroutine(AnimateProgress(Progress, Speed));
            InstantFillImage.fillAmount = Progress;
        }
    }

    private IEnumerator AnimateProgress(float Progress, float Speed)
    {
        float time = 0;
        float initialProgress = SlowFillImage.fillAmount;

        while (time < 1)
        {
            SlowFillImage.fillAmount = Mathf.Lerp(initialProgress, Progress, time);
            time += Time.deltaTime * Speed;

            SlowFillImage.color = ColorGradient.Evaluate(1 - SlowFillImage.fillAmount);

            OnProgress?.Invoke(SlowFillImage.fillAmount);
            yield return null;
        }

        SlowFillImage.fillAmount = Progress;
        SlowFillImage.color = ColorGradient.Evaluate(1 - SlowFillImage.fillAmount);

        OnProgress?.Invoke(Progress);
        OnCompleted?.Invoke();
    }
}
