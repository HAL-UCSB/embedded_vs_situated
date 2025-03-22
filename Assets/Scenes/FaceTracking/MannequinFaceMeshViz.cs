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
    public class MannequinFaceMeshViz : MonoBehaviour
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
                Debug.LogError("Null mesh renderer");
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
                Debug.LogError("Null mesh");
                return;
            }
            if (m_Face == null)
            {
                Debug.LogError("Null face");
                return;
            }
            // using the vertices from m_Face; set topology of different regions then set separate materials

            Debug.Log($"Original mesh : {mesh.vertices.Length}; ARFace : {m_Face.vertices.Length}");
            mesh.Clear();
            if (m_Face.vertices.Length > 0 && m_Face.indices.Length > 0)
            {
                // retain the mannequin mesh vertices for now
                // var faceVertices = m_Face.vertices;
                // for (int i = 0; i < faceVertices.Length; i++)
                // {
                //     faceVertices[i] = m_Face.transform.TransformDirection(faceVertices[i]);
                // }
                // mesh.SetVertices(faceVertices);
                // mesh.SetIndices(m_Face.indices, MeshTopology.Triangles, 0);

                if (arCoreRenderClone == null)
                {
                    var arCoreRenderer = m_Face.GetComponent<MeshRenderer>();
                    arCoreRenderClone = Instantiate(arCoreRenderer);
                    arCoreRenderClone.transform.position += new Vector3(0.05f, 0.15f, 0.6f);
                }

                mesh.SetVertices(m_Face.vertices);


                // mesh.subMeshCount = 1;
                // mesh.SetTriangles(OrbOris, 0, false);
                // mesh.SetTriangles(FrontalisTriangles, 0, false);
                // mesh.SetTriangles(CorrugatorSupercilliTriangles, 2, false);
                // mesh.SetTriangles(ZygoMajor, 3, false);
                // mesh.SetTriangles(ZygoMinor, 4, false);
                // mesh.SetTriangles(ProcerusTriangles, 5, false);
                // mesh.SetTriangles(DepressorSuper, 6, false);
                // mesh.SetTriangles(OrbiOculi, 7, false);
                // mesh.SetTriangles(Nasalis, 8, false);
                // mesh.SetTriangles(LevatorLabii, 9, false);
                // mesh.SetTriangles(LevatorLabii2, 10, false);
                // mesh.SetTriangles(Buccinator, 11, false);
                // mesh.SetTriangles(Risorius, 12, false);
                // mesh.SetTriangles(Masseter, 13, false);

                mesh.RecalculateBounds();
                // mesh.RecalculateNormals();
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


                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"{gameObject.transform.position}");
                // Write each vertex with its index and position
                for (int i = 0; i < mesh.vertices.Length; i++)
                {
                    Vector3 worldPos = gameObject.transform.TransformPoint(mesh.vertices[i]);
                    sb.AppendLine($"{i} {worldPos.x:F4} {worldPos.y:F4} {worldPos.z:F4}");
                }

                // Get the path for Android

                string path = Path.Combine(Application.persistentDataPath, "vertices.txt");

                try
                {
                    File.WriteAllText(path, sb.ToString());
                    Debug.Log($"Vertex positions written to {path}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error writing vertex file: {e.Message}");
                }
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
            if (m_Face == null)
            {
                Debug.Log("Null face while updating viz");
                return;
            }
            var visible = enabled &&
                (m_Face.trackingState != TrackingState.None) &&
                (ARSession.state > ARSessionState.Ready);

            SetVisible(visible);
        }

        // public void OnUpdated(ARFaceUpdatedEventArgs eventArgs)
        public void UpdateFace(ARTrackablesChangedEventArgs<ARFace> eventArgs)
        {
            if (eventArgs.removed.Count > 0)
            {
                Debug.Log("Trackable removed");
                m_TrackingAFace = false;
                m_Face = null;
                return;
            }
            if (m_TrackingAFace)
            {
                m_Face = eventArgs.updated.First<ARFace>();
            }
            else
            {
                m_Face = eventArgs.added.First<ARFace>();
                m_TrackingAFace = true;
            }
            if (m_Face != null)
            {
                Debug.Log("Got update face");
                UpdateVisibility();
                if (!m_TopologyUpdatedThisFrame)
                {
                    SetMeshTopology();
                }
                m_TopologyUpdatedThisFrame = false;
            }
            else
            {
                Debug.Log("Still null face :(");
            }
        }

        void OnSessionStateChanged(ARSessionStateChangedEventArgs eventArgs)
        {
            UpdateVisibility();
        }

        void Awake()
        {
            mesh = GetComponent<MeshFilter>().mesh;
            m_TrackingAFace = false;
            if (mesh == null)
            {
                Debug.Log("Mesh is null!");
            }
            else
            {
                Debug.Log($"Mesh has {mesh.vertices.Length} vertices to being with");
            }
            m_MeshRenderer = GetComponent<MeshRenderer>();
            m_MeshRenderer.materials = materials.ToArray();
            m_MeshRenderer.sharedMaterials = materials.ToArray();
            // m_Face = GetComponent<ARFace>();
        }

        void OnEnable()
        {
            // m_Face.updated += OnUpdated;
            ARSession.stateChanged += OnSessionStateChanged;
            m_TrackingAFace = false;
            if (m_Face != null)
            {
                UpdateVisibility();
            }
        }

        void OnDisable()
        {
            // m_Face.updated -= OnUpdated;
            ARSession.stateChanged -= OnSessionStateChanged;
        }

        ARFace m_Face;
        MeshRenderer m_MeshRenderer;
        bool m_TopologyUpdatedThisFrame;

        // remove this 
        bool m_TrackingAFace;
        int[] FrontalisTriangles = new int[] {  54, 103, 68,
                                              68, 103, 104,
                                               63, 68, 104,
                                              63, 104, 105,
                                              104, 103, 67,
                                               104, 67, 69,
                                              105, 104, 69,
                                               105, 69, 66,
                                               69, 67, 109,
                                               69, 109,108,
                                               66, 69, 108,
                                              66, 108, 107,
                                              108, 109, 10,
                                             108, 10,  151,
                                             107, 108, 151,
                                               107, 151, 9,
                                              151, 10, 337,
                                              337, 10, 338,
                                               9, 151, 336,
                                             336, 151, 337,
                                             336, 337, 296,
                                             296, 337, 299,
                                             337, 338, 299,
                                             299, 338, 297,
                                             334, 296, 299,
                                             334, 299, 333,
                                             333, 299, 297,
                                             333, 297, 332,
                                             293, 334, 333,
                                             293, 333, 298,
                                             298, 333, 332,
                                             298, 332, 284};
        int[] CorrugatorSupercilliTriangles = new int[] {
                                    65, 66, 107,
                                    65, 107, 55,
                                    65, 55, 222,
                                  295, 336, 296,
                                  295, 285, 336,
                                  442, 285, 295
        };

        int[] ZygoMinor = new int[] {
             116, 34, 143,
            116, 143, 111,
             50, 116, 111,
             50, 111, 117,
             205, 50, 117,
             205, 117, 36,
             206, 205, 36,
             206, 36, 165,
              165, 36, 98,
    //   [143, 111, 117,  36,  98, 165, 206, 205,  50, 116,  34, 143], // 10 Zygomaticus minor
    //   [327, 391, 426, 425, 280, 345, 356, 372, 340, 346, 327], // 10
            391, 327, 426,
            426, 327, 266,
            426, 266, 425,
            425, 266, 280,
            280, 266, 346,
            280, 346, 345,
            345, 346, 340,
            345, 340, 372,
            345, 372, 264
        };

        int[] ZygoMajor = new int[] {
    //   [322, 410, 436, 352, 454, 447, 345, 280, 425, 426, 322], // 11
    //   [92, 186, 216, 123, 234, 227, 116, 50, 205, 206, 92], // 11 Zygo major
            186, 216, 92,
            216, 206, 92,
            216, 205, 206,
            216,  50, 205,
            216, 116, 50,
            216, 123, 116,
            123, 234, 116,
            410, 322, 436,
            436, 322, 425,
            436, 425, 352,
            425, 280, 352,
            352, 280, 345,
            352, 345, 447,
            352, 447, 454
        };

        int[] OrbOris = new int[] {
                  0, 164, 267,
                267, 164, 393,
                267, 393, 269,
                269, 393, 391,
                269, 391, 322,
                269, 322, 270,
                270, 322, 410,
                409, 270, 410,
                409, 410, 287,
                409, 287, 306,
                306, 287, 375,
                287, 375, 273,
                321, 375, 273,
                321, 273, 335,
                406, 321, 335,
                406, 405, 321,
                406, 314, 405,
                313, 314, 406,
                313, 314,  18,
                 18, 314,  17,
                313, 421, 200,
                // right
                 37,  0, 164,
                 37, 167, 164,
                 39, 167,  37,
                 39, 165, 167,
                 // clockwise
                 92, 165,  39,
                 40,  92,  39,
                186, 92, 40,
                185, 186, 40,
                 57, 186, 185,
                 57, 185, 61,
                 57, 61, 146,
                 57, 146, 43,
                 43, 146, 91,
                 43,  91, 106,
                106,  91, 182,
                182,  91, 181,
                182, 181,  84,
                182,  84,  83,
                 83,  84,  18,
                 18,  84,  17

        }; // lips
        int[] ProcerusTriangles = new int[] {
                55, 107,   9,
                55,   9,   8,
               193,  55,   8,
               285,   9, 336,
                 8,   9, 285,
                 8, 285, 417
        };

        int[] DepressorSuper = new int[] {
            417, 285, 441,
            417, 441, 413,
            413, 441, 286,
            413, 286, 414,
            221,  55, 193,
            221, 193, 189,
             56, 221, 189,
             56, 189, 190
        };

        int[] OrbiOculi = new int[] { // super lateral
            441, 285, 442,
            286, 441, 442,
            286, 442, 258,
            258, 442, 443,
            258, 443, 259,
            259, 443, 444,
            259, 444, 260,
            260, 444, 445,
            260, 445, 467,
            467, 445, 342,
            342, 445, 276,
            342, 276, 353,
            353, 276, 300,
            353, 300, 383,
            445, 444, 283,
            445, 283, 276,
            283, 293, 276,
            276, 293, 300,
            444, 443, 282,
            444, 282, 283,
            283, 282, 334,
            283, 334, 293,
            442, 282, 443,
            442, 295, 282,
            295, 296, 282,
            282, 296, 334
        };

        int[] Nasalis = new int[] {
    //   [122, 196, 3, 236, 174, 188, 122], // 7 Nasalis
    //   [351, 419, 248, 456, 399, 412, 351], // 7
        196, 188, 122,
        174, 188, 196,
        236, 174, 196,
        236, 196, 3,
        419, 351, 412,
        419, 412, 399,
        248, 419, 456,
        456, 419, 399
        };

        int[] LevatorLabii = new int[] {
    //   [357, 350, 277, 355, 429, 420, 437, 343, 357],
    //   [128, 121, 47, 126, 209, 198, 217, 114, 128], // 8 Levator labii
        343, 357, 350,
        277, 343, 350,
        437, 343, 277,
        355, 437, 277,
        420, 437, 355,
        429, 420, 355,
        121, 128, 114,
         47, 121, 114,
         47, 114, 217,
        126,  47, 217,
        126, 217, 198,
        209, 126, 198
        };

        int[] LevatorLabii2 = new int[] {
    //   [119, 101, 36, 142, 100, 120, 119], // 9 Levator labii - 2
    //   [349, 329, 371, 266, 330, 348, 349], // 9
        119, 120, 100,
        101, 119, 100,
         36, 101, 100,
         36, 100, 142,
        329, 349, 348,
        329, 348, 330,
        266, 371, 329,
        266, 329, 330,
        };

        int[] Buccinator = new int[] {
    //   [186, 216, 123, 147, 192, 212, 57, 186], // 13 Buccinator
    //   [410, 287, 432, 416, 376, 352, 436, 410], // 13
            287, 410, 432,
            432, 410, 436,
            432, 436, 416,
            416, 436, 376,
            436, 352, 376,

            212, 186, 57,
            212, 216, 186,
            192, 216, 212,
            192, 147, 216,
            147, 123, 216
        };

        int[] Risorius = new int[] {
    //   [212, 192, 215, 214, 58, 172, 136, 150, 214, 212], // 14 Risorius
    //   [416, 435, 434, 288, 397, 365, 379, 434, 432, 416], // 14
            214, 192, 212,
            135, 192, 214,
            135, 138, 192,
            138, 215, 192,
            172,  58, 215,
            172, 215, 138,
            136, 172, 138,
            136, 138, 135,
            150, 136, 135,
            150, 135, 214,

            434, 432, 416,
            379, 434, 364,
            379, 364, 365,
            364, 434, 416,
            364, 416, 367,
            365, 364, 367,
            365, 367, 397,
            367, 416, 435,
            397, 367, 435,
            397, 435, 288
        };
        //   [57, 212, 214, 150, 149, 176, 170, 57], // 16 Depressor anguli oris
        //   [182, 170, 57, 43, 106, 182], // 17 Depressor labii inferioris

        int[] Masseter = new int[] {
    //   [123, 234, 93, 132, 58, 214, 215, 192, 123], // 15 Masseter
    //   [352, 454, 323, 401, 361, 435, 288, 434, 435, 416, 411, 352], // 15

         58, 215, 214,
        215, 213, 192,
        192, 213, 147,
        192, 147, 123,
        147, 137, 123,
        177, 137, 147,
        177,  93, 137,
        132,  93, 177,
         58, 132, 177,
         58, 177, 215,
        215, 177, 213,
        213, 177, 147
        };
        public MeshRenderer arCoreRenderClone;
    }
}
