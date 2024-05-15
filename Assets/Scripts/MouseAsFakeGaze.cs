using UnityEngine;
using UnityEngine.InputSystem;

public class MouseAsFakeGaze : MonoBehaviour
{
    private Transform cameraTrans;

    private void Start()
    {
        cameraTrans = ProtocolManager.userHead;
    }

    void Update()
    {
        // Project mouse position on screen to world ray as 3D gaze ray
        Vector2 mousePosVP = Mouse.current.position.ReadValue();
        
        Vector3 point = Gaze2TargetChckr.cam.ScreenToWorldPoint(new Vector3(mousePosVP.x, mousePosVP.y, 1));
        Vector3 camPos = Gaze2TargetChckr.CamTrans.position;
        Ray mouseRay = new Ray(camPos, point - camPos);

        EyetrackerCtrl.gazePoint.CombWorldRay = mouseRay;
        
        // Control camera
    }
}
