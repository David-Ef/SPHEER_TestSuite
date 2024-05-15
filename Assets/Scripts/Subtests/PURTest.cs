using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/*
*  https://catlikecoding.com/unity/tutorials/curves-and-splines/
*  https://assetstore.unity.com/packages/tools/utilities/bg-curve-59043
*/

public class PURTest : ITestRunner
{
    private bool _isRunning;
    private readonly int _repCount = 4;

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
        return "Smooth pursuit";
    }

    public Dictionary<string, string> GetTrialDetails()
    {
        return new Dictionary<string, string> {
            { "startPos", _trialData.startRot.ToString() },
            { "endPos", _trialData.endRot.ToString() },
            { "distance", _trialData.distance.ToString() },
            { "duration", _trialData.duration.ToString() },
            { "tweening", _trialData.tweening.ToString() }
        };
    }

    // Parameter ranges
    private static readonly int[] _subDurationRange = { 2000, 7500 }; // msec
    private static readonly int[] _intertrialDelayRange = { 2000, 5000 }; // msec
    private static readonly float[] _depthRangeDistance = { .33f, 1.5f }; // meter // TODO look at these values across all tests
    private static readonly Ease[] _allowedTweening = {Ease.Linear, Ease.EaseInOutCubic}; // TODO - more?
    // Static parameter
    private static readonly float _targetDiamDVA = 1; // TODO - Move to ProtocolManager to control globally
    
    // Trial details
    public struct TrialData
    {
        public Quaternion startRot;
        public Quaternion endRot;
        public float distance;
        public int duration;
        public Ease tweening;
    }
    private TrialData _trialData;

    private Ease _tweening;
    
    private GameObject _target;
    private KeepConstantDistToTarget compD;
    
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

    public void SetUp()
    {
        _target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _target.SetActive(false);
        compD = _target.AddComponent<KeepConstantDistToTarget>();
        _target.AddComponent<ChangeWhenIgnored>();
        KeepConstantSizeDVA comp = _target.AddComponent<KeepConstantSizeDVA>();
        comp.constantDVA = _targetDiamDVA;
        _target.name = "[PURTest] Gaze Target";
        _target.GetComponent<MeshRenderer>().material.color = Color.blue;

        Transform head = ProtocolManager.userHead;

        _trialData = new TrialData()
        {
            // Choose two random rotations facing the viewer, as a unit vec it will be projected at a constant distance to the head with "KeepConstantDistToTarget"
            startRot = Quaternion.Slerp(Quaternion.identity, Random.rotationUniform, .1f),
            endRot = Quaternion.Slerp(Quaternion.identity, Random.rotationUniform, .5f),
            distance = Utils.GetRandomUnifInRange(_depthRangeDistance),
            duration = Utils.GetRandomUnifInRange(_subDurationRange),
            tweening = _allowedTweening[ProtocolManager.instance.currentGeneralTrialIndex % _allowedTweening.Length]
        };
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

        Vector3 startPos = headPosition + _trialData.startRot * headForward * _trialData.distance;
        Vector3 endPos = headPosition + _trialData.endRot * headForward * _trialData.distance;

        Vector3 changePose = endPos - startPos;
        compD.distance = _trialData.distance;

        _target.transform.position = startPos;
        _target.SetActive(true);

        ProtocolManager.targetDirectionIndicator.SetCurrentTarget(_target.transform);

        _target.GetComponent<MeshRenderer>().material.color = Utils.preGazedTarget;

        ProtocolManager.InfoLogger.Write("[PURTest] Fixation check");
        long startGazeOnTarget = -1;
        while (true)
        {
            if (Gaze2TargetChckr.IsGaze2TargetDistanceWithinLimit(EyetrackerCtrl.gazePoint.CombWorldRay, _target.transform.position))
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

        _target.GetComponent<MeshRenderer>().material.color = Color.blue;
        ProtocolManager.InfoLogger.Write("[PURTest] Start");

        long startTrialTime = Utils.GetTimeStamp();
        long t = 0;
        while (t < _trialData.duration)
        {
            t = Utils.GetTimeStamp() - startTrialTime;
            _target.transform.position = Tweening.ChangeVector(t, startPos, changePose, _trialData.duration, _trialData.tweening);

            yield return null;
        }
        ProtocolManager.InfoLogger.Write("[PURTest] End");
        ProtocolManager.targetDirectionIndicator.Unset();

        _target.SetActive(false);
        ProtocolManager.InfoLogger.Write("[PURTest] ITI start");
        yield return new WaitForSecondsRealtime(Utils.GetRandomUnifInRange(_intertrialDelayRange) / 1000f);
        ProtocolManager.InfoLogger.Write("[PURTest] ITI end");

        _endTime = Utils.GetTimeStamp();
        _isRunning = false;
    }

    public void Clear()
    {
        GameObject.Destroy(_target);
    }
}
