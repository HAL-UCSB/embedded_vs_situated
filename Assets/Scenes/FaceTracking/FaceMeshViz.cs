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
        // public ExerciseRoutine exerciseRoutine;
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
        int[] ComputeMuscleActivation(Vector3[] faceLandmarks)
        {
            var baseline = CalibrationLandmarks.baselineLandmarks;
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
            int[] activations = new int[muscleLandmarks.Count];

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
                var activation = currDist / maxDist;
                if (activation < 0.2)
                {
                    activations[i] = 0;
                }
                else if (activation > 0.7)
                {
                    activations[i] = 2;
                }
                else
                {
                    activations[i] = 1;
                }
            }
            return activations;
        }


        void SetMeshTopology()
        {
            if (mesh == null)
            {
                return;
            }
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
                var activationIdx = ComputeMuscleActivation(m_Face.vertices.ToArray());
                Material[] materials = new Material[mesh.subMeshCount];
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    materials[i] = activationMaterials[activationIdx[i]];
                }
                m_MeshRenderer.materials = materials;
                // StringBuilder sb = new StringBuilder();
                // var sharedMaterials = m_MeshRenderer.sharedMaterials;
                // // Write each vertex with its index and position
                // for (int i = 0; i < mesh.subMeshCount; i++)
                // {
                //     try
                //     {
                //         var subMesh = mesh.GetSubMesh(i);
                //         sb.AppendLine($"sub-mesh {i} : {sharedMaterials[i]}\n");
                //     }
                //     catch (System.Exception e)
                //     {
                //         Debug.LogError($"Sub mesh {i} not found; {e}");
                //     }
                // }

                // StringBuilder sb = new StringBuilder();

                // // Write each vertex with its index and position
                // for (int i = 0; i < m_Face.vertices.Length; i++)
                // {
                //     Vector3 worldPos = m_Face.vertices[i];
                //     sb.AppendLine($"{i} {worldPos.x:F4} {worldPos.y:F4} {worldPos.z:F4}");
                // }

                // Get the path for Android
                // string path = Path.Combine(Application.persistentDataPath, "vertices.txt");

                // try
                // {
                //     File.WriteAllText(path, sb.ToString());
                //     Debug.Log($"Vertex positions written to {path}");
                // }
                // catch (System.Exception e)
                // {
                //     Debug.LogError($"Error writing vertex file: {e.Message}");
                // }
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
            GameObject gameObj = null;
            if (exerciseRoutinePrefab != null)
            {
                gameObj = Instantiate(exerciseRoutinePrefab);
                Debug.Log($"ER created with {exerciseRoutine}");
            }
            if (gameObj != null)
            {
                exerciseRoutine = gameObj.GetComponent<ExerciseRoutine>();
            }
            Assert.IsNotNull(exerciseRoutine, "Routine is null");
            exercisePhase = exerciseRoutine.currentExercisePhase();
            exerciseType = exerciseRoutine.currentExercise();
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
        void Update()
        {
            exercisePhase = exerciseRoutine.currentExercisePhase();
            exerciseType = exerciseRoutine.currentExercise();
        }
        void Awake()
        {
            mesh = new Mesh();
            m_MeshRenderer = GetComponent<MeshRenderer>();
            m_Face = GetComponent<ARFace>();
            // set this through a drop down
            // exerciseType = ExerciseType.kSmile;
            landmarkMovingAverageFilter = new LandmarkMovingAverageFilter(5);
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

        ExerciseRoutine exerciseRoutine;

        ExercisePhase exercisePhase;
        ExerciseType exerciseType;
    }
}