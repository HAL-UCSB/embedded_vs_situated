using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEngine.Rendering;
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
        public List<Material> materials = new List<Material>();
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
                mesh.subMeshCount = 26;

                mesh.SetIndices(m_Face.indices, MeshTopology.Triangles, 0);
                mesh.SetTriangles(MuscleTriangles.FrontalisLeft, 1, false);
                mesh.SetTriangles(MuscleTriangles.CorrugatorSupercilliLeft, 2, false);
                mesh.SetTriangles(MuscleTriangles.ZygoMajorLeft, 3, false);
                mesh.SetTriangles(MuscleTriangles.ZygoMinorLeft, 4, false);
                mesh.SetTriangles(MuscleTriangles.ProcerusLeft, 5, false);
                mesh.SetTriangles(MuscleTriangles.DepressorSuperLeft, 6, false);
                mesh.SetTriangles(MuscleTriangles.NasalisLeft, 7, false);
                mesh.SetTriangles(MuscleTriangles.LevatorLabiiLeft, 8, false);
                mesh.SetTriangles(MuscleTriangles.LevatorLabii2Left, 9, false);
                mesh.SetTriangles(MuscleTriangles.BuccinatorLeft, 10, false);
                mesh.SetTriangles(MuscleTriangles.RisoriusLeft, 11, false);
                mesh.SetTriangles(MuscleTriangles.OrbOrisLeft, 12, false);
                // mesh.SetTriangles(MuscleTriangles.OrbiOculiLeft, 12, false);

                mesh.SetTriangles(MuscleTriangles.FrontalisRight, 13, false);
                mesh.SetTriangles(MuscleTriangles.CorrugatorSupercilliRight, 14, false);
                mesh.SetTriangles(MuscleTriangles.ZygoMajorRight, 15, false);
                mesh.SetTriangles(MuscleTriangles.ZygoMinorRight, 16, false);
                mesh.SetTriangles(MuscleTriangles.ProcerusRight, 17, false);
                mesh.SetTriangles(MuscleTriangles.DepressorSuperRight, 18, false);
                // mesh.SetTriangles(MuscleTriangles.OrbiOculiRight, 12, false);
                mesh.SetTriangles(MuscleTriangles.NasalisRight, 19, false);
                mesh.SetTriangles(MuscleTriangles.LevatorLabiiRight, 20, false);
                mesh.SetTriangles(MuscleTriangles.LevatorLabii2Right, 21, false);
                mesh.SetTriangles(MuscleTriangles.BuccinatorRight, 22, false);
                mesh.SetTriangles(MuscleTriangles.RisoriusRight, 23, false);
                // mesh.SetTriangles(MuscleTriangles.Masseter, 13, false);
                mesh.SetTriangles(MuscleTriangles.OrbOrisRight, 24, false);


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
                    Debug.Log("Setting UVs");
                    using (new ScopedProfiler("SetUVs"))
                        mesh.SetUVs(0, m_Face.uvs);
                }


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
            // m_MeshRenderer.materials = materials.ToArray();
            // m_MeshRenderer.sharedMaterials = materials.ToArray();
            // using (new ScopedProfiler("SetMeshTopology"))
            // {
            //     using (new ScopedProfiler("ClearMesh"))
            //         mesh.Clear();

            //     if (m_Face.vertices.Length > 0 && m_Face.indices.Length > 0)
            //     {
            //         using (new ScopedProfiler("SetVertices"))
            //             mesh.SetVertices(m_Face.vertices);

            //         using (new ScopedProfiler("SetIndices"))
            //             mesh.SetIndices(m_Face.indices, MeshTopology.Triangles, 0, false);

            //         using (new ScopedProfiler("RecalculateBounds"))
            //             mesh.RecalculateBounds();

            //         if (m_Face.normals.Length == m_Face.vertices.Length)
            //         {
            //             using (new ScopedProfiler("SetNormals"))
            //                 mesh.SetNormals(m_Face.normals);
            //         }
            //         else
            //         {
            //             using (new ScopedProfiler("RecalculateNormals"))
            //                 mesh.RecalculateNormals();
            //         }
            //     }

            //     if (m_Face.uvs.Length > 0)
            //     {
            //         using (new ScopedProfiler("SetUVs"))
            //             mesh.SetUVs(0, m_Face.uvs);
            //     }

            //     var meshFilter = GetComponent<MeshFilter>();
            //     if (meshFilter != null)
            //     {
            //         meshFilter.sharedMesh = mesh;
            //     }

            //     var meshCollider = GetComponent<MeshCollider>();
            //     if (meshCollider != null)
            //     {
            //         meshCollider.sharedMesh = mesh;
            //     }

            //     m_TopologyUpdatedThisFrame = true;
            // }
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
            activationMaterials = materials.ToArray();
            muscleMaterials = new Material[26];
            muscleMaterials[0] = activationMaterials[0];

            m_MeshRenderer.materials = materials.ToArray();
            m_MeshRenderer.sharedMaterials = materials.ToArray();
            m_Face = GetComponent<ARFace>();
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

        Material[] activationMaterials;
        Material[] muscleMaterials;
    }
}