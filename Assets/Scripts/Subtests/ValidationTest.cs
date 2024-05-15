using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class ValidationTest : ITestRunner
{
    private bool _isRunning;
    private readonly int _repCount = 1;

    public int GetRepetitionCount()
    {
        return _repCount;
    }

    public bool GetIsRunning()
    {
        return _isRunning;
    }

    public string GetTestName()
    {
        return "ET validation";
    }

    public Dictionary<string, string> GetTrialDetails()
    {
        return new Dictionary<string, string> {
            { "ET validation", "5 pts x 2 dist" },
        };
    }
    
    private long _startTime;
    private long _endTime = -1;
    public long elapsedTime => Utils.GetTimeStamp() - _startTime;

    public long GetElapsedTime()
    {
        if (_endTime < 0)
        {
            return elapsedTime;
        }
        return _endTime - _startTime;
    }
    
    public Transform GetTargetTransform()
    {
        return _target.transform;
    }
    private Transform _target;

    private float FoV;
    private List<GameObject> targets;
    private int currentTargetIndex = -1;
    public Material DotMat;

    public enum Outcomes
    {
        undetermined, success, failure
    }

    public Outcomes lastOutcome { get; private set; }

    public void SetUp()
    {
        lastOutcome = Outcomes.undetermined;

        Transform camTrans = ProtocolManager.instance.camera.transform;
        FoV = ProtocolManager.instance.camera.fieldOfView;
        targets = new List<GameObject>();

        if (DotMat == null) {
            DotMat = Resources.Load<Material>("Materials/ValidationDots");
        }

        /* Generate validation dots to fixate
         * Validation dot matrix in two depths (10 points):
         *  o   o (18° apart)
         *    o
         *  o   o
         */
        List<Vector2> pointsLongLat = new List<Vector2>()
            {
                new Vector2(-9, 9), new Vector2(9, 9), // Top left, right
                new Vector2(0, 0), // Center
                new Vector2(-9, -9), new Vector2(9, -9), // Bottom left, right 
            };

        for (int iDepth = 0; iDepth < 2; iDepth++)
        {
            float dist = iDepth + 1;
            float FoVscale = 2f * Mathf.Tan(Mathf.Deg2Rad * FoV / 2f) * dist * .02f;

            int iPoint = 0;
            foreach (Vector2 point in pointsLongLat)
            {
                float lat = point.x * Mathf.Deg2Rad + Mathf.PI / 2f;
                float lon = point.y * Mathf.Deg2Rad;

                Vector3 unitVec = new Vector3
                {
                    x = Mathf.Sin(lat) * Mathf.Cos(lon),
                    y = Mathf.Sin(lat) * Mathf.Sin(lon),
                    z = Mathf.Cos(lat)
                };
                unitVec *= dist; // 1m away, then 2m away from camera

                Transform target = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                target.name = $"[VALTest] {iPoint++}_{iDepth}";
                target.position = camTrans.position + camTrans.rotation *
                    (Quaternion.FromToRotation(Vector3.forward, Vector3.left) * unitVec);
                target.localScale *= FoVscale;

                // Shader draws over everything else so the dots don't get hidden by environment
                target.GetComponent<MeshRenderer>().sharedMaterial = DotMat;
                target.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

                ChangeWhenIgnored feedback = target.gameObject.AddComponent<ChangeWhenIgnored>();
                feedback.testFactor = 1f;
                feedback.enlargementFactor = 5f;

                target.SetParent(camTrans);

                targets.Add(target.gameObject);
            }
        }

        _target = targets[0].transform;
        HideAllTargets();
    }

    private long maxTargetDispDur = 5000;
    private long durOnTargetToValidate = 500;
    
    public IEnumerator Run()
    {
        _isRunning = true;
        _startTime = Utils.GetTimeStamp();

        // Randomise validation target order
        List<int> targetIdx = Enumerable.Range(0, targets.Count).ToList();
        
        // This is updated to "failure" if the validation is not good
        //  do not use this enum var to check on the progress of the test (e.g., its completion)
        lastOutcome = Outcomes.success;

        List<int> randomTargetOrder = Enumerable.Range(0, targets.Count).ToList();
        Utils.ShuffleList(randomTargetOrder);

        for (int iTarget = 0; iTarget < targets.Count; iTarget++)
        {
            int iRandTarget = randomTargetOrder[iTarget];
            _target = targets[iRandTarget].transform;

            ProtocolManager.targetDirectionIndicator.SetCurrentTarget(_target);

            HideAllTargets();
            ShowTarget(iRandTarget);

            Debug.Log($"[{iTarget}] Val Pt {iRandTarget}");

            bool targetSuccess = false;

            long validPointOnsetTime = Utils.GetTimeStamp();
            long startGazeOnTarget = -1;

            while ((Utils.GetTimeStamp() - validPointOnsetTime) < maxTargetDispDur)
            {
                if (Gaze2TargetChckr.IsGaze2TargetDistanceWithinLimit(EyetrackerCtrl.gazePoint.CombWorldRay, _target.transform.position))
                {
                    if (startGazeOnTarget < 0) // Just started looking at target (eye dir vector close enough to target centre)
                    {
                        startGazeOnTarget = Utils.GetTimeStamp();
                    }
                    else if ((Utils.GetTimeStamp() - startGazeOnTarget) >= durOnTargetToValidate)
                    {
                        targetSuccess = true;
                        break;
                    }
                } else
                {
                    startGazeOnTarget = -1;
                }

                yield return null;
            }

            ProtocolManager.InfoLogger.Write($"[Validation] ({iTarget+1}/{targets.Count}) point {currentTargetIndex} - outcome {targetSuccess}" , true);

            if (!targetSuccess)
            {
                lastOutcome = Outcomes.failure;
                break;
            }
        }
        HideAllTargets();

        _isRunning = false;
    }

    public void ShowTarget(int iTarget)
    {
        targets[iTarget].SetActive(true);
        currentTargetIndex = iTarget;
    }

    public void HideAllTargets()
    {
        foreach (GameObject target in targets)
        {
            target.SetActive(false);
        }

        _endTime = Utils.GetTimeStamp();
        currentTargetIndex = -1;
    }

    public void Clear()
    {
        foreach (GameObject target in targets)
        {
            GameObject.Destroy(target);
        }
    }
}
