using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARFoundation.Samples
{
    /// <summary>
    /// Generates a mesh for an <see cref="ARFace"/>.
    /// </summary>
    /// <remarks>
    /// If this <c>GameObject</c> has a <c>MeshFilter</c> and/or <c>MeshCollider</c>,
    /// this component will generate a mesh from the underlying <c>XRFace</c>.
    /// </remarks>
    [RequireComponent(typeof(ARFace))]
    public sealed class FaceMeshViz : MonoBehaviour
    {
        /// <summary>
        /// Get the <c>Mesh</c> that this visualizer creates and manages.
        /// </summary>
        public Mesh mesh { get; private set; }
        public List<Material> activationMaterials = new List<Material>();
        public GameObject exerciseRoutinePrefab;
        // Material[] activationMaterials;
        void SetVisible(bool visible)
        {
            m_MeshRenderer = GetComponent<MeshRenderer>();
            if (m_MeshRenderer == null)
            {
                return;
            }

            //if it is getting visible after being invisible for a while, set its topology
            if (visible && !m_MeshRenderer.enabled)
            {
                SetMeshTopology();
            }

            m_MeshRenderer.enabled = visible;
        }
        float[] ComputeMuscleActivation(Vector3[] faceLandmarks, Quaternion faceRotation)
        {
            var baseline = CalibrationLandmarks.baselineLandmarks;
            // var faceInverseRotation = Quaternion.Inverse(faceRotation);
            // for (int i = 0; i < faceLandmarks.Length; i++)
            // {
            //     faceLandmarks[i] = CalibrationLandmarks.RotateAroundPivot(faceLandmarks[i],
            //                                                               faceLandmarks[4],
            //                                                           faceInverseRotation);
            // }

            var current = landmarkMovingAverageFilter.Process(faceLandmarks);
            Vector3[] exercise = null;

            if (exerciseType == ExerciseType.kSmile)
            {
                exercise = CalibrationLandmarks.smileLandmarks;
            }
            else if (exerciseType == ExerciseType.kEyebrowRaise)
            {
                exercise = CalibrationLandmarks.eyebrowraiseLandmarks;
            }
            else
            {
                exercise = CalibrationLandmarks.reversefrownLandmarks;
            }

            Assert.IsNotNull(exercise, "Invalid exercise");
            var muscleLandmarks = MuscleTriangles.exerciseLandmarks[(int)exerciseType];
            float[] activations = new float[muscleLandmarks.Count];

            for (int i = 0; i < muscleLandmarks.Count; i++)
            {
                var landmarks = muscleLandmarks[i];
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
                activations[i] = Mathf.Min(currDist / maxDist, 1.0f);
            }
            var actString = "";
            foreach (var act in activations)
            {
                actString += $"{act},";
            }
            LogFile.Log("FAct", actString);
            Debug.Log($"{exerciseType} : {actString}");
            return activations;
        }


        void SetMeshTopology()
        {
            if (mesh == null)
            {
                return;
            }
            var verticesString = "";
            var faceRotation = m_Face.pose.rotation;
            var facePosition = m_Face.pose.position;
            verticesString += $"{facePosition.x}, {facePosition.y}, {facePosition.z},";
            verticesString += $"{faceRotation.w}, {faceRotation.x}, {faceRotation.y}, {faceRotation.z},";
            foreach (var v in m_Face.vertices)
            {
                verticesString += $"{v.x}, {v.y}, {v.z},";
            }

            LogFile.Log("FV", verticesString);
            // using the vertices from m_Face; set topology of different regions then set separate materials
            mesh.Clear();
            if (m_Face.vertices.Length > 0 && m_Face.indices.Length > 0)
            {

                mesh.SetVertices(m_Face.vertices);

                var muscles = MuscleTriangles.exerciseMusclesArray[(int)exerciseType];
                mesh.subMeshCount = muscles.Count;

                Debug.Log($"{muscles.Count} muscles for {exerciseType}");
                for (int i = 0; i < muscles.Count; i++)
                {
                    mesh.SetTriangles(muscles[i], i);
                }

                mesh.RecalculateBounds();
                if (m_Face.normals.Length == m_Face.vertices.Length)
                {
                    mesh.SetNormals(m_Face.normals);
                }
                else
                {
                    mesh.RecalculateNormals();
                }

                if (m_Face.uvs.Length > 0)
                {
                    mesh.SetUVs(0, m_Face.uvs);
                }

                // set materials based on activation levels
                var activations = ComputeMuscleActivation(m_Face.vertices.ToArray(),
                                                            m_Face.pose.rotation);
                Material[] materials = new Material[mesh.subMeshCount];
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    var activationIdx = 1;
                    var activationAlpha = 1.0f;
                    if (activations[i] < 0.4)
                    {
                        activationIdx = 0;
                        activationAlpha = 1.0f - (2 * activations[i]);
                    }
                    else if (activations[i] > 0.7)
                    {
                        activationIdx = 2;
                        activationAlpha = -1.66f + (activations[i] * 2.66f);
                    }
                    else
                    {
                        activationAlpha = activations[i] > 0.55f ? 3.931f - 5.33f * activations[i] : -1.933f + 5.33f * activations[i];
                    }
                    materials[i] = activationMaterials[activationIdx];
                    var matColor = materials[i].color;
                    matColor.a = activationAlpha;
                    materials[i].color = matColor;


                    // Debug.Log($"{muscleName} : MinX {materials[i].GetFloat("_SubMeshMinX")}, MaxX {materials[i].GetFloat("_SubMeshMaxX")}," +
                    //            $"MinY {materials[i].GetFloat("_SubMeshMinY")}, MaxY {materials[i].GetFloat("_SubMeshMaxY")} ; FA {materials[i].GetFloat("_FillAmount")}");
                    // Debug.Log($"{exerciseType} ; {muscleName} ; MinX {minmaxXs[i][0]}; MaxX {minmaxXs[i][1]}; MinY {minmaxYs[i][0]}; MaxY {minmaxYs[i][1]} {activations[i]}");
                }
                m_MeshRenderer.materials = materials;
            }

            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = mesh;
            }

            var meshCollider = GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = mesh;
            }

            m_TopologyUpdatedThisFrame = true;
        }

        void UpdateVisibility()
        {
            var visible = enabled &&
                (m_Face.trackingState != TrackingState.None) &&
                (ARSession.state > ARSessionState.Ready);

            SetVisible(visible);
        }

        void OnUpdated(ARFaceUpdatedEventArgs eventArgs)
        {
            if (exercisePhase != ExercisePhase.Exercise)
            {
                return;
            }
            UpdateVisibility();
            if (!m_TopologyUpdatedThisFrame)
            {
                SetMeshTopology();
            }
            m_TopologyUpdatedThisFrame = false;
        }

        void OnSessionStateChanged(ARSessionStateChangedEventArgs eventArgs)
        {
            UpdateVisibility();
        }

        void Start()
        {
            if (exerciseRoutinePrefab != null)
            {
                exerciseRoutineGameObj = Instantiate(exerciseRoutinePrefab);
            }
            Assert.IsNotNull(exerciseRoutineGameObj);
            exerciseRoutine = exerciseRoutineGameObj.GetComponent<ExerciseRoutine>();
            Assert.IsNotNull(exerciseRoutine, "Routine is null");
            exercisePhase = exerciseRoutine.currentExercisePhase();
            exerciseType = exerciseRoutine.currentExercise();
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

        }
        void Update()
        {
            exercisePhase = exerciseRoutine.currentExercisePhase();
            exerciseType = exerciseRoutine.currentExercise();
            if (exercisePhase != ExercisePhase.Exercise)
            {
                SetVisible(false);
            }

            if (exercisePhase == ExercisePhase.Break && landmarkMovingAverageFilter.IsInitialized())
            {
                landmarkMovingAverageFilter.Reset();
            }
        }
        void Awake()
        {
            mesh = new Mesh();
            m_MeshRenderer = GetComponent<MeshRenderer>();
            m_Face = GetComponent<ARFace>();
            landmarkMovingAverageFilter = new LandmarkMovingAverageFilter(3);
        }

        void OnEnable()
        {
            m_Face.updated += OnUpdated;
            ARSession.stateChanged += OnSessionStateChanged;
            UpdateVisibility();
        }

        void OnDisable()
        {
            m_Face.updated -= OnUpdated;
            ARSession.stateChanged -= OnSessionStateChanged;
        }

        ARFace m_Face;
        MeshRenderer m_MeshRenderer;
        bool m_TopologyUpdatedThisFrame;
        LandmarkMovingAverageFilter landmarkMovingAverageFilter;
        GameObject exerciseRoutineGameObj;
        ExerciseRoutine exerciseRoutine;
        ExercisePhase exercisePhase;
        ExerciseType exerciseType;
    }
}