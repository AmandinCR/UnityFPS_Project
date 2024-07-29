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
    [SerializeField] private float textTimeToDissapear = 1f;
    [SerializeField] private float textTimeBeforeAlpha = 2f;
    private float alphaTimer = 0f;
    private float accumulatedDamage = 0f;
    [HideInInspector] public Transform player;
    [SerializeField] private Image[] barImages = new Image[3];

    /*
    private void Update()
    {
        float distance = (player.position - this.transform.position).magnitude / 10f;
        transform.localScale = new Vector3(distance,distance,distance);
    }
    */

    public void SetDamage(float damage, float progress)
    {
        accumulatedDamage += damage;
        damageNumberText.text = "[ " + Mathf.Round(accumulatedDamage).ToString() + " ]";
        damageNumberText.alpha = 1f;
        foreach (Image bar in barImages)
        {
            var tempColor = bar.color;
            tempColor.a = 1f;
            bar.color = tempColor;
        }
        alphaTimer = textTimeBeforeAlpha + textTimeToDissapear;
        SetProgress(progress, DefaultSpeed);
    }

    private void FixedUpdate()
    {
        float distance = (player.position - this.transform.position).magnitude / 10f;
        transform.localScale = new Vector3(distance,distance,distance);

        if (alphaTimer > 0f)
        {
            alphaTimer -= Time.fixedDeltaTime;
        }

        if (alphaTimer < 0f)
        {
            alphaTimer = 0f;
            accumulatedDamage = 0f;
        }

        if (alphaTimer < textTimeToDissapear)
        {
            damageNumberText.alpha = alphaTimer / textTimeToDissapear;
            foreach (Image bar in barImages)
            {
                var tempColor = bar.color;
                tempColor.a = alphaTimer / textTimeToDissapear;
                bar.color = tempColor;
            }
        }
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
            //Debug.LogWarning($"Invalid progress passed, expected value is between 0 and 1, got {Progress}. Clamping.");
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
