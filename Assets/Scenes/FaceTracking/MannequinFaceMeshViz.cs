using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.Receiver.Primitives;

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
    public sealed class MannequinFaceMeshViz : MonoBehaviour
    {
        /// <summary>
        /// Get the <c>Mesh</c> that this visualizer creates and manages.
        /// </summary>
        public Mesh mesh { get; private set; }
        public List<Material> activationMaterials = new List<Material>();
        public GameObject exerciseRoutinePrefab;
        public Material baseMaterial;

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

        int[] ComputeMuscleActivation(Vector3[] faceLandmarks, Quaternion faceRotation)
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
                Debug.Log($"CD : {currDist} MD: {maxDist} A: {activation}");
                if (activation < 0.4)
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

            var actString = "";
            foreach (var act in activations)
            {
                actString += $"{act},";
            }
            LogFile.Log("MFAct", actString);
            return activations;
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

        void SetMeshTopology()
        {
            if (mesh == null)
            {
                return;
            }
            // using the vertices from m_Face; set topology of different regions then set separate materials
            mesh.Clear();

            var verticesString = "";
            var faceRotation = m_Face.pose.rotation;
            var facePosition = m_Face.pose.position;
            verticesString += $"{facePosition.x}, {facePosition.y}, {facePosition.z},";
            verticesString += $"{faceRotation.w}, {faceRotation.x}, {faceRotation.y}, {faceRotation.z},";
            foreach (var v in m_Face.vertices)
            {
                verticesString += $"{v.x}, {v.y}, {v.z},";
            }

            LogFile.Log("MFV", verticesString);

            if (m_Face.vertices.Length > 0 && m_Face.indices.Length > 0)
            {
                mesh.SetVertices(m_Face.vertices);


                // mesh.SetIndices(m_Face.indices, MeshTopology.Triangles, 0);
                // Material[] materials = new Material[mesh.subMeshCount];
                // materials[0] = baseMaterial;

                // Debug.Log($"CEP : {exercisePhase}; Ex : {exerciseType}");
                // if (exercisePhase == ExercisePhase.Exercise)
                // {
                //     var muscles = MuscleTriangles.exerciseMusclesArray[(int)exerciseType];
                //     mesh.subMeshCount = muscles.Count + 1;
                //     for (int i = 0; i < muscles.Count; i++)
                //     {
                //         mesh.SetTriangles(muscles[i], i + 1);
                //     }

                //     var activationIdx = ComputeMuscleActivation(m_Face.vertices.ToArray());
                //     // set materials based on activation levels
                //     for (int i = 1; i < mesh.subMeshCount; i++)
                //     {
                //         materials[i] = activationMaterials[activationIdx[i - 1]];
                //     }
                // }

                mesh.SetIndices(m_Face.indices, MeshTopology.Triangles, 0);

                Debug.Log($"Face rotation {m_Face.pose.rotation}");
                var muscles = MuscleTriangles.exerciseMusclesArray[(int)exerciseType];
                mesh.subMeshCount = muscles.Count + 1;
                Material[] materials = new Material[mesh.subMeshCount];
                materials[0] = baseMaterial;

                for (int i = 0; i < muscles.Count; i++)
                {
                    mesh.SetTriangles(muscles[i], i + 1);
                }

                var activationIdx = ComputeMuscleActivation(m_Face.vertices.ToArray(), m_Face.pose.rotation);
                // set materials based on activation levels
                for (int i = 0; i < activationIdx.Length; i++)
                {
                    if (exercisePhase == ExercisePhase.Exercise)
                    {
                        materials[i + 1] = activationMaterials[activationIdx[i]];
                    }
                    else
                    {
                        materials[i + 1] = baseMaterial;
                    }
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

        void Awake()
        {
            mesh = new Mesh();
            m_MeshRenderer = GetComponent<MeshRenderer>();
            m_Face = GetComponent<ARFace>();

            landmarkMovingAverageFilter = new LandmarkMovingAverageFilter(3);
            // gameObject.transform.position += new Vector3(0.05f, 0.15f, 0.6f);
        }

        void OnEnable()
        {
            Debug.Log("In on Enable");
            m_Face.updated += OnUpdated;
            ARSession.stateChanged += OnSessionStateChanged;
            UpdateVisibility();
        }

        void OnDisable()
        {
            m_Face.updated -= OnUpdated;
            ARSession.stateChanged -= OnSessionStateChanged;
        }

        void OnDestroy()
        {
            Destroy(exerciseRoutineGameObj);
        }

        ARFace m_Face;
        MeshRenderer m_MeshRenderer;
        ExerciseRoutine exerciseRoutine;
        GameObject exerciseRoutineGameObj;
        bool m_TopologyUpdatedThisFrame;
        ExerciseType exerciseType;
        ExercisePhase exercisePhase;
        LandmarkMovingAverageFilter landmarkMovingAverageFilter;
    }
}
