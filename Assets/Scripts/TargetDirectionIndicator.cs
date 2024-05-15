using UnityEngine;

public class TargetDirectionIndicator : MonoBehaviour
{
    [SerializeField]
    private Transform _currentTarget;
    public Transform DirectionArrow;

    private Camera _camera;
    private Transform _cameraTrans;
    private MeshRenderer _meshRenderer;

    private void Start()
    {
        _camera = ProtocolManager.instance.camera;
        _cameraTrans = _camera.transform;
        _meshRenderer = DirectionArrow.GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        if (_currentTarget != null)
        {
            Vector3 VPtargetProject = _camera.WorldToViewportPoint(_currentTarget.position);
            Vector2 VPtarget = new Vector2(VPtargetProject.x, VPtargetProject.y);
            Vector2 VPcenter = new Vector2(.5f, .5f);

            // Distance in viewport space
            float dist = (VPtarget - VPcenter).magnitude;
            // Do not show the direction arrow if the target is within 10% of the center of the viewport
            if (VPtargetProject.z > 0 && dist < .10f)
            {
                ToggleArrow(false);
                return;
            }

            // Angle the direction arrow towards the target

            // METHOD 1 -- Just angle arrow in the direction of the target
            //DirectionArrow.LookAt(_currentTarget.position);

            // METHOD 2 -- Project target position onto camera plane
            Vector3 camPos = _cameraTrans.position;
            Vector3 camForw = _cameraTrans.forward;
            VPtargetProject = Vector3.ProjectOnPlane(_currentTarget.position - camPos, camForw);
            DirectionArrow.LookAt(camPos + VPtargetProject + camForw * 5f);

            // Correct the direction arrow quad's normal
            DirectionArrow.Rotate(Vector3.right, 90);
            DirectionArrow.Rotate(Vector3.up, 90);
            ToggleArrow(true);
        }
    }
    
    public void SetCurrentTarget(Transform target)
    {
        enabled = target != null;

        _currentTarget = target;

        // Reset
        DirectionArrow.localRotation = Quaternion.identity;
        DirectionArrow.localPosition = new Vector3(0, 0, 5);
    }

    private void ToggleArrow(bool state)
    {
        _meshRenderer.enabled = state;
    }

    public void Unset()
    {
        SetCurrentTarget(null);
        ToggleArrow(false);
    }
}
