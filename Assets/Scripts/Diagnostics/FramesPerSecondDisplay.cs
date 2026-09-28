using Cysharp.Text;
using TMPro;
using UnityEngine;

namespace Diagnostics
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class FramesPerSecondDisplay : MonoBehaviour
    {
        [SerializeField, Min(0.1f)]
        private float displayUpdateInterval = 0.5f;

        private TMP_Text framesPerSecondText;
        private float accumulatedFrameTime;
        private int accumulatedFrameCount;

        private void Awake()
        {
            framesPerSecondText = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            accumulatedFrameTime += Time.unscaledDeltaTime;
            accumulatedFrameCount++;

            if (accumulatedFrameTime < displayUpdateInterval)
            {
                return;
            }

            var framesPerSecond =
                accumulatedFrameCount / accumulatedFrameTime;

            framesPerSecondText.SetTextFormat(
                "{0:F1} FPS",
                framesPerSecond
            );

            accumulatedFrameTime = 0f;
            accumulatedFrameCount = 0;
        }

        private void OnValidate()
        {
            displayUpdateInterval =
                Mathf.Max(0.1f, displayUpdateInterval);
        }
    }
}