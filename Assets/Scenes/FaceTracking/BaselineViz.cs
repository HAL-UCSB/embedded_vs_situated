using UnityEngine.Assertions;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class BaselineViz : MonoBehaviour
    {

        public ExerciseRoutine exerciseRoutine;
        ExerciseType currentExercise;
        ExercisePhase currentExercisePhase;
        LandmarkMovingAverageFilter landmarkMovingAverageFilter;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            landmarkMovingAverageFilter = new LandmarkMovingAverageFilter(5);
        }


        public void ProcessLandmarks(ARTrackablesChangedEventArgs<ARFace> trackablesChangedEventArgs)
        {

            if (currentExercisePhase != ExercisePhase.Exercise)
            {
                Debug.LogFormat($"CEP is {currentExercisePhase}");
                return;
            }
            Debug.Log("Processing Landmarks");
            ARFace arFace = null;
            if (trackablesChangedEventArgs.added.Count > 0)
            {
                Debug.Log("Face added ");
                arFace = trackablesChangedEventArgs.added[0];
            }
            else if (trackablesChangedEventArgs.updated.Count > 0)
            {
                Debug.Log("Face updated ");
                arFace = trackablesChangedEventArgs.updated[0];
            }

            if (arFace != null)
            {
                var baseline = CalibrationLandmarks.baselineLandmarks;
                var current = landmarkMovingAverageFilter.Process(arFace.vertices.ToArray());
                Vector3[] exercise = null;
                var verticesString = "";
                var faceRotation = arFace.pose.rotation;
                var facePosition = arFace.pose.position;
                verticesString += $"{facePosition.x}, {facePosition.y}, {facePosition.z},";
                verticesString += $"{faceRotation.w}, {faceRotation.x}, {faceRotation.y}, {faceRotation.z},";
                foreach (var v in arFace.vertices)
                {
                    verticesString += $"{v.x}, {v.y}, {v.z},";
                }

                LogFile.Log("BaslineV", verticesString);

                if (currentExercise == ExerciseType.kSmile)
                {
                    exercise = CalibrationLandmarks.smileLandmarks;
                }
                else if (currentExercise == ExerciseType.kEyebrowRaise)
                {
                    exercise = CalibrationLandmarks.eyebrowraiseLandmarks;
                }
                else
                {
                    exercise = CalibrationLandmarks.reversefrownLandmarks;
                }

                Assert.IsNotNull(exercise, "Invalid exercise");

                if (baseline == null || baseline.Length == 0)
                {
                    Debug.Log("Baseline is null or empty");
                    return;
                }

                if (current == null || current.Length == 0)
                {
                    Debug.LogFormat("Current is null or empty", tag = "IntARFace");
                    return;
                }


                Debug.Log($"Processing {currentExercise} exercise");
                var muscleLandmarks = MuscleTriangles.exerciseLandmarks[(int)currentExercise];
                var muscleNames = MuscleTriangles.commonMuscleNames[(int)currentExercise];

                float[] distances = new float[muscleLandmarks.Count];

                for (int i = 0; i < muscleLandmarks.Count; i++)
                {
                    var landmarks = muscleLandmarks[i];
                    distances[i] = 0.0f;
                    var baselinePos = Vector3.zero;
                    var currentPos = Vector3.zero;
                    var exercisePos = Vector3.zero;
                    foreach (var landmark in landmarks)
                    {
                        currentPos += current[landmark];
                        baselinePos += baseline[landmark];
                        exercisePos += exercise[landmark];
                    }
                    var currDist = (currentPos - baselinePos).magnitude;
                    var maxDist = (exercisePos - baselinePos).magnitude;
                    distances[i] = currDist / maxDist;
                }

                var actString = $"{currentExercisePhase}, {currentExercise},";
                foreach (var act in distances)
                {
                    actString += $"{act},";
                }
                LogFile.Log("BaselineAct", actString);
            }
        }
        // Update is called once per frame
        void Update()
        {
            currentExercisePhase = exerciseRoutine.currentExercisePhase();
            currentExercise = exerciseRoutine.currentExercise();
            if (currentExercisePhase == ExercisePhase.Break && landmarkMovingAverageFilter.IsInitialized())
            {
                landmarkMovingAverageFilter.Reset();
            }
        }
    }
}