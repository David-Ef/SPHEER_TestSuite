using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/*
 *  !! NOT USED -> ./Subtests/ValidationTest.cs !!
 */ 

public class EyeValidationPrtcl : MonoBehaviour
{
    private float FoV;
    private new Camera camera;
    private List<GameObject> targets;
    private int currentTargetIndex = -1;

    public bool DrawGizmos;
    public Material DotMat;
    public TextMeshProUGUI textMesh;

    private void OnEnable()
    {
        camera = ProtocolManager.instance.camera;
        FoV = camera.fieldOfView;
        targets = new List<GameObject>();
        
        /* Generate validation dots to fixate
         * Validation dot matrix in two depths (10 points):
         *  o   o (18° apart)
         *    o
         *  o   o
         */
        List<Vector2> pointsLongLat = new List<Vector2>()
            {
                new Vector2(-9, 9), new Vector2(9, 9), // Top left, right
                new Vector2(0, 0), // Center
                new Vector2(-9, -9), new Vector2(9, -9), // Bottom left, right 
            };

        for (int iDepth = 0; iDepth < 2; iDepth++)
        {
            float dist = iDepth + 1;
            float FoVscale = 2f * Mathf.Tan(Mathf.Deg2Rad * FoV / 2f) * dist * .02f;

            int iPoint = 0;
            foreach (Vector2 point in pointsLongLat)
            {
                float lat = point.x * Mathf.Deg2Rad + Mathf.PI / 2f;
                float lon = point.y * Mathf.Deg2Rad;

                Vector3 unitVec = new Vector3
                {
                    x = Mathf.Sin(lat) * Mathf.Cos(lon),
                    y = Mathf.Sin(lat) * Mathf.Sin(lon),
                    z = Mathf.Cos(lat)
                };
                unitVec *= dist; // 1m away, then 2m away from camera

                Transform target = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                target.name = $"{iPoint++}_{iDepth}";
                target.position = transform.position + transform.rotation *
                    (Quaternion.FromToRotation(Vector3.forward, Vector3.left) * unitVec);
                target.localScale *= FoVscale;
                
                // Shader draws over everything else so the dots don't get hidden by environment
                target.GetComponent<MeshRenderer>().sharedMaterial = DotMat;
                target.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                
                target.SetParent(transform);

                targets.Add(target.gameObject);
            }
        }
    }

    private IEnumerator Start()
    {
        // Debug - calculate angle difference between world point below mouse on viewport and the current target

        for (int iPoint = 0; iPoint < targets.Count; iPoint++)
        {
            HideAllTargets();
            ShowTarget(iPoint);
            
            yield return new WaitForSeconds(4);
        }
        HideAllTargets();
    }

    private void Update()
    {
        // DEBUGGING
        if (currentTargetIndex != -1)
        {
            Vector2 mouseVPPos = Mouse.current.position.ReadValue();
            Vector3 mousePoint = camera.ScreenToWorldPoint(new Vector3(mouseVPPos.x, mouseVPPos.y, 1));
            Vector3 camPos = camera.transform.position;
            Ray mouseRay = new Ray(camPos, mousePoint - camPos);

            float dist = DistanceToTargetCenter(mouseRay, currentTargetIndex);
            textMesh.text = $"{dist * Mathf.Rad2Deg:F2}";
        }
    }

    public void ShowTarget(int iTarget)
    {
        targets[iTarget].SetActive(true);
        currentTargetIndex = iTarget;
    }

    public void HideAllTargets()
    {
        foreach (GameObject target in targets)
        {
            target.SetActive(false);
        }

        currentTargetIndex = -1;
    }

    public float DistanceToTargetCenter(Ray ray, int iTarget)
    {
        Vector3 rayVec = ray.direction; // Ray origin is eye/camera position
        // Target points becomes a vector with the eye/camera as the origin
        Vector3 targetVec = targets[iTarget].transform.position - ray.origin;
        
        rayVec /= rayVec.magnitude;
        targetVec /= targetVec.magnitude;

        // Angle between two unit vectors
        return Mathf.Acos(Vector3.Dot(rayVec, targetVec));
    }

    private void OnDrawGizmos()
    {
        if (!DrawGizmos) return;
        
        FoV = Camera.main.fieldOfView;

        List<Vector2> pointsLongLat = new List<Vector2>()
        {
            new Vector2(-9, 9), new Vector2(9, 9),
            new Vector2(0, 0),
            new Vector2(-9, -9), new Vector2(9, -9),
        };

        for (int iDepth = 0; iDepth < 2; iDepth++)
        {
            float dist = iDepth + 1;
            float FoVscale = 2f * Mathf.Tan(Mathf.Deg2Rad * FoV / 2f) * dist * .02f;
            
            foreach (Vector2 point in pointsLongLat)
            {
                float lat = point.x * Mathf.Deg2Rad + Mathf.PI / 2f;
                float lon = point.y * Mathf.Deg2Rad;

                Vector3 unitVec = new Vector3
                {
                    x = Mathf.Sin(lat) * Mathf.Cos(lon),
                    y = Mathf.Sin(lat) * Mathf.Sin(lon),
                    z = Mathf.Cos(lat)
                };
                unitVec *= dist; // 1m away, then 2m away from camera

                Gizmos.color = new Color(.5f, .75f, iDepth * .5f, .75f);
                Gizmos.DrawSphere(transform.position + transform.rotation * ( Quaternion.FromToRotation(Vector3.forward, Vector3.left) * unitVec), FoVscale);
                
            }
        }
    }
}
