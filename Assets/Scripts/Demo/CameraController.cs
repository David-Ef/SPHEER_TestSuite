using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    private static CameraController _instance;
    public static CameraController instance => _instance;

    public delegate void inputCallback(bool pressed);

    private Key[] KeyCodesUsed;
    public Dictionary<Key, bool> isPressed = new Dictionary<Key, bool>(); // Pressed, Released
    private Dictionary<Key, List<inputCallback>> InputAction = new Dictionary<Key, List<inputCallback>>();

    public const float camYpos = 1.8f;
    public float camRotSpeed = 100f;
    public float camTransSpeed = 10f;

    private Transform maincamTrans;
    
    public bool allReleased
    {
        get
        {
            foreach (Key keycode in KeyCodesUsed)
            {
                if (isPressed[keycode]) return false;
            }
            return true;
        }
    }

    private void OnEnable()
    {
        _instance = this;
        maincamTrans = ProtocolManager.userHead;
        maincamTrans.position = new Vector3(0, camYpos, 0);
        maincamTrans.rotation = Quaternion.identity;
        maincamTrans.Rotate(Vector3.right, 20);
        
        camPosWin = new Vector3[maxCamWin];
        camRotWin = new Vector3[maxCamWin];
        for (int i = 0; i < maxCamWin; i++)
        {
            camPosWin[i].y = camYpos;
            // camRotWin[i].x = -10f;
        }
        
        KeyCodesUsed = new Key[]
        {
            // Crouch down (move camera up/down)
            Key.LeftCtrl,
            // Move forward
            Key.Z,
            // Move backward
            Key.S,
            // Rotate left
            Key.Q,
            // Rotate right
            Key.D,
            // Strafe left
            Key.A,
            // Strafe right
            Key.E
        };

        foreach (Key keycode in KeyCodesUsed)
        {
            isPressed.Add(keycode, false);
            InputAction.Add(keycode, new List<inputCallback>());
            
            // Tie logging to input events
            // InputAction[keycode].Add((state) => {});
        }
        
        // Define input actions/effects
        //  Crouch
        InputAction[Key.LeftCtrl].Add((state) =>
        {
            Vector3 camPos = maincamTrans.position;
            camPos.y = state ? .6f: camYpos;
            maincamTrans.position = camPos;
        });
        //  Move forward
        InputAction[Key.Z].Add(state =>
        {
            if (state)
            {
                Vector3 oldPos = maincamTrans.position;
                float Ypos = oldPos.y; // Keep Y position that can bee .6f or camYpos
                Vector3 newPos = oldPos + maincamTrans.forward * camTransSpeed * Time.deltaTime;
                newPos.y = Ypos;
                maincamTrans.position = newPos;
            }
        });
        //  Move backward
        InputAction[Key.S].Add(state =>
        {
            if (state)
            {
                Vector3 oldPos = maincamTrans.position;
                float Ypos = oldPos.y;
                Vector3 newPos = oldPos - maincamTrans.forward * camTransSpeed * Time.deltaTime;
                newPos.y = Ypos;
                maincamTrans.position = newPos;
            }
        });
        //  Rotate right
        InputAction[Key.Q].Add(state =>
        {
            if (state)
            {
                maincamTrans.Rotate(Vector3.up, camRotSpeed * Time.deltaTime, Space.World);
            }
        });
        //  Rotate left
        InputAction[Key.D].Add(state =>
        {
            if (state)
            {
                maincamTrans.Rotate(Vector3.up, -camRotSpeed * Time.deltaTime, Space.World);
            }
        });
        //  Strafe right
        InputAction[Key.A].Add(state =>
        {
            if (state)
            {
                Vector3 oldPos = maincamTrans.position;
                float Ypos = oldPos.y;
                Vector3 newPos = oldPos + maincamTrans.right * camTransSpeed * Time.deltaTime;
                newPos.y = Ypos;
                maincamTrans.position = newPos;
            }
        });
        //  Strafe left
        InputAction[Key.E].Add(state =>
        {
            if (state)
            {
                Vector3 oldPos = maincamTrans.position;
                float Ypos = oldPos.y;
                Vector3 newPos = oldPos - maincamTrans.right * camTransSpeed * Time.deltaTime;
                newPos.y = Ypos;
                maincamTrans.position = newPos;
            }
        });
        //  Interaction with environment (mouse click
    }
    
    // Smoother camera control with averaging window
    Vector3 getAverageVec3(Vector3[] arr)
    {
        Vector3 outvec = Vector3.zero;
        for (int i = 0; i < maxCamWin; i++)
        {
            outvec += arr[i];
        }
        return outvec / maxCamWin;
    }
    private int iCamWin = 0;
    private int maxCamWin = 4;
    private Vector3[] camPosWin;
    private Vector3[] camRotWin;

    private void Update()
    {
        // // Mouse movement -> camera rotation
        // mousePos.x += Mouse.current.delta.x.ReadValue()  * camRotSpeed*2 * Time.deltaTime;
        // mousePos.y -= Mouse.current.delta.y.ReadValue() * camRotSpeed * Time.deltaTime;
        // mousePos.y = Mathf.Clamp(mousePos.y, -75, 75);
        // Vector3 newCamRot = new Vector3(mousePos.y, mousePos.x, 0f);
        
        foreach (Key keycode in KeyCodesUsed)
        {
            // bool hasChanged = false;
            bool oldStateIsPressed = isPressed[keycode];
            bool isDown = Keyboard.current[keycode].isPressed;
            bool isUp = Keyboard.current[keycode].wasReleasedThisFrame;
            // bool hasChanged = inputPresssed[keycode] == isDown;

            if (isDown && !oldStateIsPressed)
            {
                isPressed[keycode] = true;
                // hasChanged = true;
            } else if (isUp) // && oldStateIsPressed)
            {
                isPressed[keycode] = false;
                // hasChanged = true;
            }
            
            // print($"{keycode} - {oldStateIsPressed}, {isDown}, {isUp}, {isPressed[keycode]}");

            // if (!hasChanged && !isPressed[keycode]) 
            // // if (!hasChanged)
            // {
            //     continue;
            // }
            
            // Continue to invoke attached action(s) as long as the key is pressed
            if (InputAction.ContainsKey(keycode))
            {
                foreach (inputCallback func in InputAction[keycode])
                {
                    func(isPressed[keycode]);
                }
            }
        }

        Vector3 newCamRot = maincamTrans.eulerAngles;
        Vector3 newCamPos = maincamTrans.position;
        
        newCamPos.x = Mathf.Clamp(newCamPos.x, -1.85f, 1.85f);
        newCamPos.z = Mathf.Clamp(newCamPos.z, -2f, 2f);
        
        iCamWin = (++iCamWin) % maxCamWin;

        maincamTrans.position = (getAverageVec3(camPosWin) + newCamPos) /2;
        maincamTrans.eulerAngles = newCamRot; // (getAverageVec3(camRotWin) + newCamRot) /2;
        // maincamTrans.eulerAngles = (getAverageVec3(camRotWin) - new Vector3(90f,0,0) + newCamRot) /2;
        
        camPosWin[iCamWin] = newCamPos;
        camRotWin[iCamWin] = newCamRot;// + new Vector3(90f,0,0);

    }
}
