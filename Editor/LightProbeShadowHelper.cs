using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if (UNITY_EDITOR)
using UnityEditor;
#endif

#if (UNITY_EDITOR)

namespace TechArtUtils
{
    internal class LightProbeShadowHelper : EditorWindow
    {
        internal struct PlacementSettings
        {
            internal Vector3 LightDirection;
            internal int StepsAlongRays;
            internal bool DebugAll;
            internal bool DrawGizmosOnAllCasterVerts;
            internal bool DrawGizmosOnCastedShadowVerts;
            internal bool DrawRaysFromAllCasterVerts;
            internal bool DrawEdgesFromAllCasterVerts;
            internal bool DrawEdgesFromVisibleCasterVerts;
        }

        public struct Edge
        {
            public Vector3 V1 { get; set; }
            public Vector3 V2 { get; set; }
        }

        public List<Edge> EdgesList;

        private Transform _shadowCastingLight;
        private Vector3 _lightDirection;
        private GameObject _shadowCaster;

        private LightProbeGroup _lightProbeGroup;
        private List<MeshFilter> _meshFilters;
        private PlacementSettings _placementSettings;

        private bool _subdivideCaster = true;
        private int _stepsAlongRays;


        private bool _showDebug;
        private bool _drawGizmosOnAllCasterVerts;
        private bool _drawGizmosOnCastedShadowVerts;
        private bool _drawRaysFromAllCasterVerts;
        private bool _drawEdgesFromAllCasterVerts;
        private bool _drawEdgesFromVisibleCasterVerts;

        private List<Vector3> _allShadowEdges;

        private IEnumerable<Collider> _shadowColliders;

        private readonly int _tempCollidersLayer = 28;



        [MenuItem("TA Tools/Light probes shadow helper")]
        public static void ShowSettings()
        {
            GetWindow<LightProbeShadowHelper>();
        }


