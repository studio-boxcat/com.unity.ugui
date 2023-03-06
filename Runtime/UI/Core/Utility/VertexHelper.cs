using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace UnityEngine.UI
{
    /// <summary>
    /// A utility class that can aid in the generation of meshes for the UI.
    /// </summary>
    /// <remarks>
    /// This class implements IDisposable to aid with memory management.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.UI;
    ///
    /// public class ExampleClass : MonoBehaviour
    /// {
    ///     Mesh m;
    ///
    ///     void Start()
    ///     {
    ///         Color32 color32 = Color.red;
    ///         using (var vh = new VertexHelper())
    ///         {
    ///             vh.AddVert(new Vector3(0, 0), color32, new Vector2(0f, 0f));
    ///             vh.AddVert(new Vector3(0, 100), color32, new Vector2(0f, 1f));
    ///             vh.AddVert(new Vector3(100, 100), color32, new Vector2(1f, 1f));
    ///             vh.AddVert(new Vector3(100, 0), color32, new Vector2(1f, 0f));
    ///
    ///             vh.AddTriangle(0, 1, 2);
    ///             vh.AddTriangle(2, 3, 0);
    ///             vh.FillMesh(m);
    ///         }
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public class VertexHelper : IDisposable
    {
        private List<Vector3> m_Positions;
        private List<Color32> m_Colors;
        private List<Vector4> m_Uv0S;
        private List<Vector4> m_Uv1S;
        private List<int> m_Indices;

        private bool m_ListsInitalized = false;

        public VertexHelper()
        {}

        public VertexHelper(Mesh m)
        {
            InitializeListIfRequired();

            m_Positions.AddRange(m.vertices);
            m_Colors.AddRange(m.colors32);
            List<Vector4> tempUVList = new List<Vector4>();
            m.GetUVs(0, tempUVList);
            m_Uv0S.AddRange(tempUVList);
            m.GetUVs(1, tempUVList);
            m_Uv1S.AddRange(tempUVList);
            m_Indices.AddRange(m.GetIndices(0));
        }

        private void InitializeListIfRequired()
        {
            if (!m_ListsInitalized)
            {
                m_Positions = ListPool<Vector3>.Get();
                m_Colors = ListPool<Color32>.Get();
                m_Uv0S = ListPool<Vector4>.Get();
                m_Uv1S = ListPool<Vector4>.Get();
                m_Indices = ListPool<int>.Get();
                m_ListsInitalized = true;
            }
        }

        /// <summary>
        /// Cleanup allocated memory.
        /// </summary>
        public void Dispose()
        {
            if (m_ListsInitalized)
            {
                ListPool<Vector3>.Release(m_Positions);
                ListPool<Color32>.Release(m_Colors);
                ListPool<Vector4>.Release(m_Uv0S);
                ListPool<Vector4>.Release(m_Uv1S);
                ListPool<int>.Release(m_Indices);

                m_Positions = null;
                m_Colors = null;
                m_Uv0S = null;
                m_Uv1S = null;
                m_Indices = null;

                m_ListsInitalized = false;
            }
        }

        /// <summary>
        /// Clear all vertices from the stream.
        /// </summary>
        public void Clear()
        {
            // Only clear if we have our lists created.
            if (m_ListsInitalized)
            {
                m_Positions.Clear();
                m_Colors.Clear();
                m_Uv0S.Clear();
                m_Uv1S.Clear();
                m_Indices.Clear();
            }
        }

        /// <summary>
        /// Current number of vertices in the buffer.
        /// </summary>
        public int currentVertCount
        {
            get { return m_Positions != null ? m_Positions.Count : 0; }
        }

        /// <summary>
        /// Get the number of indices set on the VertexHelper.
        /// </summary>
        public int currentIndexCount
        {
            get { return m_Indices != null ? m_Indices.Count : 0; }
        }

        /// <summary>
        /// Fill a UIVertex with data from index i of the stream.
        /// </summary>
        /// <param name="vertex">Vertex to populate</param>
        /// <param name="i">Index to populate.</param>
        public void PopulateUIVertex(ref UIVertex vertex, int i)
        {
            InitializeListIfRequired();

            vertex.position = m_Positions[i];
            vertex.color = m_Colors[i];
            vertex.uv0 = m_Uv0S[i];
            vertex.uv1 = m_Uv1S[i];
        }

        /// <summary>
        /// Set a UIVertex at the given index.
        /// </summary>
        /// <param name="vertex">The vertex to fill</param>
        /// <param name="i">the position in the current list to fill.</param>
        public void SetUIVertex(UIVertex vertex, int i)
        {
            InitializeListIfRequired();

            m_Positions[i] = vertex.position;
            m_Colors[i] = vertex.color;
            m_Uv0S[i] = vertex.uv0;
            m_Uv1S[i] = vertex.uv1;
        }

        /// <summary>
        /// Fill the given mesh with the stream data.
        /// </summary>
        public void FillMesh(Mesh mesh)
        {
            InitializeListIfRequired();

            mesh.Clear();

            if (m_Positions.Count >= 65000)
                throw new ArgumentException("Mesh can not have more than 65000 vertices");

            mesh.SetVertices(m_Positions);
            mesh.SetColors(m_Colors);
            mesh.SetUVs(0, m_Uv0S);
            mesh.SetUVs(1, m_Uv1S);
            mesh.SetTriangles(m_Indices, 0);
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Add a single vertex to the stream.
        /// </summary>
        /// <param name="position">Position of the vert</param>
        /// <param name="color">Color of the vert</param>
        /// <param name="uv0">UV of the vert</param>
        /// <param name="uv1">UV1 of the vert</param>
        /// <param name="uv2">UV2 of the vert</param>
        /// <param name="uv3">UV3 of the vert</param>
        /// <param name="normal">Normal of the vert.</param>
        /// <param name="tangent">Tangent of the vert</param>
        public void AddVert(Vector3 position, Color32 color, Vector4 uv0, Vector4 uv1)
        {
            InitializeListIfRequired();

            m_Positions.Add(position);
            m_Colors.Add(color);
            m_Uv0S.Add(uv0);
            m_Uv1S.Add(uv1);
        }

        /// <summary>
        /// Add a single vertex to the stream.
        /// </summary>
        /// <param name="position">Position of the vert</param>
        /// <param name="color">Color of the vert</param>
        /// <param name="uv0">UV of the vert</param>
        public void AddVert(Vector3 position, Color32 color, Vector4 uv0)
        {
            AddVert(position, color, uv0, Vector4.zero);
        }

        /// <summary>
        /// Add a single vertex to the stream.
        /// </summary>
        /// <param name="v">The vertex to add</param>
        public void AddVert(UIVertex v)
        {
            AddVert(v.position, v.color, v.uv0, v.uv1);
        }

        /// <summary>
        /// Add a triangle to the buffer.
        /// </summary>
        /// <param name="idx0">index 0</param>
        /// <param name="idx1">index 1</param>
        /// <param name="idx2">index 2</param>
        public void AddTriangle(int idx0, int idx1, int idx2)
        {
            InitializeListIfRequired();

            m_Indices.Add(idx0);
            m_Indices.Add(idx1);
            m_Indices.Add(idx2);
        }

        /// <summary>
        /// Add a quad to the stream.
        /// </summary>
        /// <param name="verts">4 Vertices representing the quad.</param>
        public void AddUIVertexQuad(UIVertex[] verts)
        {
            int startIndex = currentVertCount;

            for (int i = 0; i < 4; i++)
                AddVert(verts[i].position, verts[i].color, verts[i].uv0);

            AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }

        /// <summary>
        /// Add a stream of custom UIVertex and corresponding indices.
        /// </summary>
        /// <param name="verts">The custom stream of verts to add to the helpers internal data.</param>
        /// <param name="indices">The custom stream of indices to add to the helpers internal data.</param>
        public void AddUIVertexStream(List<UIVertex> verts, List<int> indices)
        {
            InitializeListIfRequired();

            if (verts != null)
            {
                CanvasRenderer.AddUIVertexStream(verts, m_Positions, m_Colors, m_Uv0S, m_Uv1S, m_Normals, m_Tangents);
            }

            if (indices != null)
            {
                m_Indices.AddRange(indices);
            }
        }

        /// <summary>
        /// Add a list of triangles to the stream.
        /// </summary>
        /// <param name="verts">Vertices to add. Length should be divisible by 3.</param>
        public void AddUIVertexTriangleStream(List<UIVertex> verts)
        {
            if (verts == null)
                return;

            InitializeListIfRequired();

            CanvasRenderer.SplitUIVertexStreams(verts, m_Positions, m_Colors, m_Uv0S, m_Uv1S, m_Normals, m_Tangents, m_Indices);
            ClearFakeNormalsAndTangents();
        }

        /// <summary>
        /// Create a stream of UI vertex (in triangles) from the stream.
        /// </summary>
        public void GetUIVertexStream(List<UIVertex> stream)
        {
            if (stream == null)
                return;

            InitializeListIfRequired();

            CanvasRenderer.CreateUIVertexStream(stream, m_Positions, m_Colors, m_Uv0S, m_Uv1S, m_Normals, m_Tangents, m_Indices);
        }

        private static readonly List<Vector3> m_Normals = new();
        private static readonly List<Vector4> m_Tangents = new();

        private static void ClearFakeNormalsAndTangents()
        {
            m_Normals.Clear();
            m_Tangents.Clear();
        }
    }
}
