
using System.Collections.Generic;
using System.Numerics;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public static class CalibrationLandmarks
    {
        public static Vector3[] baselineLandmarks { set; get; }
        public static Vector3[] smileLandmarks { set; get; }
        public static Vector3[] eyebrowraiseLandmarks { set; get; }
        public static Vector3[] reversefrownLandmarks { set; get; }
        public static Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            var translatedPoint = point - pivot;
            var rotatedPoint = rotation * translatedPoint;
            return rotatedPoint + pivot;
        }

    }
}