using UnityEngine;

public static class Tools
{
    public static bool InLineOfSightExtention(this Vector3 initPos, Vector3 endPos, LayerMask obstacleMask)
    {
        Vector3 dir = endPos - initPos;

        return !Physics.Raycast(initPos, dir, dir.magnitude, obstacleMask);
    }

    public static bool InLineOfSight(Vector3 initPos, Vector3 endPos, LayerMask obstacleMask)
    {
        Vector3 dir = endPos - initPos;

        return !Physics.Raycast(initPos, dir, dir.magnitude, obstacleMask);
    }


    public static bool FieldOfView(Vector3 InitPos, Vector3 AgentFwd, Vector3 TargetPos, float ViewRadius, float ViewAngle, LayerMask mask)
    {
        Vector3 dir = TargetPos - InitPos;
        
        if(dir.sqrMagnitude > ViewRadius * ViewRadius) return false;

        if(Vector3.Angle(AgentFwd, TargetPos - InitPos) > ViewAngle /2 ) return false;

        if(!InitPos.InLineOfSightExtention(TargetPos - InitPos, mask)) return false;

        return true;
    }
}
