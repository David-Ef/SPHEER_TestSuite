using UnityEngine;

public static class Gaze2TargetChckr
{
    public static Camera cam;
    public static Transform CamTrans;

    public static float maxAngDist2Target; // In rad 
    
    // Angle (relative to the camera) between a 2D-point in viewport-space and a point in 3D world-space 
    public static float AngularDistanceVP2W(Vector2 VPPos, Vector3 worldPos)
    {
        Vector3 point = cam.ScreenToWorldPoint(new Vector3(VPPos.x, VPPos.y, 1));
        Vector3 camPos = CamTrans.position;
        Ray ray = new Ray(camPos, point - camPos);
        
        Vector3 rayVec = ray.direction;
        // Target points becomes a vector with the eye/camera as the origin
        Vector3 targetVec = worldPos - ray.origin;
        
        rayVec /= rayVec.magnitude;
        targetVec /= targetVec.magnitude;

        // Angle between two unit vectors
        return Mathf.Acos(Vector3.Dot(rayVec, targetVec));
    }
    
    // Angle (relative to the camera) between 2 points in 3D world-space
    public static float AngularDistanceW2W(Vector3 worldPos1, Vector3 worldPos2)
    {
        // Turn them into dir. vectors with the cam pos as origin
        Vector3 camPos = CamTrans.position;
        worldPos1 -= camPos;
        worldPos2 -= camPos;

        // Normalise
        worldPos1 /= worldPos1.magnitude;
        worldPos2 /= worldPos2.magnitude;

        // Angle between two unit vectors
        float dist = Mathf.Acos(Vector3.Dot(worldPos1, worldPos2));

        return dist;
    }

    public static bool IsGaze2TargetDistanceWithinLimit(Ray gazeRay, Vector3 targetPos)
    {
        return AngularDistanceW2W(gazeRay.GetPoint(1f), targetPos) <= maxAngDist2Target;
    }
}
