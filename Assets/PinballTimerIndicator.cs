using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PinballTimerIndicator : MonoBehaviour
{
    public float Timer = 1.0f;
    public GameObject Fill, Background;

    private bool cancel;

    void Start()
    {
        LeanTween.alpha(Fill.GetComponent<RectTransform>(), 0.0f, 0.5f).setDelay(Timer + 0.5f);
        LeanTween.alpha(Background.GetComponent<RectTransform>(), 0.0f, 0.5f).setDelay(Timer + 0.5f);
        LeanTween.scale(GetComponent<RectTransform>(), Vector3.zero, 1.0f)
            .setEaseInElastic().setDelay(Timer);
    }

    void Update()
    {
        if (Timer >= 0.0f && !cancel)
        {
            Timer -= Time.deltaTime;
            GetComponent<Slider>().value = Timer;
        }
        else if (Timer <= 0.0f && (int)Timer != -1)
        {
            
            Timer = -1;
        }
    }

    public void Cancel()
    {
        LeanTween.cancel(Fill);
        LeanTween.cancel(Background);
        LeanTween.cancel(gameObject);
        cancel = true;
    }
}
