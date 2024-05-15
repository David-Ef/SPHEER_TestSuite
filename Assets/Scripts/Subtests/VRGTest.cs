using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 Analysis of human vergence dynamics (2012)
 "stimulus scales in size with changes in depth to maintain a constant apparent size so that vergence eye movements are driven by image disparity alone"
 */

public class VRGTest : ITestRunner
{
    private bool _isRunning;
    private readonly int _repCount = 2; // Fewer because this is a special case of a (still) smooth pursuits for which we manipulated vergence distance in VRGPURTest

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
        return "Vergence";
    }

    public Dictionary<string, string> GetTrialDetails()
    {
        return new Dictionary<string, string> {
            { "startDistance", _trialData.startDistance.ToString() },
            { "endDistance", _trialData.endDistance.ToString() },
            { "duration", _trialData.duration.ToString() },
            { "tweening", _trialData.tweening.ToString() }
        };
    }

    // Parameter ranges
    private static readonly int[] _subDurationRange = { 12000, 20000 }; // msec
    private static readonly int[] _intertrialDelayRange = { 2000, 3000 }; // msec
    private static readonly float[] _depthRangeStart = { .3f, .5f }; // meter
    private static readonly float[] _depthRangeDistance = { .33f, 1f }; // meter
    private static readonly Ease[] _allowedTweening = {Ease.Linear, Ease.EaseInOutCubic, Ease.EaseInSine};
    // Static parameter
    private static readonly float _targetDiamDVA = 1;
    
    // trial details
    public struct TrialData
    {
        public float startDistance;
        public float endDistance;
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
        _target.AddComponent<ChangeWhenIgnored>();
        KeepConstantSizeDVA comp = _target.AddComponent<KeepConstantSizeDVA>();
        comp.constantDVA = _targetDiamDVA;
        _target.name = "[VRGTest] Gaze Target";
        _target.GetComponent<MeshRenderer>().material.color = Color.blue; // DEBUG?

        _trialData = new TrialData()
        {
            startDistance = Utils.GetRandomUnifInRange(_depthRangeStart),
            endDistance = Utils.GetRandomUnifInRange(_depthRangeDistance),
            duration = Utils.GetRandomUnifInRange(_subDurationRange),
            tweening = Ease.Linear
        };

        // Convergent reaction (moving towards the user, eyes moving towards the nasal bridge)
        if (ProtocolManager.instance.currentGeneralTrialIndex % 2 == 0)
        {
            float tmpDist = _trialData.startDistance; 
            
            _trialData.startDistance = _trialData.endDistance + tmpDist;
            _trialData.endDistance = tmpDist;
        }
        // Different interpolation method (tween)
        if (ProtocolManager.instance.currentGeneralTrialIndex / 2 % 2 == 0) // 1 out of 4 times
        {
            _trialData.tweening = Ease.EaseInOutCubic;
        }
    }

    public IEnumerator Run()
    {
        // TODO - Keep target size constant in DVA?
        // TODO - target position stays relative to the head? (new component: MoveInHeadSpace)
        // TODO - log trial info as they happen
        // TODO - log vergence estimated distance, gaze, pupil size
        
        _isRunning = true;
        _startTime = Utils.GetTimeStamp();

        Vector3 headPosition = ProtocolManager.userHead.position;
        Vector3 headForward = ProtocolManager.userHead.forward;

        Vector3 startPos = headPosition + headForward * _trialData.startDistance;
        Vector3 endPos = headPosition + headForward * _trialData.endDistance;

        Vector3 changePose = endPos - startPos;
        
        ProtocolManager.InfoLogger.Write($"[VRGTest] startPos={startPos}");
        ProtocolManager.InfoLogger.Write($"[VRGTest] endPos={endPos}");

        _target.transform.position = startPos;
        _target.SetActive(true);
        ProtocolManager.targetDirectionIndicator.SetCurrentTarget(_target.transform);

        _target.GetComponent<MeshRenderer>().material.color = Utils.preGazedTarget;

        ProtocolManager.InfoLogger.Write("[VRGTest] Fixation check");
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
        ProtocolManager.InfoLogger.Write("[VRGTest] Start");
        
        long startTrialTime = Utils.GetTimeStamp();
        long t = 0;
        while (t < _trialData.duration)
        {
            t = Utils.GetTimeStamp() - startTrialTime;
            _target.transform.position = Tweening.ChangeVector(t, startPos,
                changePose, _trialData.duration, _trialData.tweening);
                
            yield return null;
        }
        
        ProtocolManager.InfoLogger.Write("[VRGTest] End");
        ProtocolManager.targetDirectionIndicator.Unset();

        _target.SetActive(false);
        ProtocolManager.InfoLogger.Write("[VRGTest] ITI start");
        yield return new WaitForSecondsRealtime(Utils.GetRandomUnifInRange(_intertrialDelayRange) / 1000f);
        ProtocolManager.InfoLogger.Write("[VRGTest] ITI end");

        _endTime = Utils.GetTimeStamp();
        _isRunning = false;
    }

    public void Clear()
    {
        GameObject.Destroy(_target);
    }
}
