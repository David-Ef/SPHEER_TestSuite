using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITestRunner
{
    public int GetRepetitionCount();
    public bool GetIsRunning();
    public string GetTestName();
    // TODO - Add get duration function -- returns current elapsed time if not finished
    public long GetElapsedTime();
    public Dictionary<string, string> GetTrialDetails();
    public Transform GetTargetTransform();
    public void SetUp();
    public IEnumerator Run();
    public void Clear();
}
