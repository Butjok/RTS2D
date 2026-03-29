using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

public static class MathUtility {

    public static Vector2 FindClosestPointOnPolyline(Vector2 point, IReadOnlyList<Vector2> polyline, out float distance, out int closestSegmentIndex, out bool isEndOfPolyline) {
        var closestPoint = Vector2.zero;
        var closestDistanceSquared = float.MaxValue;
        distance = 0;
        closestSegmentIndex = -1;

        isEndOfPolyline = false;
        var distanceWalked = 0f;
        for (var segmentIndex = 0; segmentIndex < polyline.Count - 1; segmentIndex++) {
            var segmentStart = polyline[segmentIndex];
            var segmentEnd = polyline[segmentIndex + 1];

            var segmentVector = segmentEnd - segmentStart;
            var segmentLengthSquared = Vector2.Dot(segmentVector, segmentVector);

            var t = Vector2.Dot(point - segmentStart, segmentVector) / segmentLengthSquared;
            t = System.Math.Clamp(t, 0, 1);

            var projection = segmentStart + t * segmentVector;
            var distanceSquared = Vector2.SqrMagnitude(point - projection);

            if (distanceSquared < closestDistanceSquared) {
                closestDistanceSquared = distanceSquared;
                closestPoint = projection;
                distance = distanceWalked + Sqrt(segmentLengthSquared) * t;
                closestSegmentIndex = segmentIndex;
                if (segmentIndex == polyline.Count - 2 && Approximately(t,1)) 
                    isEndOfPolyline = true;
            }
            distanceWalked += Sqrt(segmentLengthSquared);
        }

        return closestPoint;
    }

    public static Vector2 GetPointOnPolylineByDistance(IReadOnlyList<Vector2> polyline, float distance, out bool isEndOfPolyline) {
        var distanceWalked = 0f;
        isEndOfPolyline = false;
        for (var i = 0; i < polyline.Count - 1; i++) {
            var segmentStart = polyline[i];
            var segmentEnd = polyline[i + 1];

            var segmentVector = segmentEnd - segmentStart;
            var segmentLength = segmentVector.magnitude;

            if (distanceWalked + segmentLength >= distance) {
                var t = (distance - distanceWalked) / segmentLength;
                return segmentStart + t * segmentVector;
            }
            distanceWalked += segmentLength;
        }
        isEndOfPolyline = true;
        return polyline[polyline.Count - 1];
    }

    public static Vector2 ToVector2(this Vector3 vector) {
        return new Vector2(vector.x, vector.z);
    }

    public static Vector3 ToVector3(this Vector2 vector) {
        return new Vector3(vector.x, 0, vector.y);
    }
    public static Vector3 ToVector3(this Vector2Int vector) {
        return new Vector3(vector.x, 0, vector.y);
    }

    private static readonly float[] t = new float[8];

    public struct RayCollision {
        public bool hit;
        public float distance;
        public Vector2 point;
    }
    
    // adopted from raylib:
    // https://github.com/raysan5/raylib/blob/0f0983c06565b3a7ec7c2248a8361037cf3e4d24/src/rmodels.c#L4195
    
    public static RayCollision GetRayCollisionBox(Ray2D ray, Vector2 boxMin, Vector2 boxMax) {
        RayCollision collision = new RayCollision();

        // NOTE: If ray.origin is inside the box, the distance is negative (as if the ray was reversed)
        // Reversing ray.direction will give use the correct result
        bool insideBox = (ray.origin.x > boxMin.x) && (ray.origin.x < boxMax.x) &&
                         (ray.origin.y > boxMin.y) && (ray.origin.y < boxMax.y);

        if (insideBox) ray.direction = -(ray.direction);

        t[6] = 1.0f / ray.direction.x;
        t[7] = 1.0f / ray.direction.y;

        t[0] = (boxMin.x - ray.origin.x) * t[6];
        t[1] = (boxMax.x - ray.origin.x) * t[6];
        t[2] = (boxMin.y - ray.origin.y) * t[7];
        t[3] = (boxMax.y - ray.origin.y) * t[7];
        t[4] = Max(Max(Min(t[0], t[1]), Min(t[2], t[3])));
        t[5] = Min(Min(Max(t[0], t[1]), Max(t[2], t[3])));

        collision.hit = !(t[5] < 0 || t[4] > t[5]);
        collision.distance = t[4];
        collision.point = ray.origin + ray.direction * collision.distance;

        if (insideBox) {
            // Reset ray.direction
            ray.direction = -(ray.direction);

            // Fix result
            collision.distance *= -1.0f;
        }

        return collision;
    }
}