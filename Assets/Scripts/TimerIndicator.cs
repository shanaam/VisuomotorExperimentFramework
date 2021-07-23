using UnityEngine;
using UnityEngine.UI;

public class TimerIndicator : MonoBehaviour
{
    /// <summary>
    /// How long the participant has in seconds to perform the trial
    /// </summary>
    public float Timer = 1.0f;

    private float timerMaxTime;

    // Visual cue objects
    public GameObject Fill, Background, Text;

    // When true, we suspend the timer
    private bool cancel = true;

    /// <summary>
    /// Begins the animation sequence and starts the timer
    /// </summary>
    public void BeginTimer()
    {
        timerMaxTime = Timer;

        // Transition out by fading alpha
        LeanTween.alpha(Fill.GetComponent<RectTransform>(), 0.0f, 0.5f).setDelay(Timer + 0.5f);
        LeanTween.alpha(Background.GetComponent<RectTransform>(), 0.0f, 0.5f).setDelay(Timer + 0.5f);

        // Transition out with squishing effect
        LeanTween.scale(Fill.GetComponent<RectTransform>(), Vector3.zero, 1.0f)
            .setEaseInElastic().setDelay(Timer);
        LeanTween.scale(Background.GetComponent<RectTransform>(), Vector3.zero, 1.0f)
            .setEaseInElastic().setDelay(Timer);

        // Instantly show text cue after timer expires
        LeanTween.alphaText(Text.GetComponent<RectTransform>(), 1.0f, 0.1f).setDelay(Timer);

        // Transition out text
        LeanTween.scale(Text.GetComponent<RectTransform>(),
            Text.GetComponent<RectTransform>().localScale * 1.4f, 0.5f
        ).setEaseOutCubic().setDelay(Timer);
        LeanTween.alphaText(Text.GetComponent<RectTransform>(), 0.0f, 0.5f).setDelay(Timer + 0.5f);

        cancel = false;
    }

    void Update()
    {
        if (cancel) return;

        if (Timer >= 0.0f)
        {
            Timer -= Time.deltaTime;
            GetComponent<Slider>().value = Timer / timerMaxTime;
        }
        else
        {
            cancel = true;
        }
    }

    /// <summary>
    /// Suspends all animations and halts the timer
    /// </summary>
    public void Cancel()
    {
        LeanTween.cancel(Fill);
        LeanTween.cancel(Background);
        LeanTween.cancel(gameObject);
        LeanTween.cancel(Text);
        cancel = true;
    }
}
