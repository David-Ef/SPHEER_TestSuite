using System.Collections;
using UnityEngine;

public class ChangeWhenIgnored : MonoBehaviour
{

    public long maximumIgnoredDur = 500;
    private long sinceFixatedDuration = 0;

    private float step = 0;
    private int stepDirection = 1;

    private Material material;

    public float testFactor = 1f;
    public float enlargementFactor = 3f;
    public Color ignoredColour = Color.red;
    
    private bool isRunning;
    public bool isIgnored => isRunning;

    private float originalScale;
    private Color originalColour;

    void Start()
    {
        material = GetComponent<MeshRenderer>().material;

        originalScale = transform.localScale.x;
        originalColour = material.color;
    }

    void Update()
    {
        if (Gaze2TargetChckr.IsGaze2TargetDistanceWithinLimit(EyetrackerCtrl.gazePoint.CombWorldRay, transform.position))
        {
            sinceFixatedDuration = 0;
            isRunning = false;
        } else
        {
            sinceFixatedDuration += (long)(Time.deltaTime * 1000);
        }

        if (sinceFixatedDuration > maximumIgnoredDur && !isRunning)
        {
            step = 0; stepDirection = 1;
            StartCoroutine(playSaliencyPattern());
        }
    }

    private IEnumerator playSaliencyPattern()
    {
        isRunning = true;
        while (isRunning)
        {
            step += Time.deltaTime / testFactor * stepDirection;

            if (step < 0)
            {
                step = 0;
                stepDirection = 1;
            } else if (step > 1)
            {
                step = 1;
                stepDirection = -1;
            }

            float newScale = Tweening.ChangeFloat(step, originalScale, originalScale * enlargementFactor - originalScale, 1, Ease.EaseInQuad);
            transform.localScale = new Vector3(newScale, newScale, newScale);

            material.color = Color.Lerp(originalColour, ignoredColour, step);

            yield return null;
        }
        
        material.color = originalColour;
        transform.localScale = new Vector3(originalScale, originalScale, originalScale);
    }
}
