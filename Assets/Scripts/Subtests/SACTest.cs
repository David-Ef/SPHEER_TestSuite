using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SACTest : ITestRunner
{
    private bool _isRunning;
    private readonly int _repCount = 5;

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
        return "Saccade";
    }

    public Dictionary<string, string> GetTrialDetails()
    {
        return new Dictionary<string, string> {
            { "startDist", _trialData.startDist.ToString() },
            { "endDist", _trialData.endDist.ToString() },
            { "endPos", _trialData.endPos.ToString() }, // TODO CHECK OUTPUT OF VECT3D
        };
    }
    
    // Parameter ranges
    private static readonly float[] _AngularRange = { 5f, 25f }; // Degrees
    private static readonly float[] _depthRangeDistance = { .33f, 2f }; // Meters
    private static readonly int[] _intertrialDelayRange = { 2000, 3000 }; // msec
    // Static parameter
    private static readonly float _targetDiamDVA = 1;
    
    // Trial details
    public struct TrialData
    {
        public float startDist;
        public float endDist;
        public Vector3 endPos;
    }
    private TrialData _trialData;
    
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
    private Transform _starttarget;
    private Transform _saccTarget;

    public void SetUp()
    {
        // TODO - Varying vergence distance between the two target positions?

        _starttarget = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        KeepConstantSizeDVA comp = _starttarget.gameObject.AddComponent<KeepConstantSizeDVA>();
        comp.constantDVA = _targetDiamDVA;
        _starttarget.name = "[SACTest] Gaze start Target";
        _starttarget.GetComponent<MeshRenderer>().material.color = Color.red;

        _saccTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        comp = _saccTarget.gameObject.AddComponent<KeepConstantSizeDVA>();
        comp.constantDVA = _targetDiamDVA;
        _saccTarget.name = "[SACTest] Gaze sacc Target";
        _saccTarget.GetComponent<MeshRenderer>().material.color = Color.red;

        _starttarget.gameObject.SetActive(false);
        _saccTarget.gameObject.SetActive(false);
        
        _target = _starttarget;

        Vector3 endPos = Vector3.forward;
        float AngDistance = 0;
        while (AngDistance < _AngularRange[0]) // Loop to avoid angular distances below our set minimum
        {
            endPos = Utils.GetRandomAroundSphere(_AngularRange[1], _AngularRange[1], Vector3.forward);
            AngDistance = Vector3.Angle(Vector3.forward, endPos);
        }

        _trialData = new TrialData
        {
            startDist = Utils.GetRandomUnifInRange(_depthRangeDistance),
            endDist = Utils.GetRandomUnifInRange(_depthRangeDistance),
            endPos = endPos
        };
    }

    public IEnumerator Run()
    {
        _isRunning = true;
        _startTime = Utils.GetTimeStamp();

        /*
         * Init target at pos A, same position in front of the viewer
         *  Wait for fixation on start target
         * Show sacc target at pos B
         *  Wait for fixation on sacc target
         */

        _target = _starttarget;
        ProtocolManager.targetDirectionIndicator.SetCurrentTarget(_target);

        Transform head = ProtocolManager.userHead;

        _target.gameObject.SetActive(true);
        _target.position = head.position + head.forward * _trialData.startDist;

        _target.GetComponent<MeshRenderer>().material.color = Utils.preGazedTarget;

        ProtocolManager.InfoLogger.Write("[SACTest] Start position fixation check");
        ProtocolManager.InfoLogger.Write($"[SACTest] Start position: {_target.position.x}, {_target.position.y}, {_target.position.z}");
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
        ProtocolManager.InfoLogger.Write("[SACTest] Start");

        _target = _saccTarget;
        // ProtocolManager.targetDirectionIndicator.SetCurrentTarget(_target);
        ProtocolManager.targetDirectionIndicator.Unset();

        // Display new target at saccade end location
        _target.position = head.position + _trialData.endPos * _trialData.endDist;
        _target.gameObject.SetActive(true);
        
        ProtocolManager.InfoLogger.Write("[SACTest] End position fixation check");
        ProtocolManager.InfoLogger.Write($"[SACTest] Target position: {_target.position.x}, {_target.position.y}, {_target.position.z}");
        startGazeOnTarget = -1;
        while (true)
        {
            if (Gaze2TargetChckr.IsGaze2TargetDistanceWithinLimit(EyetrackerCtrl.gazePoint.CombWorldRay, _target.transform.position))
            {
                if (startGazeOnTarget < 0) // Just started looking at target (eye dir vector close enough to target centre)
                {
                    startGazeOnTarget = Utils.GetTimeStamp();
                }
                else if ((Utils.GetTimeStamp() - startGazeOnTarget) >= 100)
                {
                    break;
                }
            } else
            {
                startGazeOnTarget = -1;
            }
            yield return null;
        }

        ProtocolManager.InfoLogger.Write("[SACTest] End");
        ProtocolManager.targetDirectionIndicator.Unset();

        yield return new WaitForSecondsRealtime(2f);
        ProtocolManager.InfoLogger.Write("[SACTest] Extra time end");

        // Change target from red to blue to give feedback on fixation check
        _target.GetComponent<MeshRenderer>().material.color = Color.blue;

        _starttarget.gameObject.SetActive(false);
        _saccTarget.gameObject.SetActive(false);

        ProtocolManager.InfoLogger.Write("[SACTest] ITI start");
        yield return new WaitForSecondsRealtime(Utils.GetRandomUnifInRange(_intertrialDelayRange) / 1000f);
        ProtocolManager.InfoLogger.Write("[SACTest] ITI end");

        _endTime = Utils.GetTimeStamp();
        _isRunning = false;
    }

    public void Clear()
    {
        GameObject.Destroy(_starttarget.gameObject);
        GameObject.Destroy(_saccTarget.gameObject);
        _target = null;
    }
}