        private void OnGUI()
        {
            EdgesList = new List<Edge>();

            GUILayout.Label("Light Probe volume shadow helper", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            _lightProbeGroup = FindObjectOfType<LightProbeGroup>();
            if (!_lightProbeGroup)
            {
                GameObject lpgGo = new GameObject("Light Probe Group");
                GameObject lasGO = GameObject.Find("LightAndSky");
                if (!lasGO)
                    lasGO = new GameObject("LightAndSky");
                lpgGo.transform.SetParent(lasGO.transform);
                _lightProbeGroup = lpgGo.AddComponent<LightProbeGroup>();
                Debug.LogWarning("No light probe group found, created one in 'LightAndSky' GO");

                //EditorUtility.DisplayDialog("No light probe group found", "Created one in 'LightAndSky' GO", "Ok");
            }

            EditorGUILayout.BeginHorizontal();
            Light lightObject = EditorGUILayout.ObjectField(_shadowCastingLight, typeof(Light), true) as Light;
            if (lightObject)
            {
                _shadowCastingLight = lightObject.transform;
                _lightDirection = _shadowCastingLight.forward;
            }

            GameObject shadowCasterMainGO =
                EditorGUILayout.ObjectField(_shadowCaster, typeof(GameObject), true) as GameObject;
            if (!shadowCasterMainGO) return;

            _shadowCaster = shadowCasterMainGO;

            MeshRenderer[] allMeshes = shadowCasterMainGO.GetComponentsInChildren<MeshRenderer>();
            IEnumerable<MeshRenderer> allCastingMeshes = from item in allMeshes
                where (item.gameObject.isStatic &&
                       item.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                select item;

            _meshFilters = new List<MeshFilter>();
            foreach (MeshRenderer renderer in allCastingMeshes)
            {
                _meshFilters.Add(renderer.gameObject.GetComponent<MeshFilter>());
            }

            if (_meshFilters.Count == 0)
            {
                EditorGUILayout.EndHorizontal();
                GUILayout.Label("No shadow casters found", EditorStyles.boldLabel);
                return;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            GUILayout.Label("Placement settings", EditorStyles.label);
            EditorGUILayout.Space();

            LPShadowHelperSettings settingsComponent = shadowCasterMainGO.GetComponent<LPShadowHelperSettings>();

            if (!settingsComponent)
            {
                GUILayout.Label("No settings found");
                if (GUILayout.Button("Add settings component"))
                {
                    settingsComponent = shadowCasterMainGO.AddComponent<LPShadowHelperSettings>();
                    settingsComponent.SetPlacementParams(_placementSettings);
                }
            }

            if (settingsComponent)
                _placementSettings = settingsComponent.GetPlacementParams();
            else return;

            _subdivideCaster = EditorGUILayout.Toggle("Subdivide caster", _subdivideCaster);

            if (GUILayout.Button("Create shadow volume"))
            {
                //CreateShadowVolume();
                settingsComponent.CasterAllVerticesList = GetAllCasterVertices();
                settingsComponent.CastedShadowVerticesList = GetAllCastedShadowVertices();
            }

            EditorGUILayout.Space();
            _stepsAlongRays = EditorGUILayout.IntSlider(_stepsAlongRays, 1, 20);
            EditorGUILayout.Space();

            if (GUILayout.Button("Add probes along rays"))
            {
                settingsComponent.VisibleEdgesVertsList = GetAllVisibleCastedEdges();
                PlaceProbesAlongRays();
            }


            EditorGUILayout.Space();
            EditorGUILayout.Separator();
            EditorGUILayout.Space();



            //Debug
            _showDebug = EditorGUILayout.BeginToggleGroup("Debug", _showDebug);
            _drawGizmosOnAllCasterVerts = EditorGUILayout.Toggle("Caster vertices", _drawGizmosOnAllCasterVerts);
            _drawGizmosOnCastedShadowVerts =
                EditorGUILayout.Toggle("Casted shadow vertices", _drawGizmosOnCastedShadowVerts);
            _drawRaysFromAllCasterVerts = EditorGUILayout.Toggle("Caster vertices rays", _drawRaysFromAllCasterVerts);
            _drawEdgesFromAllCasterVerts =
                EditorGUILayout.Toggle("Caster to shadow vertices edges", _drawEdgesFromAllCasterVerts);
            _drawEdgesFromVisibleCasterVerts =
                EditorGUILayout.Toggle("Visible casters", _drawEdgesFromVisibleCasterVerts);


            EditorGUILayout.EndToggleGroup();


            _placementSettings.LightDirection = _lightDirection;
            _placementSettings.DebugAll = _showDebug;
            _placementSettings.DrawGizmosOnAllCasterVerts = _drawGizmosOnAllCasterVerts;
            _placementSettings.DrawGizmosOnCastedShadowVerts = _drawGizmosOnCastedShadowVerts;
            _placementSettings.DrawRaysFromAllCasterVerts = _drawRaysFromAllCasterVerts;
            _placementSettings.DrawEdgesFromAllCasterVerts = _drawEdgesFromAllCasterVerts;
            _placementSettings.DrawEdgesFromVisibleCasterVerts = _drawEdgesFromVisibleCasterVerts;


            settingsComponent.CasterToCastedVerticesList = _allShadowEdges;

            settingsComponent.SetPlacementParams(_placementSettings);
        }


        private void CreateShadowVolume()
        {
        }

        private void PlaceProbesAlongRays()
        {

            const float maxDist = 4.0f;

            List<Vector3> newProbes = _lightProbeGroup.probePositions.ToList();
            foreach (Edge edge in EdgesList)
            {

                newProbes.Add(edge.V1);
                newProbes.Add(edge.V2);
                for (int i = 1; i < _stepsAlongRays; i++)
                {
                    float distance = Vector3.Distance(edge.V1, edge.V2);
                    Vector3 currentPos = Vector3.Lerp(edge.V1, edge.V2, 1.0f / _stepsAlongRays * i);
                    if (Vector3.Distance(edge.V1, currentPos) > maxDist)
                        newProbes.Add(currentPos);
                }
            }

            _lightProbeGroup.probePositions = newProbes.ToArray();


        }


        private void UpdateShadowColliders()
        {
            // create shadow colliders group
            var collidersGroup = GameObject.Find("__shadow_colliders__");
            if (collidersGroup)
                DestroyImmediate(collidersGroup);
            collidersGroup = new GameObject("__shadow_colliders__");
            collidersGroup.transform.SetParent(_shadowCastingLight.transform, true);

            var allMeshes = GameObject.FindObjectsOfType<MeshRenderer>();

            //Selection.objects = allMeshes.Select<MeshRenderer, GameObject>(x => x.gameObject).ToArray();
            var shadowCasters = from item in allMeshes
                where item.gameObject.isStatic && item.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off
                select item;

            //Selection.objects = shadowCasters.Select<MeshRenderer, GameObject>(x => x.gameObject).ToArray();

            var colliders = new List<Collider>();
            foreach (var item in shadowCasters)
            {
                // create collision from visible mesh
                var filter = item.GetComponent<MeshFilter>().sharedMesh;
                if (!filter)
                {
                    Debug.Log(string.Format("No mesh filter on object {0}", item.name));
                    continue;
                }

                var mesh = new Mesh();
                mesh.vertices = filter.vertices;
                mesh.triangles = filter.triangles;

                // world space transform
                var tm = item.gameObject.transform.localToWorldMatrix;

                // dummy gameobject
                var go = new GameObject();
                go.transform.position = tm.GetColumn(3);
                go.transform.rotation = Quaternion.LookRotation(tm.GetColumn(2), tm.GetColumn(1));
                go.transform.localScale = new Vector3(tm.GetColumn(0).magnitude, tm.GetColumn(1).magnitude,
                    tm.GetColumn(2).magnitude);
                go.transform.SetParent(collidersGroup.transform, true);

                go.layer = _tempCollidersLayer;


                // create mesh collider
                var col = go.AddComponent<MeshCollider>();
                col.sharedMesh = mesh;
                colliders.Add(col);
            }

            // special case -- terrain object
            var terr = GameObject.FindObjectOfType<TerrainCollider>();
            if (terr)
                colliders.Add(terr);

            // update colliders
            _shadowColliders = colliders.ToArray();
        }


        void CleanupShadowColliders()
        {
            if (_shadowColliders.Count() > 0)
            {
                DestroyImmediate(_shadowColliders.First().gameObject.transform.parent.gameObject);
                _shadowColliders = null;
            }
        }


        private List<Vector3> GetAllCasterVertices()
        {
            if (_subdivideCaster)
            {
                GameObject subdHelper = new GameObject();
                subdHelper.transform.SetPositionAndRotation(_shadowCaster.transform.position,
                    _shadowCaster.transform.rotation);
                subdHelper.transform.SetParent(_shadowCaster.transform);

                List<MeshFilter> subdividedMeshes = new List<MeshFilter>(_meshFilters);
                ;
                foreach (MeshFilter meshFilter in subdividedMeshes)
                {
                    GameObject subObjectHelper = new GameObject();
                    subObjectHelper.transform.SetParent(subdHelper.transform);

                    MeshFilter currMf = subObjectHelper.AddComponent<MeshFilter>();
                    currMf.sharedMesh = Instantiate(meshFilter.sharedMesh);
                    MeshHelper.Subdivide(currMf.sharedMesh);
                }

                _meshFilters = subdividedMeshes;
            }

            List<Vector3> allVertices = new List<Vector3>();

            foreach (MeshFilter meshFilter in _meshFilters)
            {
                foreach (Vector3 meshVertex in meshFilter.sharedMesh.vertices)
                {
                    allVertices.Add(_shadowCaster.transform.localToWorldMatrix.MultiplyPoint3x4(meshVertex));
                }
            }

            allVertices = allVertices.Distinct().ToList();

            return allVertices;
        }


        private List<Vector3> GetAllVisibleCastedEdges()
        {
            List<Vector3> castedVerts = GetAllCastedShadowVertices();
            RaycastHit tempHit;

            EdgesList.Clear();
            List<Vector3> edgeVerts = new List<Vector3>();
            Edge foundEdge = new Edge();
            Vector3 lightDirection = _shadowCastingLight.forward;
            foreach (Vector3 castedVert in castedVerts)
            {
                if (Physics.Raycast(castedVert, -lightDirection, out tempHit, 500f))
                {
                    foundEdge.V1 = castedVert;
                    foundEdge.V2 = tempHit.point;
                    EdgesList.Add(foundEdge);
                    edgeVerts.Add(castedVert);
                    edgeVerts.Add(tempHit.point);
                }
            }

            return edgeVerts;
        }

        private List<Vector3> GetAllCastedShadowVertices()
        {

            UpdateShadowColliders();
            List<Vector3> allCasterVertices = GetAllCasterVertices();
            List<Vector3> allCastedShadowVertices = new List<Vector3>();

            _allShadowEdges = new List<Vector3>();

            Vector3 lightDirection = _shadowCastingLight.forward;

            RaycastHit tempHit = new RaycastHit();

            foreach (Vector3 casterVertex in allCasterVertices)
            {
                if (Physics.Raycast(casterVertex, lightDirection, out tempHit, 500f))
                {
                    allCastedShadowVertices.Add(tempHit.point);
                    _allShadowEdges.Add(casterVertex);
                    _allShadowEdges.Add(tempHit.point);
                }
            }

            CleanupShadowColliders();
            return allCastedShadowVertices;
        }

    }

#endif


    public class LPShadowHelperSettings : MonoBehaviour
    {

        [NonSerialized] public List<Vector3> CasterAllVerticesList;
        [NonSerialized] public List<Vector3> CastedShadowVerticesList;
        [NonSerialized] public List<Vector3> CasterToCastedVerticesList;
        [NonSerialized] public List<Vector3> VisibleEdgesVertsList;

        [SerializeField] private LightProbeShadowHelper.PlacementSettings _lightProbesPlacementSettings;

        private void Awake()
        {
            CasterAllVerticesList = new List<Vector3>();
            CastedShadowVerticesList = new List<Vector3>();
            CasterToCastedVerticesList = new List<Vector3>();
            VisibleEdgesVertsList = new List<Vector3>();

        }


        internal void SetPlacementParams(LightProbeShadowHelper.PlacementSettings settingsToSet)
        {
            _lightProbesPlacementSettings = settingsToSet;
        }

        internal LightProbeShadowHelper.PlacementSettings GetPlacementParams()
        {
            return _lightProbesPlacementSettings;
        }


#if (UNITY_EDITOR)

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.gray;

            if (!_lightProbesPlacementSettings.DebugAll)
                return;


            if (_lightProbesPlacementSettings.DrawGizmosOnAllCasterVerts && CasterAllVerticesList != null)
            {
                foreach (Vector3 vertex in CasterAllVerticesList)
                {
                    Gizmos.DrawSphere(vertex, 1f);
                }
            }

            if (_lightProbesPlacementSettings.DrawGizmosOnCastedShadowVerts && CastedShadowVerticesList != null)
            {
                foreach (Vector3 vertex in CastedShadowVerticesList)
                {
                    Gizmos.DrawSphere(vertex, 1f);
                }
            }

            if (_lightProbesPlacementSettings.DrawRaysFromAllCasterVerts && CasterAllVerticesList != null)
            {
                foreach (Vector3 vertex in CasterAllVerticesList)
                {
                    Gizmos.DrawRay(vertex, _lightProbesPlacementSettings.LightDirection * 50);
                }
            }

            if (_lightProbesPlacementSettings.DrawEdgesFromAllCasterVerts && CasterToCastedVerticesList != null)
            {
                for (int i = 0; i < CasterToCastedVerticesList.Count - 1; i += 2)
                {
                    Gizmos.DrawLine(CasterToCastedVerticesList[i], CasterToCastedVerticesList[i + 1]);
                }
            }

            if (_lightProbesPlacementSettings.DrawEdgesFromVisibleCasterVerts && CasterToCastedVerticesList != null)
            {
                for (int i = 0; i < VisibleEdgesVertsList.Count - 1; i += 2)
                {
                    Gizmos.DrawLine(VisibleEdgesVertsList[i], VisibleEdgesVertsList[i + 1]);
                }
            }

        }
    }

    public static class MeshHelper
    {
        private static List<Vector3> vertices;

        private static List<Vector3> normals;

        // [... all other vertex data arrays you need]

        private static List<int> indices;
        private static Dictionary<uint, int> newVectices;

        private static int GetNewVertex(int i1, int i2)
        {
            // We have to test both directions since the edge
            // could be reversed in another triangle
            uint t1 = ((uint) i1 << 16) | (uint) i2;
            uint t2 = ((uint) i2 << 16) | (uint) i1;
            if (newVectices.ContainsKey(t2))
                return newVectices[t2];
            if (newVectices.ContainsKey(t1))
                return newVectices[t1];

            // generate vertex:
            int newIndex = vertices.Count;
            newVectices.Add(t1, newIndex);

            // calculate new vertex
            vertices.Add((vertices[i1] + vertices[i2]) * 0.5f);
            normals.Add((normals[i1] + normals[i2]).normalized);

            // [... all other vertex data arrays]

            return newIndex;
        }


        public static void Subdivide(Mesh mesh)
        {
            newVectices = new Dictionary<uint, int>();

            vertices = new List<Vector3>(mesh.vertices);
            normals = new List<Vector3>(mesh.normals);

            // [... all other vertex data arrays]
            indices = new List<int>();

            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i1 = triangles[i + 0];
                int i2 = triangles[i + 1];
                int i3 = triangles[i + 2];

                int a = GetNewVertex(i1, i2);
                int b = GetNewVertex(i2, i3);
                int c = GetNewVertex(i3, i1);
                indices.Add(i1);
                indices.Add(a);
                indices.Add(c);
                indices.Add(i2);
                indices.Add(b);
                indices.Add(a);
                indices.Add(i3);
                indices.Add(c);
                indices.Add(b);
                indices.Add(a);
                indices.Add(b);
                indices.Add(c); // center triangle
            }

            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();

            // [... all other vertex data arrays]
            mesh.triangles = indices.ToArray();

            // since this is a static function and it uses static variables
            // we should erase the arrays to free them:
            newVectices = null;
            vertices = null;
            normals = null;

            // [... all other vertex data arrays]

            indices = null;
        }
    }
}
#endif