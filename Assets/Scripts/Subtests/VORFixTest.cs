using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VORFixTest : ITestRunner
{
    public bool _isRunning;
    private readonly int _repCount = 2;

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
        return "VOR fixating";
    }

    public Dictionary<string, string> GetTrialDetails()
    {
        return new Dictionary<string, string> {
            { "distance", _trialData.distance.ToString() },
            { "duration", _trialData.duration.ToString() }
        };
    }

    // Parameter ranges
    private static readonly int[] _subDurationRange = { 1500, 2500 }; // msec
    private static readonly float[] _depthRangeDistance = { .33f, 2f }; // meter
    private static readonly int[] _intertrialDelayRange = { 2000, 3000 }; // msec // TODO - move to ProtocolManager
    // Static parameter
    private static readonly float _targetDiamDVA = 1;

    // Trial details
    public struct TrialData
    {
        public float distance;
        public float duration;
    }
    private TrialData _trialData;

    private GameObject _target;
    
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
        KeepConstantSizeDVA comp = _target.AddComponent<KeepConstantSizeDVA>();
        comp.constantDVA = _targetDiamDVA;
        _target.name = "[VORFixTest] Gaze Target";
        _target.GetComponent<MeshRenderer>().material.color = Color.blue; // DEBUG

        _trialData = new TrialData() {
            distance = Utils.GetRandomUnifInRange(_depthRangeDistance),
            duration= Utils.GetRandomUnifInRange(_subDurationRange) / 1000f
        };
    }

    public IEnumerator Run()
    {
        _isRunning = true;
        _startTime = Utils.GetTimeStamp();
        
        ProtocolManager.instance.camera.transform.SetParent(null);

        Vector3 headPosition = ProtocolManager.userHead.position;
        Vector3 headForward = ProtocolManager.userHead.forward;
        
        Vector3 targetPos = headPosition + headForward * _trialData.distance;

        Transform targetTrans = _target.transform;
        _target.SetActive(true);
        targetTrans.position = targetPos;
        ProtocolManager.targetDirectionIndicator.SetCurrentTarget(targetTrans);

        _target.GetComponent<MeshRenderer>().material.color = Utils.preGazedTarget;

        ProtocolManager.InfoLogger.Write("[VORFixTest] Fixation check");
        long startGazeOnTarget = -1;
        while (true)
        {
            if (Gaze2TargetChckr.IsGaze2TargetDistanceWithinLimit(EyetrackerCtrl.gazePoint.CombWorldRay, targetTrans.position))
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
        ProtocolManager.InfoLogger.Write("[VORFixTest] Start");
        
        yield return new WaitForSeconds(_trialData.duration);
        
        ProtocolManager.InfoLogger.Write("[VORFixTest] End");
        ProtocolManager.targetDirectionIndicator.Unset();

        _target.SetActive(false);
        yield return new WaitForSecondsRealtime(Utils.GetRandomUnifInRange(_intertrialDelayRange) / 1000f);

        _endTime = Utils.GetTimeStamp();
        _isRunning = false;
    }

    public void Clear()
    {
        GameObject.Destroy(_target);
    }
}
