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
        // int[] ComputeMuscleActivation(Vector3[] faceLandmarks, Quaternion faceRotation)
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
            // int[] activations = new int[muscleLandmarks.Count];
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
                activations[i] = currDist / maxDist;
                // var activation = currDist / maxDist;
                // if (activation < 0.4)
                // {
                //     activations[i] = 0;
                // }
                // else if (activation > 0.7)
                // {
                //     activations[i] = 2;
                // }
                // else
                // {
                //     activations[i] = 1;
                // }
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

                if (minmaxYs.Count == 0)
                {
                    var uvs = m_Face.uvs;
                    for (var i = 0; i < muscles.Count; i++)
                    {
                        float[] minmaxX = new float[2];
                        float[] minmaxY = new float[2];
                        minmaxX[0] = float.MaxValue;
                        minmaxX[1] = float.MinValue;
                        minmaxY[0] = float.MaxValue;
                        minmaxY[1] = float.MinValue;
                        var muscle = muscles[i];

                        foreach (var vertexId in muscle)
                        {
                            if (vertexId < uvs.Length)
                            {
                                var uv = uvs[vertexId];
                                minmaxY[0] = Mathf.Min(minmaxY[0], uv.y);
                                minmaxY[1] = Mathf.Max(minmaxY[1], uv.y);
                                minmaxX[0] = Mathf.Min(minmaxX[0], uv.x);
                                minmaxX[1] = Mathf.Max(minmaxX[1], uv.x);
                            }
                        }
                        minmaxYs.Add(minmaxY);
                        minmaxXs.Add(minmaxX);
                    }
                }

                // set materials based on activation levels
                var activations = ComputeMuscleActivation(m_Face.vertices.ToArray(),
                                                            m_Face.pose.rotation);
                Material[] materials = new Material[mesh.subMeshCount];
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    var activationIdx = 1;
                    if (activations[i] < 0.4)
                    {
                        activationIdx = 0;
                    }
                    else if (activations[i] > 0.7)
                    {
                        activationIdx = 2;
                    }
                    // else
                    // {
                    //     activations[i] = 1;
                    // }
                    // materials[i] = activationMaterials[activationIdx[i]];
                    materials[i] = activationMaterials[activationIdx];
                    var muscleName = MuscleTriangles.commonMuscleNames[(int)exerciseType][i];

                    materials[i].shader = MuscleTriangles.horMuscles.Contains(muscleName) ? horShader : verShader;
                    materials[i].SetFloat(subMeshMinYId, minmaxYs[i][0]);
                    materials[i].SetFloat(subMeshMaxYId, minmaxYs[i][1]);
                    materials[i].SetFloat(subMeshMinXId, minmaxXs[i][0]);
                    materials[i].SetFloat(subMeshMaxXId, minmaxXs[i][1]);
                    materials[i].SetFloat(fillAmountId, activations[i]);

                    Debug.Log($"{muscleName} : {materials[i].shader}, MinX {materials[i].GetFloat(subMeshMinXId)}, MaxX {materials[i].GetFloat(subMeshMaxXId)}," +
                               $"MinY {materials[i].GetFloat(subMeshMinYId)}, MaxY {materials[i].GetFloat(subMeshMaxYId)}");
                    Debug.Log($"{exerciseType} ; {muscleName} ; MinX {minmaxXs[i][0]}; MaxX {minmaxXs[i][1]}; MinY {minmaxYs[i][0]}; MaxY {minmaxYs[i][1]} {activations[i]}");
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

            verShader = Shader.Find("Custom/SegmentFaceMeshVer");
            horShader = Shader.Find("Custom/SegmentFaceMeshHor");
            Assert.IsNotNull(verShader, "Could not find shader");
            Assert.IsNotNull(horShader, " Could not find hor shader");
            fillAmountId = Shader.PropertyToID("_FillAmount");
            subMeshMinYId = Shader.PropertyToID("_SubmeshUVMinY");
            subMeshMaxYId = Shader.PropertyToID("_SubmeshUVMaxY");
            subMeshMinXId = Shader.PropertyToID("_SubmeshUVMinX");
            subMeshMaxXId = Shader.PropertyToID("_SubmeshUVMaxX");
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
                minmaxXs.Clear();
                minmaxYs.Clear();
            }
        }
        void Awake()
        {
            mesh = new Mesh();
            m_MeshRenderer = GetComponent<MeshRenderer>();
            m_Face = GetComponent<ARFace>();
            landmarkMovingAverageFilter = new LandmarkMovingAverageFilter(3);
            minmaxXs = new List<float[]>();
            minmaxYs = new List<float[]>();
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
        List<float[]> minmaxXs;
        List<float[]> minmaxYs;
        Shader verShader;
        Shader horShader;

        int fillAmountId;
        int subMeshMaxYId;
        int subMeshMinYId;
        int subMeshMaxXId;
        int subMeshMinXId;
    }
}