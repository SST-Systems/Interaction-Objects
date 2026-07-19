using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace SST.InteractionObjects
{
    /// <summary>
    /// Draws a solid-colour outline around an object's silhouette.
    /// </summary>
    /// <remarks>
    /// The effect is an inverted-hull outline made of two materials added to each
    /// renderer: a <b>mask</b> that writes the silhouette into the stencil buffer, and
    /// a <b>fill</b> that extrudes the mesh along its smoothed normals and colours the
    /// ring around the object (skipping the masked pixels). Two single-pass materials
    /// are used deliberately — the Built-in Render Pipeline and URP both draw them
    /// reliably, whereas a single multi-pass material is not drawn correctly in URP.
    ///
    /// Attach it to any object with one or more <see cref="Renderer"/>s (mesh or
    /// skinned) and switch it on or off with <see cref="Behaviour.enabled"/>.
    /// Colour, width and visibility <see cref="Mode"/> can be changed at runtime.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Outline : MonoBehaviour
    {
        /// <summary>Controls when the outline is drawn relative to scene depth.</summary>
        public enum Mode
        {
            /// <summary>Always visible, even through occluding geometry.</summary>
            OutlineAll,

            /// <summary>Visible only where the object itself is not occluded.</summary>
            OutlineVisible,

            /// <summary>Visible only where the object is occluded (an x-ray highlight).</summary>
            OutlineHidden
        }

        private const string MaskShaderName = "SST/Interaction Objects/Outline Mask";
        private const string FillShaderName = "SST/Interaction Objects/Outline Fill";
        private const string MaskFallbackResource = "SSTOutlineMask";
        private const string FillFallbackResource = "SSTOutlineFill";
        private const int SmoothNormalsUvChannel = 3;
        private const float WidthToClipScale = 0.005f;
        
        private static readonly HashSet<Mesh> BakedMeshes = new HashSet<Mesh>();

        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
        private static readonly int ZTestId = Shader.PropertyToID("_ZTest");

        [SerializeField] private Mode _outlineMode = Mode.OutlineVisible;
        [SerializeField] private Color _outlineColor = Color.white;
        [SerializeField, Range(0f, 10f)] private float _outlineWidth = 4f;

        private Renderer[] _renderers;
        private Material _maskMaterial;
        private Material _fillMaterial;
        private bool _isDirty;

        /// <summary>When the outline is drawn relative to scene depth.</summary>
        public Mode OutlineMode
        {
            get => _outlineMode;
            set { _outlineMode = value; _isDirty = true; }
        }

        /// <summary>Outline colour. The alpha channel controls opacity.</summary>
        public Color OutlineColor
        {
            get => _outlineColor;
            set { _outlineColor = value; _isDirty = true; }
        }

        /// <summary>Outline thickness in screen space (roughly percent of height).</summary>
        public float OutlineWidth
        {
            get => _outlineWidth;
            set { _outlineWidth = value; _isDirty = true; }
        }

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();

            _maskMaterial = new Material(ResolveShader(MaskShaderName, MaskFallbackResource)) { name = "SST Outline Mask (Instance)" };
            _fillMaterial = new Material(ResolveShader(FillShaderName, FillFallbackResource)) { name = "SST Outline Fill (Instance)" };

            BakeSmoothNormals();

            _isDirty = true;
        }

        private void OnEnable()
        {
            foreach (var renderer in _renderers)
                AddOutlineMaterials(renderer);
        }

        private void OnDisable()
        {
            foreach (var renderer in _renderers)
                RemoveOutlineMaterials(renderer);
        }

        private void OnValidate() => _isDirty = true;

        private void Update()
        {
            if (!_isDirty)
                return;

            _isDirty = false;
            ApplyMaterialProperties();
        }

        private void OnDestroy()
        {
            if (_maskMaterial != null)
                Destroy(_maskMaterial);

            if (_fillMaterial != null)
                Destroy(_fillMaterial);
        }

        private static Shader ResolveShader(string shaderName, string fallbackResource)
        {
            var shader = Shader.Find(shaderName);
            return shader != null ? shader : Resources.Load<Shader>(fallbackResource);
        }

        private void ApplyMaterialProperties()
        {
            if (_fillMaterial == null)
                return;

            _fillMaterial.SetColor(OutlineColorId, _outlineColor);
            _fillMaterial.SetFloat(OutlineWidthId, _outlineWidth * WidthToClipScale);
            _fillMaterial.SetFloat(ZTestId, (float)ResolveZTest(_outlineMode));
        }

        private static CompareFunction ResolveZTest(Mode mode)
        {
            switch (mode)
            {
                case Mode.OutlineAll: return CompareFunction.Always;
                case Mode.OutlineHidden: return CompareFunction.Greater;
                default: return CompareFunction.LessEqual;
            }
        }
        
        private void AddOutlineMaterials(Renderer renderer)
        {
            var materials = renderer.sharedMaterials.ToList();

            var changed = false;
            if (!materials.Contains(_maskMaterial)) { materials.Add(_maskMaterial); changed = true; }
            if (!materials.Contains(_fillMaterial)) { materials.Add(_fillMaterial); changed = true; }

            if (changed)
                renderer.materials = materials.ToArray();
        }

        private void RemoveOutlineMaterials(Renderer renderer)
        {
            var materials = renderer.sharedMaterials.ToList();

            var changed = materials.Remove(_maskMaterial);
            changed |= materials.Remove(_fillMaterial);

            if (changed)
                renderer.materials = materials.ToArray();
        }

        private void BakeSmoothNormals()
        {
            foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
                BakeSmoothNormals(meshFilter.sharedMesh);

            foreach (var skinnedRenderer in GetComponentsInChildren<SkinnedMeshRenderer>())
                BakeSmoothNormals(skinnedRenderer.sharedMesh);
        }

        private static void BakeSmoothNormals(Mesh mesh)
        {
            if (mesh == null || !BakedMeshes.Add(mesh))
                return;

            mesh.SetUVs(SmoothNormalsUvChannel, ComputeSmoothNormals(mesh));
        }

        /// <summary>
        /// Averages the normals of every vertex that shares a position, so that hard
        /// edges (split normals) extrude into a continuous shell without gaps.
        /// </summary>
        private static List<Vector3> ComputeSmoothNormals(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var smoothNormals = new List<Vector3>(normals);

            var groupsByPosition = new Dictionary<Vector3, List<int>>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                if (!groupsByPosition.TryGetValue(vertices[i], out var indices))
                {
                    indices = new List<int>();
                    groupsByPosition.Add(vertices[i], indices);
                }

                indices.Add(i);
            }

            foreach (var indices in groupsByPosition.Values)
            {
                if (indices.Count < 2)
                    continue;

                var averaged = Vector3.zero;
                foreach (var index in indices)
                    averaged += normals[index];

                averaged.Normalize();

                foreach (var index in indices)
                    smoothNormals[index] = averaged;
            }

            return smoothNormals;
        }
    }
}