using System;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using ViveSR.anipal.Eye;
using System.Threading;

public class EyetrackerCtrl : MonoBehaviour
{
    public static EyetrackerCtrl instance { get; private set; }
    public bool isReady { get; private set; }

    public static GazePoint gazePoint = new GazePoint();

    public SRanipal_Eye_Framework EyeFramework;

    //    private Thread thread;
    private void OnApplicationQuit()
    {
        SRanipal_Eye.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
        //        thread.Abort();
    }

    private void OnDisable()
    {
        SRanipal_Eye.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
        //        thread.Abort();
    }

    private void Awake()
    {
        instance = this;

        isReady = false;
    }

    private IEnumerator Start()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

        // https://forum.vive.com/topic/5897-getting-verbosedata-at-the-fastest-rate-possible/page/3/
        // https://forum.vive.com/topic/9341-vive-eye-tracking-at-120hz/
        // thread = new Thread(QueryEyeData); 
        // thread.Start();

        gazePoint = new GazePoint();

        if (!(bool)ConfigParser.instance["disableEyeTracking"]){
            
            while (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
                SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT)
            {
                yield return null;
            }

            // SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)Callback));
            SRanipal_Eye.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
            
            Debug.Log("Eye tracking ready.");
        }
        else
        {
            yield return null;
            Debug.Log("\"disableEyeTracking\" is on.");
        }

