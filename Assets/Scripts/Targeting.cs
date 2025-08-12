using UnityEngine;
public static class Targeting
{
    public static IDamageable FindClosestTarget(Vector3 position, float radius, Collider[] colliderArray, Alignment thisAlignment)
    {
        Physics.OverlapSphereNonAlloc(position, radius, colliderArray);

        float closestDistSqr = float.MaxValue;
        IDamageable closestTarget = null;
        for (int i = 0; i < colliderArray.Length; i++)
        {
            var sensedObj = colliderArray[i];
            var damageable = sensedObj.GetComponent<IDamageable>();

            if (damageable == null || damageable.Alignment == thisAlignment)
                continue;

            var dist = sensedObj.ClosestPoint(position) - position;
            if (dist.sqrMagnitude < closestDistSqr)
            {
                closestDistSqr = dist.sqrMagnitude;
                closestTarget = damageable;
            }
        }

        return closestTarget;
    }
}