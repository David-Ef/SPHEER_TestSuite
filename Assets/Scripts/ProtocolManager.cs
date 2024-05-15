using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class ProtocolManager : MonoBehaviour
{
    private static readonly Dictionary<string, ConfigParser.ConfigItem> defConfig =
        new Dictionary<string, ConfigParser.ConfigItem>
        {
            {"debug", new ConfigParser.ConfigItem(false)},

            {"bypassCalibration", new ConfigParser.ConfigItem(false)},
            {"bypassValidation", new ConfigParser.ConfigItem(false)},
            {"disableEyeTracking", new ConfigParser.ConfigItem(false)},

            {"bypassInstructions", new ConfigParser.ConfigItem(false)},
            {"bbypassUserAtCenterRoomCheck", new ConfigParser.ConfigItem(false)},

            {"maxDistanceToTarget", new ConfigParser.ConfigItem(2f)},

            {"startTrial", new ConfigParser.ConfigItem(0)}
        };

    public static bool debugMode => (bool)ConfigParser.instance["debug"];

    private EyetrackerCtrl _eyetrackerCtrl;
    private LogRecorderCtrl _logRecorderCtrl;
    private UserInteract _userInteract;
    public static TargetDirectionIndicator targetDirectionIndicator;
    public static LogRecorder InfoLogger => LogRecorderCtrl.instance.writerDict["ExpeDetails"];

    public static ProtocolManager instance;
    public new Camera camera;
    public static Transform userHead;

    public ITestRunner currentTestRunner { private set; get; }
    public string currentTestName => currentTestRunner.GetTestName();

    public int currentTestTrialIndex { get; private set; }
    public int currentGeneralTrialIndex { get; private set; }
    public int indexToResumeTestingAt = 0;

    public string GetCurrentTrialId()
    {
        return $"{currentGeneralTrialIndex}_{currentTestName}_{currentTestTrialIndex}";
    }

    private void Awake()
    {
        instance = this;
        _eyetrackerCtrl = GetComponent<EyetrackerCtrl>();
        targetDirectionIndicator = GetComponent<TargetDirectionIndicator>();
        userHead = camera.transform;

        _logRecorderCtrl = new LogRecorderCtrl();
        _logRecorderCtrl.Newrecord("debug");
        _logRecorderCtrl.Newrecord("ExpeDetails");

        // ConfigParser's ctor will set the instance var, so no need for property var
        new ConfigParser(defConfig);

#if MOCKHMD
        ConfigParser.instance.Set("disableEyeTracking", true);
#endif

        if ((bool)ConfigParser.instance["disableEyeTracking"])
        {
            gameObject.AddComponent<MouseAsFakeGaze>();
            gameObject.AddComponent<CameraController>();

            ConfigParser.instance.Set("bypasscalibration", true);
            ConfigParser.instance.Set("bypassValidation", true);
        }
        else
        {
            _eyetrackerCtrl.EyeFramework.enabled = true;
        }
    }

    IEnumerator Start()
    {
        _userInteract = UserInteract.instance;

        currentTestTrialIndex = 0;
        currentGeneralTrialIndex = 0;

        // Replicable randomness in case of interruption and need to rerun
        Utils.SetRandomWithSeed(LogRecorderCtrl.userId); // TODO - test

        LogRecorderCtrl.instance.writerDict["ExpeDetails"].Write($"[PM] Debug mode: {debugMode}");

        Gaze2TargetChckr.cam = camera;
        Gaze2TargetChckr.CamTrans = Gaze2TargetChckr.cam.transform;
        Gaze2TargetChckr.maxAngDist2Target = (float)ConfigParser.instance["maxDistanceToTarget"] * Mathf.Deg2Rad;

        /*
         * TODO
         *  Integrate/Test logger
         *  Integrate/Test message panel
         *  Test saved vergence info
         *  Check output data (see logRecordCtrl StartNewRecord)
         *  Log events in info log
         *  Grey screen for pupil size baseline - before every test
         *  CHECK
         *      log _trialData in all tests
         */

        List<ITestRunner> runners = new List<ITestRunner>()
        {
            new VRGTest(), new VORFixTest(), new PURTest(), new SACTest(), new VRGPURTest(), new VORWalkTest()
        };
        ValidationTest validationRunner = new ValidationTest();
        validationRunner.SetUp();

        List<int> testRunnerPlaylist;
        if (debugMode)
        {
            // In debug mode we present each test just their number of repetition ("_repCount")
            // Tests order is not randomised
            testRunnerPlaylist = Enumerable.Range(0, runners.Count).ToList();
        }
        else
        {
            testRunnerPlaylist = Enumerable.Range(0, runners.Count * 4).Select(i => i % runners.Count).ToList();
            Utils.ShuffleList(testRunnerPlaylist);
        }

        yield return new WaitUntil(() => _eyetrackerCtrl.isReady);

        ShowInInstructPanel("instructions");
        yield return new WaitUntil(() => _userInteract.userInteracted);
        HideInstructPanel();
        yield return new WaitUntil(() => !_userInteract.userInteracted);

        for (int iTest = 0; iTest < testRunnerPlaylist.Count; iTest++)
        {
            // Calibration + Validadition + break
            if (currentGeneralTrialIndex >= indexToResumeTestingAt)
            {
                bool showBreakMessage = iTest > 0 && iTest % (testRunnerPlaylist.Count / runners.Count) == 0; // Triggered at 25%, 50%, 75% protocol completion
                bool runCalibration = iTest % 4 == 0 && !(bool)ConfigParser.instance["bypassCalibration"]; // every four test (6 times)
                bool runValidation = !(bool)ConfigParser.instance["bypassValidation"]; // before every tests

                int repValidation = 0;

                while (runValidation)
                {
                    if (runCalibration)
                    {
                        // TODO Should be a loop to repeat until calibration is successful

                        ShowInInstructPanel("calibstart");
                        yield return new WaitUntil(() => _userInteract.userInteracted);
                        HideInstructPanel();
                        yield return new WaitUntil(() => !_userInteract.userInteracted);
                        // TODO https://docs.unity3d.com/Packages/com.unity.inputsystem@1.5/manual/Actions.html
                        //  Implement to be able to use "wasReleasedThisFrame" type of data

                        StartCoroutine(_eyetrackerCtrl.Calibrate());

                        InfoLogger.Write("[PM] Calibration start");
                        yield return new WaitUntil(() => _eyetrackerCtrl.isCalibrationRoutineFinished);
                        InfoLogger.Write("[PM] Calibration stop");

                        repValidation = 0;
                    }

                    // A validation always follows a calibration
                    //  But a calibration follows a validation, only when the latter failed
                    // TODO CHECK
                    // TODO log validation sequence - CHECK
                    currentTestRunner = validationRunner;

                    ShowInInstructPanel("validStart");
                    yield return new WaitUntil(() => _userInteract.userInteracted);
                    HideInstructPanel();

                    InfoLogger.Write("[PM] Validation start", true);
                    _logRecorderCtrl.StartNewRecord($"valid_{iTest}_{repValidation}");
                    StartCoroutine(validationRunner.Run());

                    yield return new WaitWhile(currentTestRunner.GetIsRunning);
                    InfoLogger.Write("[PM] Validation stop", true);
                    _logRecorderCtrl.StopRecord(0);
                    targetDirectionIndicator.Unset();

                    if (validationRunner.lastOutcome == ValidationTest.Outcomes.success)
                    {
                        runValidation = false;
                        ShowInInstructPanel("successVal");
                    }
                    else
                    {
                        runCalibration = ++repValidation == 3; // TODO TEST

                        ShowInInstructPanel("failedVal");
                    }
                    yield return new WaitUntil(() => (bool)ConfigParser.instance["bypassInstructions"] || _userInteract.userInteracted);
                    HideInstructPanel();
                }
            }

            currentTestRunner = runners[testRunnerPlaylist[iTest]];

            // Gray screen for pupil data baseline
            ToggleUniformlyGreyScreen(true);
            InfoLogger.Write($"[PM] Grey screen start");
            yield return new WaitForSeconds(3f);
            InfoLogger.Write($"[PM] Grey screen stop");
            ToggleUniformlyGreyScreen(false);

            for (int currentTestTrialRep = 0; currentTestTrialRep < currentTestRunner.GetRepetitionCount(); currentTestTrialRep++)
            {
                // In case of bug and had to restart
                currentGeneralTrialIndex++;
                if (indexToResumeTestingAt > currentGeneralTrialIndex) { continue; }

                // Force participant to move to center of room
                ShowInInstructPanel("WALK ON TO BLUE SQUARE AT THE CENTER OF THE ROOM"); // TODO CHECK
                // Wait until participant is at the center of the room (X: 0, Y: 0)
                yield return new WaitUntil(() =>
                {
                    return (bool)ConfigParser.instance["bypassUserAtCenterRoomCheck"] || new Vector2(userHead.position.x, userHead.position.z).magnitude < .2;
                });
                HideInstructPanel();

                ShowInInstructPanel($"<b>Instructions</b> regarding {currentTestRunner}");
                yield return new WaitUntil(() => _userInteract.userInteracted);
                HideInstructPanel();

                currentTestRunner.SetUp();

                InfoLogger.Write($"[PM] Starting test \"{currentTestName}\", iTest #{iTest}, iSubTest #{currentTestTrialRep}");

                Dictionary<string, string> trialDetails = currentTestRunner.GetTrialDetails();
                InfoLogger.Write("[PM] TrialDetails start");
                foreach (KeyValuePair<string, string> detail in trialDetails)
                {
                    InfoLogger.Write($"[PM] {detail.Key}: {detail.Value}");
                }
                InfoLogger.Write("[PM] TrialDetails end");

                _logRecorderCtrl.StartNewRecord($"{currentTestName}_{iTest}_{currentTestTrialRep}");
                StartCoroutine(currentTestRunner.Run());

                yield return new WaitWhile(currentTestRunner.GetIsRunning);
                _logRecorderCtrl.StopRecord(0);

                targetDirectionIndicator.Unset();

                currentTestRunner.Clear();
            }
            _logRecorderCtrl.FlushAll();
        }

        _logRecorderCtrl.CloseAll();
        Quit();
    }

    [FormerlySerializedAs("instrutPanel")] public GameObject instructPanel;
    private TextMeshPro _msgHolder;
    public void ToggleMessage(bool state, string message = "")
    {
        instructPanel.SetActive(state);
        if (_msgHolder == null)
        {
            _msgHolder = instructPanel.transform.GetChild(0).GetComponent<TextMeshPro>();
        }

        _msgHolder.text = Utils.messages.ContainsKey(message) ? Utils.messages[message] : message;
    }

    public void ShowInInstructPanel(string message)
    {
        InfoLogger.Write($"[Instruct] Show: {message}");

        targetDirectionIndicator.SetCurrentTarget(instructPanel.transform);
        ToggleMessage(true, message);
    }

    public void HideInstructPanel()
    {
        InfoLogger.Write("[Instruct] Hide");

        targetDirectionIndicator.SetCurrentTarget(null);
        instructPanel.SetActive(false);
    }

    public void ToggleUniformlyGreyScreen(bool state)
    {
        InfoLogger.Write($"[PM] GreyScreen: {state}");
        camera.cullingMask = state ? 0 : 1;
    }

    public static void Quit()
    {
        print("Quitting gracefully");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}