        isReady = true;
    }

    public bool isSampling;

    // Required class for IL2CPP scripting backend support
    internal class MonoPInvokeCallbackAttribute : System.Attribute
    {
        public MonoPInvokeCallbackAttribute() { }
    }

    [MonoPInvokeCallback]
    private static void EyeCallback(ref EyeData eye_data)
    {
        // private static void Callback(ref EyeData_v2 eye_data) {
        gazePoint = new GazePoint(eye_data);

        if (instance.isSampling)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            (Vector3 gazeIntersectionPoint, Vector2 vergenceEstimatedDistance) = EstimateVergenceDistance(
                gazePoint.LeftWorldRay.origin, gazePoint.RightWorldRay.origin,
                gazePoint.LeftWorldRay.direction, gazePoint.RightWorldRay.direction);

            // Cf ./ViveSR/Scripts/Eye/SRanipal_Eye.cs
            Vector3 meanBasePoint = gazePoint.CombGaze.gaze_origin_mm * 0.001f;
            Vector3 leftBasePoint = gazePoint.LeftGaze.gaze_origin_mm * 0.001f;
            Vector3 rightBasePoint = gazePoint.RightGaze.gaze_origin_mm * 0.001f;

            float leftPupilDiam = gazePoint.LeftGaze.pupil_diameter_mm;
            float rightPupilDiam = gazePoint.RightGaze.pupil_diameter_mm;

            Vector3 meanGazeDirection = gazePoint.CombGaze.gaze_direction_normalized;
            Vector3 leftGazeDirection = gazePoint.LeftGaze.gaze_direction_normalized;
            Vector3 rightGazeDirection = gazePoint.RightGaze.gaze_direction_normalized;

            meanGazeDirection.x *= -1;
            leftGazeDirection.x *= -1;
            rightGazeDirection.x *= -1;

            bool valC = gazePoint.valid(Utils.Lateralisation.comb);
            bool valL = gazePoint.valid(Utils.Lateralisation.left);
            bool valR = gazePoint.valid(Utils.Lateralisation.right);

            LogRecorderCtrl.instance.writerDict["ET"].Write(
                $"{gazePoint.data.timestamp},{instance.UnityTimeStamp}," +
                $"{instance.cameraPosition.x},{instance.cameraPosition.y},{instance.cameraPosition.z}," +
                $"{instance.cameraQuaternion.x},{instance.cameraQuaternion.y}," +
                $"{instance.cameraQuaternion.z},{instance.cameraQuaternion.w}," +
                $"{meanBasePoint.x},{meanBasePoint.y},{meanBasePoint.z}," +
                $"{meanGazeDirection.x},{meanGazeDirection.y},{meanGazeDirection.z}," +
                $"{leftBasePoint.x},{leftBasePoint.y},{leftBasePoint.z}," +
                $"{rightBasePoint.x},{rightBasePoint.y},{rightBasePoint.z}," +
                $"{leftGazeDirection.x},{leftGazeDirection.y},{leftGazeDirection.z}," +
                $"{rightGazeDirection.x},{rightGazeDirection.y},{rightGazeDirection.z}," +
                $"{gazeIntersectionPoint.x},{gazeIntersectionPoint.y},{gazeIntersectionPoint.z}," +
                $"{vergenceEstimatedDistance.x},{vergenceEstimatedDistance.y}," +
                $"{valC},{valL},{valR}", false
             );

            LogRecorderCtrl.instance.writerDict["PD"].Write($"{gazePoint.data.timestamp},{leftPupilDiam},{rightPupilDiam}", false);
        }
    }

    private void Update()
    {
        if (!(bool) ConfigParser.instance["disableEyeTracking"])
        {
            RetrieveCameraData();   
        }

        if (isSampling)
        {
            UserInteract _userInteract = UserInteract.instance;

            string HMDmessage = $"{gazePoint.data.timestamp},{UnityTimeStamp},";

            Vector3 tmpPos;

            foreach (Transform trans in new[] {
                ProtocolManager.userHead,
                _userInteract.Torso, _userInteract.Leg, _userInteract.Hand,
                ProtocolManager.instance.currentTestRunner.GetTargetTransform() })
            {
                tmpPos = trans.position;
                Quaternion tmpRot = trans.rotation;

                HMDmessage +=
                    $"{tmpPos.x},{tmpPos.y},{tmpPos.z}," +
                    $"{tmpRot.x},{tmpRot.y},{tmpRot.z},{tmpRot.w},";
            }

            LogRecorderCtrl.instance.writerDict["HMD"].Write(HMDmessage, false);
        }
    }

    public Vector3 cameraPosition;
    public Quaternion cameraQuaternion;

    [NonSerialized]
    public long UnityTimeStamp;

    public void RetrieveCameraData()
    {
        Camera mainCam = ProtocolManager.instance.camera;
        Transform camTrans = mainCam.transform;

        cameraPosition = camTrans.position;
        cameraQuaternion = camTrans.rotation;

        SRanipal_Eye.GetGazeRay(GazeIndex.LEFT, out gazePoint.LeftWorldRay, gazePoint.data);
        gazePoint.LeftWorldRay = new Ray(cameraPosition, mainCam.transform.TransformDirection(gazePoint.LeftWorldRay.direction));

        SRanipal_Eye.GetGazeRay(GazeIndex.RIGHT, out gazePoint.RightWorldRay, gazePoint.data);
        gazePoint.RightWorldRay = new Ray(cameraPosition, mainCam.transform.TransformDirection(gazePoint.RightWorldRay.direction));

        SRanipal_Eye.GetGazeRay(GazeIndex.COMBINE, out gazePoint.CombWorldRay, gazePoint.data);
        gazePoint.CombWorldRay = new Ray(cameraPosition, mainCam.transform.TransformDirection(gazePoint.CombWorldRay.direction));

        UnityTimeStamp = Utils.GetTimeStamp();
    }

    public class GazePoint
    {
        public GazePoint() // Empty ctor
        {
            data = new EyeData();
        }

        public GazePoint(EyeData gaze)
        {
            LeftGaze = gaze.verbose_data.left;
            RightGaze = gaze.verbose_data.right;
            CombGaze = gaze.verbose_data.combined.eye_data;
            data = gaze;
        }

        public readonly EyeData data;
        public SingleEyeData LeftGaze;
        public SingleEyeData RightGaze;
        public SingleEyeData CombGaze;

        public Ray LeftWorldRay;
        public Ray RightWorldRay;
        public Ray CombWorldRay;

        public bool valid(Utils.Lateralisation later)
        {
            switch (later)
            {
                case Utils.Lateralisation.left:
                    return LeftGaze.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_ORIGIN_VALIDITY);
                case Utils.Lateralisation.right:
                    return RightGaze.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_ORIGIN_VALIDITY);
                default:
                    return CombGaze.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_ORIGIN_VALIDITY);
            }
        }
    }

    private bool _isCalibrationRoutineFinished;
    public bool isCalibrationRoutineFinished => _isCalibrationRoutineFinished;
    public IEnumerator Calibrate()
    {
        _isCalibrationRoutineFinished = false;
        ProtocolManager protocolManager = ProtocolManager.instance;
        UserInteract userInteract = UserInteract.instance;

        int calibRep = 0;
        bool calibrationSuccess = false;
        while (!calibrationSuccess)
        {
            ProtocolManager.InfoLogger.Write($"[calib] rep {calibRep}");

            int calibReturnCode = SRanipal_Eye_API.LaunchEyeCalibration(IntPtr.Zero);
            print($"calibReturnCode: {calibReturnCode} == {(int)ViveSR.Error.WORK}");
            calibrationSuccess = calibReturnCode == (int)ViveSR.Error.WORK;

            ProtocolManager.InfoLogger.Write($"[calib] rep {calibRep} - outcome {calibrationSuccess}");

            calibRep++;

            if (!calibrationSuccess)
            {
                if (calibRep > 2)
                {
                    calibRep = 0;
                    protocolManager.ToggleMessage(true, "calibCooldown");
                    yield return new WaitUntil(() => userInteract.userInteracted);
                    protocolManager.ToggleMessage(false);
                }
                else
                {
                    protocolManager.ToggleMessage(true, "failedCal");
                    yield return new WaitUntil(() => userInteract.userInteracted);
                    protocolManager.ToggleMessage(false);
                }
            }
            else
            {
                protocolManager.ToggleMessage(true, "successCal");
                yield return new WaitUntil(() => userInteract.userInteracted);
                protocolManager.ToggleMessage(false);
            }
            yield return new WaitForSecondsRealtime(2f);
            protocolManager.ToggleMessage(false);
        }
        protocolManager.ToggleMessage(false);

        _isCalibrationRoutineFinished = true;
    }
    

    private static (Vector3, Vector2) EstimateVergenceDistance(Vector3 pLeftEye, Vector3 pRightEye, Vector3 rLeftEye, Vector3 rRightEye)
    {
        // DOI: 10.1016/j.procs.2022.09.221 - Duchowski et al. (KES 2022)

        Vector3 eyePosDiv = pLeftEye - pRightEye;

        // Dot products
        float dotLeftRay = Vector3.Dot(rLeftEye, rLeftEye);
        float dotRightRay = Vector3.Dot(rRightEye, rRightEye);
        float dotLeftRightRays = Vector3.Dot(rLeftEye, rRightEye);

        // Check denominator
        float denom = Mathf.Pow(dotLeftRightRays, 2) - dotLeftRay * dotRightRay;

        if (dotLeftRay < Mathf.Epsilon || Mathf.Abs(denom) < Mathf.Epsilon)
        {
            return (Vector3.zero, Vector2.zero);
        }

        float t2 = ((Vector3.Dot(eyePosDiv, rRightEye) * dotLeftRay) -
                    (Vector3.Dot(eyePosDiv, rLeftEye) * dotRightRay)) / denom;
        float t1 = (Vector3.Dot(eyePosDiv, rLeftEye) + t2 * dotLeftRightRays) / dotLeftRay;

        Vector3 gazeIntersectionPoint = ((pLeftEye - t2 * rLeftEye) + (pRightEye - t1 * rRightEye)) / 2f;
        Vector2 vergenceEstimatedDistance = new Vector2(t1, t2);

        return (gazeIntersectionPoint, vergenceEstimatedDistance);
    }
}
