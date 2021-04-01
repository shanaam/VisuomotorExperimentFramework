using UnityEngine;
using UnityEngine.UI;

public class PinballTimerIndicator : MonoBehaviour
{
    /// <summary>
    /// How long the participant has in seconds to perform the trial
    /// </summary>
    public float Timer = 1.0f;

    // Visual cue objects
    public GameObject Fill, Background, Text;

    // When true, we suspend the timer
    private bool cancel;

    void Start()
    {
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

    }

    void Update()
    {
        // If the participant hasn't fired the pinball, we decrement the time
        if (Timer >= 0.0f && !cancel)
        {
            Timer -= Time.deltaTime;
            GetComponent<Slider>().value = Timer;
        }
        else if (Timer <= 0.0f && (int)Timer != -1)
        {
            // When the timer expires, we set the timer to -1
            // TODO: Implement a better way of executing once in an update loop
            Timer = -1;
        }
    }

    public void Cancel()
    {
        // Suspends all animations if the user had fired the ball before timer expires
        LeanTween.cancel(Fill);
        LeanTween.cancel(Background);
        LeanTween.cancel(gameObject);
        LeanTween.cancel(Text);
        cancel = true;
    }
}
