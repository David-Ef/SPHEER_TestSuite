using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRGPURTest : ITestRunner
{
    private bool _isRunning;
    private readonly int _repCount = 6;

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
        return "Vergence + smooth pursuit";
    }

    public Dictionary<string, string> GetTrialDetails()
    {
        return new Dictionary<string, string> {
            { "startPos", _trialData.startRot.ToString() },
            { "endPos", _trialData.endRot.ToString() },
            { "startDist", _trialData.startDist.ToString() },
            { "endDist", _trialData.endDist.ToString() },
            { "duration", _trialData.duration.ToString() },
            { "tweening", _trialData.tweening.ToString() },
        };
    }

    // Parameter ranges
    private static readonly int[] _subDurationRange = { 2000, 5000 }; // msec
    private static readonly int[] _intertrialDelayRange = { 2000, 5000 }; // msec
    private static readonly float[] _depthRangeDistance = { .33f, 1f }; // meter
    private static readonly Ease[] _allowedTweening = { Ease.Linear, Ease.EaseInOutCubic }; //, Ease.EaseOutBounce};
    // Static parameter
    private static readonly float _targetDiamDVA = 1;

    // trial details
    public struct TrialData
    {
        public Quaternion startRot;
        public Quaternion endRot;
        public float startDist;
        public float endDist;
        public int duration;
        public Ease tweening;
    }
    private TrialData _trialData;

    private Ease _tweening;
    
    private GameObject _target;
    public GameObject target => _target;

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
        return target.transform;
    }

    public void SetUp()
    {
        _target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _target.SetActive(false);
        _target.AddComponent<KeepConstantSizeDVA>().constantDVA = _targetDiamDVA;
        _target.name = "[VRGPURTtest] Gaze Target";
        _target.GetComponent<MeshRenderer>().material.color = Color.blue;
        ChangeWhenIgnored feedback = _target.gameObject.AddComponent<ChangeWhenIgnored>();
        feedback.testFactor = 1f;
        feedback.enlargementFactor = 5f;

        _trialData = new TrialData
        {
            // Choose two random rotations facing the viewer, as a unit vec it will be projected at a constant distance to the head with "KeepConstantDistToTarget"
            startRot = Quaternion.Slerp(Quaternion.identity, Random.rotationUniform, .1f),
            endRot = Quaternion.Slerp(Quaternion.identity, Random.rotationUniform, .5f),
            startDist = Utils.GetRandomUnifInRange(_depthRangeDistance),
            endDist = Utils.GetRandomUnifInRange(_depthRangeDistance),
            duration = Utils.GetRandomUnifInRange(_subDurationRange),
            tweening = _allowedTweening[ProtocolManager.instance.currentGeneralTrialIndex % _allowedTweening.Length]
        };
        // Distance reported in OnGUI (VRG dist estimation) will be less than set here because OnGUI measures from the eyes and here its from the head's centre
    }

    public IEnumerator Run()
    {
        _isRunning = true;
        _startTime = Utils.GetTimeStamp();
        
        // TODO - add cue (arrow) to guide to target at the start of the trial 
        // TODO - start target movement animation after looking at it for 1s
        //  Negative feedback if stop looking at it

        Vector3 headPosition = ProtocolManager.userHead.position;
        Vector3 headForward = ProtocolManager.userHead.forward;

        Vector3 startPos = headPosition + _trialData.startRot * headForward * _trialData.startDist;
        Vector3 endPos = headPosition + _trialData.endRot * headForward * _trialData.endDist;

        Vector3 changePose = endPos - startPos;

        target.transform.position = startPos;
        target.SetActive(true);
        ProtocolManager.targetDirectionIndicator.SetCurrentTarget(target.transform);

        target.GetComponent<MeshRenderer>().material.color = Utils.preGazedTarget;

        ProtocolManager.InfoLogger.Write("[VRGPURTest] Fixation check");
        long startGazeOnTarget = -1;
        while (true)
        {
            if (Gaze2TargetChckr.IsGaze2TargetDistanceWithinLimit(EyetrackerCtrl.gazePoint.CombWorldRay, target.transform.position))
            {
                if (startGazeOnTarget < 0) // Just started looking at target (eye dir vector close enough to target centre)
                {
                    startGazeOnTarget = Utils.GetTimeStamp();
                }
                else if ((Utils.GetTimeStamp() - startGazeOnTarget) >= 250)
                {
                    break;
                }
            } else
            {
                startGazeOnTarget = -1;
            }
            yield return null;
        }

        target.gameObject.GetComponent<ChangeWhenIgnored>().enabled = false;
        target.GetComponent<MeshRenderer>().material.color = Color.blue;
        ProtocolManager.InfoLogger.Write("[VRGPURTest] Start");
        
        long startTrialTime = Utils.GetTimeStamp();
        long t = 0;
        while (t < _trialData.duration)
        {
            t = Utils.GetTimeStamp() - startTrialTime;
            target.transform.position = Tweening.ChangeVector(t, startPos,changePose, _trialData.duration, _trialData.tweening);
                
            yield return null;
        }
        
        ProtocolManager.InfoLogger.Write("[VRGPURTest] End");
        ProtocolManager.targetDirectionIndicator.Unset();

        target.SetActive(false);
        yield return new WaitForSecondsRealtime(Utils.GetRandomUnifInRange(_intertrialDelayRange) / 1000f);

        _endTime = Utils.GetTimeStamp();
        _isRunning = false;
    }

    public void Clear()
    {
        GameObject.Destroy(_target);
    }
}
