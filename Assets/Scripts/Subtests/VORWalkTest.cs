using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class VORWalkTest : ITestRunner
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
        return "VOR walking";
    }

    public Dictionary<string, string> GetTrialDetails()
    {
        return new Dictionary<string, string> {
            { "targetPathPoints", _trialData.pointsToString() },
            { "height", _trialData.height.ToString() }
        };
    }

    // Static parameter
    private static readonly float _targetDiamDVA = 2;

    // Trial details
    public struct TrialData
    {
        public List<Vector2> targetPathPoints;
        public float height;
        
        public string pointsToString()
        {
            StringBuilder sOut = new StringBuilder();
            foreach (Vector2 point in targetPathPoints)
            {
                sOut.Append(point.ToString());
            }
            return sOut.ToString();
        }
    }
    
    private TrialData _trialData;

    private GameObject _target;
    public GameObject target => _target;
    
    private GameObject _floorMark;
    
    private ChangeWhenIgnored compCWI;
    
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
        KeepConstantSizeDVA compKCS = _target.AddComponent<KeepConstantSizeDVA>();
        compKCS.constantDVA = _targetDiamDVA;
        compKCS.setScale();

        compCWI = _target.AddComponent<ChangeWhenIgnored>();
        compCWI.enlargementFactor = 2;
        _target.name = "[VORWalkTest] Gaze Target";
        _target.GetComponent<MeshRenderer>().material.color = Color.blue;
        
        _floorMark = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _floorMark.transform.rotation = Quaternion.Euler(new Vector3(90, 0, 0));
        _floorMark.transform.localScale *= .3f;
        _floorMark.GetComponent<MeshRenderer>().material.color = Color.blue;
        _floorMark.name = "[VORWalkTest] Floor mark";
        
        _target.SetActive(false);
        _floorMark.SetActive(false);

        _trialData = new TrialData {
            targetPathPoints = new List<Vector2>(){
                new Vector2(-1.4f, 1.4f),
                new Vector2(-1.4f, -1.4f),
                new Vector2(1.4f, -1.4f),
                new Vector2(1.4f, 1.4f)
            },
            height= 1.7f
        };

        Utils.ShuffleList(_trialData.targetPathPoints);
    }

    public IEnumerator Run()
    {
        _isRunning = true;
        _startTime = Utils.GetTimeStamp();

        Transform userTrans = ProtocolManager.userHead;

        Transform targTrans = _target.transform;
        targTrans.position = new Vector3(_trialData.targetPathPoints[0].x, _trialData.height, _trialData.targetPathPoints[0].y);

        ProtocolManager.InfoLogger.Write("[VORWalkTest] Fixation check");
        
        yield return new WaitWhile(() => compCWI.isIgnored);
        
        ProtocolManager.InfoLogger.Write("[VORWalkTest] Start");

        for (int iPathPoint = 1; iPathPoint < _trialData.targetPathPoints.Count; iPathPoint++)
        {
            Vector3 startPos = new Vector3(_trialData.targetPathPoints[iPathPoint - 1].x, _trialData.height, _trialData.targetPathPoints[iPathPoint - 1].y);
            Vector3 endPos = new Vector3(_trialData.targetPathPoints[iPathPoint].x, _trialData.height, _trialData.targetPathPoints[iPathPoint].y);
            
            ProtocolManager.InfoLogger.Write($"[VORWalkTest] Point {iPathPoint} - {startPos} to {endPos}");
            
            // Show mark on the floor to move to
            _floorMark.transform.position = new Vector3(startPos.x, 0.025f, startPos.z);
            _floorMark.SetActive(true);
            ProtocolManager.targetDirectionIndicator.SetCurrentTarget(_floorMark.transform);

            // Wait until participant is standing on floor mark
            yield return new WaitUntil(() =>
            {
                Vector3 userPos = userTrans.position;
                Vector2 diff = new Vector2(userPos.x, userPos.z) - new Vector2(startPos.x, startPos.z);
                return diff.magnitude < .2; // Within 20cm
            });
            _target.SetActive(true);
            _floorMark.SetActive(false);
            
            // Show target at end position
            targTrans.position = endPos;
            ProtocolManager.targetDirectionIndicator.SetCurrentTarget(targTrans);

            // Wait until participant is close enough to target
            yield return new WaitUntil(() =>
            {
                Vector3 userPos = userTrans.position;
                Vector2 diff = new Vector2(userPos.x, userPos.z) - new Vector2(endPos.x, endPos.z);
                return diff.magnitude < .4;
            });
            _target.SetActive(false);

            ProtocolManager.InfoLogger.Write($"[VORWalkTest] Point {iPathPoint} end");
        }
        
        ProtocolManager.InfoLogger.Write("[VORWalkTest] End");

        _target.SetActive(false);

        _endTime = Utils.GetTimeStamp();
        _isRunning = false;
    }

    public void Clear()
    {
        GameObject.Destroy(_target);
        GameObject.Destroy(_floorMark);
    }
}
