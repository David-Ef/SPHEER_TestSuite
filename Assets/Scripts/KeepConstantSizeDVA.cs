using UnityEngine;

public class KeepConstantSizeDVA : MonoBehaviour
{
    private Transform _relative;
    public float constantDVA = 2f;
    private static float _FoV = -1;

    private void OnEnable()
    {
        if (_FoV < 0)
        {
            _FoV = ProtocolManager.instance.camera.fieldOfView;
            _FoV *= Mathf.Deg2Rad;
        }

        _relative = ProtocolManager.userHead;

        constantDVA *= Mathf.Deg2Rad;
        
        float dist = (transform.position - _relative.position).magnitude;
        float scale = 2f * Mathf.Tan(_FoV / 2f) * dist * constantDVA;
        transform.localScale = new Vector3(scale, scale, scale);
    }

    private void Update()
    {
        setScale();
    }

    public void setScale()
    {
        float dist = (transform.position - _relative.position).magnitude;
        float scale = 2f * Mathf.Tan(_FoV / 2f) * dist * constantDVA;
        transform.localScale = new Vector3(scale, scale, scale);
    }
}
