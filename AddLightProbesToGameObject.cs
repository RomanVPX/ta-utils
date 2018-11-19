using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;


#if UNITY_EDITOR

namespace TechArtUtils

{
    [ExecuteInEditMode]
    public class AddLightProbesToGameObject : MonoBehaviour
    {   
        [Header("Game objects")]
        public LightProbeGroup SceneLpg;
        public Light MainDirectionalLight;
        public GameObject CasterObject;
        public GameObject ShadowPivot;
        
        [Header("Options")]
        public bool RemoveDuplicates = true;
        public bool OptimizeVertices = true;
        [Range(0f, 50f)] public float RemoveDistanceThreshold = 5f;
        
        [HideInInspector] public bool RemoveBackCast = true;
        public bool AccurateRecievers = false;
        public bool AccurateCasters = false;
        public bool MeshCollidersAreDirty = false; 
        //public bool TesselateCasters = false;
        
        [Range(0f, 1000f)] public float ShadowRaycastDistance = 300f;
        [Range(0f, 50f)] public float BackCastRemoveDistance = 5.0f;
        [Range(0f, 300f)] public float BackCastDistanceOffset = 2.0f;

        [Header("Lower light probes")]
        [Range(0f, 50f)] public float LiftAboveGround = 0.8f;
        public bool UseShadowPivot = false;
        public bool ProbesSeparateOffsetFWD = false;
        public bool ProbesSeparateScale = true;
        public bool SeparateScaleDouble = true;
        public bool CheckCollidersOnLPs = false;
        [Range(0f, 50f)] public float SeparateShadowSeparateFWD = 5.0f;
        [Range(0.5f, 2f)] public float SeparateShadowScaleX = 1.0f;
        [Range(0.5f, 2f)] public float SeparateShadowScaleY = 1.0f;
        [Range(0.5f, 2f)] public float SeparateShadowScaleZ = 1.0f;
        public bool AddAlongEdgeUpwards = false;
        [Range(1, 25)] public int AddUpwardsNumber = 1;
        [Range(0f, 100)] public float AddUpwardsDistance = 4f;
        public bool AdjustToCasterDistance =false;
        public bool PlaceEvenly = true;
        public bool StopOnCollider = false;
        //public bool KeepProbesOnTheGround = false;
        
        [Header("Upper light probes")]
        public bool MakeUpperLightProbes = false;
        public bool SeparateVerticaly = true;
        [Range(0f, 5f)] public float SeparateAlongEdgeDistance = 0.1f;
        [Range(0f, 300f)] public float SeparateMoveBeforeColl = 100.0f;
        [Range(0f, 50f)] public float LiftUpperAmongNormal = 0.0f;
        
        [Header("Debug")]
        public bool DisplayVertexGizmos = true;
        [Range(0f, 3f)] public float VertexGizmoSize = 1.0f;
        public bool DisplayShadowGizmos = true;
        [Range(0f, 3f)] public float ShadowGizmoSize = 1.0f;
        public bool DisplayBackCastDebug = false;
        [Range(0f, 3f)] public float BackCastDebugGizmoSize = 1.0f;
        public bool LowerLightProbes = true;
        [Range(0f, 3f)] public float LightProbesGizmoSize = 1.0f;
        public bool UpperLightProbes = true;
        [Range(0f, 3f)] public float UpperLightProbesGizmoSize = 1.0f;

        
        [Header("Make LPs")]
        public bool OptimizeLightProbes = false;
        [Range(0f, 50f)] public float LPRemoveDistanceThreshold = 5f;

        public bool AddToLPGroup = false;
        public bool LPGIsDirty = true;

        [Header("Utils")] 
        public bool FindDarkProbes = false;
        [Range(0f, 1f)] public float IntensityThreshold = 0.1f;
        public bool RemoveDarkProbes = false;

        private struct ShadowEdgesAndHitsStruct
        {
            public ShadowEdgesAndHitsStruct(Vector3 source, Vector3 destination, RaycastHit hit) : this()
            {
                Source = source;
                Destination = destination;
                Hit = hit;
            }

            public RaycastHit Hit { get; set; }
            public Vector3 Destination { get; set; }
            public Vector3 Source { get; set; }
        }
        
        private List<RaycastHit> _shadowRaycastHits = new List<RaycastHit>();
        private Matrix4x4 _lightMatrix;
        private Vector3 _lightForwardDirection;
        private Matrix4x4 _casterMatrix;
        private List<Vector3> _renderersVertices = new List<Vector3>();
        private List<ShadowEdgesAndHitsStruct> _shadowEdges = new List<ShadowEdgesAndHitsStruct>();
        private List<KeyValuePair<Vector3, Vector3>> _shadowEdgesBackCastDebug = new List<KeyValuePair<Vector3, Vector3>>();
        private List<Vector3> _newProbes = new List<Vector3>();
        private List<Vector3> _upperProbes = new List<Vector3>();
        private List<MeshRenderer> _casterRenderers;

        private List<MeshCollider> _casterCollidersToRestore = new List<MeshCollider>();
        private List<MeshCollider> _otherCollidersToRestore = new List<MeshCollider>();

        private GameObject _tempMeshCollidersGO;
        private bool _otherCollidersAreDisabled = true;
        private List<Vector3> _darkLPsMarking = new List<Vector3>();
        
        

        
		private LayerMask _castLayerMask = 8;

        void Start()
        {
            InitializeLightprobeGroup();
            FindMainLight();
           
            CasterObject = gameObject;
            
            if(CasterObject)
                _casterRenderers = GetAllCastingRenderersInGOHierarchy();
        }


        private void FindMainLight()
        {
            List<Light> allLights = FindObjectsOfType<Light>().ToList();
            if (allLights.Count <= 0) return;

            allLights.RemoveAll(x => x.type != LightType.Directional);
            allLights.Sort((dir1, dir2) => dir1.intensity.CompareTo(dir2.intensity));
            if (allLights.Count > 0)
                MainDirectionalLight = allLights[0];
        }
        
        
        private void InitializeLightprobeGroup()
        {
            SceneLpg = FindObjectOfType<LightProbeGroup>();
            if (SceneLpg) return;

            GameObject lpgGo = new GameObject("Light Probe Group");
            GameObject lasGO = GameObject.Find("LightAndSky");
            if (!lasGO)
                lasGO = new GameObject("LightAndSky");
            lpgGo.transform.SetParent(lasGO.transform);
            SceneLpg = lpgGo.AddComponent<LightProbeGroup>();
            Debug.LogWarning("No light probe group found, created one in 'LightAndSky' GO");
        }

        
        private void OnValidate()
        {
            if(_shadowEdgesBackCastDebug != null && _shadowEdgesBackCastDebug.Count>0)
                _shadowEdgesBackCastDebug.Clear();
            else
            {
                _shadowEdgesBackCastDebug = new List<KeyValuePair<Vector3, Vector3>>();
            }

            if (CasterObject)
            {
                if(_casterRenderers != null && _casterRenderers.Count>0)
                    _casterRenderers.Clear();
                else
                {
                    _casterRenderers = new List<MeshRenderer>();
                }
                
                _casterRenderers = GetAllCastingRenderersInGOHierarchy();
            }

            if (MainDirectionalLight)
            {
                _lightMatrix = MainDirectionalLight.transform.localToWorldMatrix;
                _lightForwardDirection = _lightMatrix.MultiplyVector(Vector3.forward);
            }

            if (_casterRenderers.Count <= 0 || !MainDirectionalLight) return;
			
            GetAllVerticesFromAllCasterRenderers();
            
            
            if(_renderersVertices.Count<=0)
                return;

            if (OptimizeVertices)
                OptimizeVerticesByDistance(null,0f);

            SwitchCasterColliders(!AccurateCasters);
            
            MakeShadowEdges();

            if (_otherCollidersAreDisabled != AccurateRecievers)
                SwitchAllColliders(!AccurateCasters);

            if (_shadowEdges.Count <= 0)
                return;
            
            if(RemoveBackCast)
                RemoveBackCastedEdges();

            if(ProbesSeparateOffsetFWD||ProbesSeparateScale)
                GetShadowSeparatedProbes();
            else
                MakeBaseLowerProbes();

            if (AddAlongEdgeUpwards && _newProbes.Count > 0)
                AddProbesAlongEdge();
            
            if(MakeUpperLightProbes&&!SeparateVerticaly)
                MakeBaseUpperProbes();

            if (SeparateVerticaly&&MakeUpperLightProbes)
                SeparateUpperProbes();

            if (!MakeUpperLightProbes && _upperProbes.Count>0)
                _upperProbes.Clear();

            if (OptimizeLightProbes)
            {
                List<Vector3> combinedProbes = new List<Vector3>();
                combinedProbes.AddRange(_newProbes);
                combinedProbes.AddRange(_upperProbes);
                _newProbes = OptimizeVerticesByDistance(combinedProbes,LPRemoveDistanceThreshold);
                _upperProbes.Clear();
            }

            if (AddToLPGroup && LPGIsDirty)
            {
                CombineAndAddToLPGroup();
                LPGIsDirty = false;
                AddToLPGroup = false; 
            }

            if (FindDarkProbes)
                FindAndRemoveDarkProbes(IntensityThreshold, false);
            
            if (RemoveDarkProbes)
            {
                FindAndRemoveDarkProbes(IntensityThreshold, false);
                RemoveDarkProbes = false;
            }

        }


        private void FindAndRemoveDarkProbes(float intensityThreshold, bool remove)
        {
            SphericalHarmonicsL2[] bakedProbes = LightmapSettings.lightProbes.bakedProbes;
            foreach (SphericalHarmonicsL2 probeIntensity in bakedProbes)
            {
//                SphericalHarmonicsL2 sphericalHarmonicsL2 = probeIntensity;
//                {
//                    if(probeIntensity[rgb:0,coefficient:0] < intensityThreshold)
//                }
            }
        }

        private void CombineAndAddToLPGroup()
        {
            List<Vector3> allPositions = SceneLpg.probePositions.Select(lpgProbePosition => SceneLpg.transform.TransformPoint(lpgProbePosition)).ToList();

            allPositions.AddRange(_upperProbes);
            allPositions.AddRange(_newProbes);

            List<Vector3> localPositions =
                allPositions.Select(p => SceneLpg.transform.InverseTransformPoint(p)).ToList();
            
            SceneLpg.probePositions = localPositions.ToArray();

        }
        
        private void InitShadowPivot()
        {
            if (ShadowPivot) return;

            ShadowPivot = new GameObject("_Shadow_Pivot");
            ShadowPivot.transform.SetParent(CasterObject.transform);
            ShadowPivot.transform.localPosition = Vector3.zero;
            ShadowPivot.transform.localPosition = Vector3.zero;
            
        }

        private void SwitchCasterColliders(bool enable)
        {
            if (_casterCollidersToRestore.Count <= 0)
            {
                _casterCollidersToRestore = CasterObject.GetComponentsInChildren<MeshCollider>().ToList();
            }
        
            
            if (_casterCollidersToRestore.Count <= 0) return;

            foreach (MeshCollider meshCollider in _casterCollidersToRestore)
            {
                meshCollider.enabled = enable;
            }
            
        }

        private void SwitchAllColliders(bool enable)
        {
            if (_otherCollidersToRestore.Count <= 0)
            {
                _otherCollidersToRestore = FindObjectsOfType<MeshCollider>().Distinct()
                    .Except(_casterCollidersToRestore).ToList();
            }
           
            if(_otherCollidersToRestore.Count<=0) return;

            foreach (MeshCollider meshCollider in _otherCollidersToRestore)
            {
                meshCollider.enabled = enable;
            }

            _otherCollidersAreDisabled = enable;
        }
        

        
        private void MakeBaseLowerProbes()
        {
            if(_newProbes.Count>0)
                _newProbes.Clear();
            
            foreach (ShadowEdgesAndHitsStruct shadowEdge in _shadowEdges)
            {
                _newProbes.Add(shadowEdge.Destination);
            }
        }
        
        
        
        private void MakeBaseUpperProbes()
        {
            if(_upperProbes.Count>0)
                _upperProbes.Clear();
            
            foreach (ShadowEdgesAndHitsStruct shadowEdge in _shadowEdges)
            {
                _upperProbes.Add(shadowEdge.Source);
            }
        }



        private void SeparateUpperProbes()
        {
            
            if(_upperProbes.Count>0)
                _upperProbes.Clear();
            
            List<Vector3> upperLPs = new List<Vector3>();
            
            
            RaycastHit tempHit;
            
            Vector3 upperDirection = (_lightForwardDirection - _lightMatrix.inverse.MultiplyVector(Vector3.forward * SeparateAlongEdgeDistance));
            Vector3 lowerDirection = (-_lightForwardDirection - _lightMatrix.inverse.MultiplyVector(Vector3.forward *SeparateAlongEdgeDistance));
            
            foreach (ShadowEdgesAndHitsStruct shadowEdge in _shadowEdges)
            {
                if(Physics.Raycast(shadowEdge.Source-SeparateMoveBeforeColl*_lightForwardDirection,upperDirection, out tempHit))
                    
                {
                    upperLPs.Add(tempHit.point - LiftUpperAmongNormal*_lightForwardDirection);
                }
                
                Debug.DrawRay(shadowEdge.Source-SeparateMoveBeforeColl*_lightForwardDirection,upperDirection,Color.white,0.1f,false);

                
                if (Physics.Raycast(shadowEdge.Destination - _lightForwardDirection * 5, lowerDirection,
                    out tempHit))
                {
                    upperLPs.Add(tempHit.point + LiftUpperAmongNormal * _lightForwardDirection);
                }

                Debug.DrawRay(shadowEdge.Destination - _lightForwardDirection * 5, lowerDirection, Color.black,
                    0.1f, false);
            }

            _upperProbes = upperLPs;
        }
            

        
        private void GetShadowSeparatedProbes()
        {

            if(_newProbes.Count<=0)
                MakeBaseLowerProbes();
            
            if(UseShadowPivot)
                InitShadowPivot();
            
            List<Vector3> castedLightProbes = _newProbes;
            
            List<Vector3> AddedProbesList = new List<Vector3>();
            var NewDirectionsList = new List<KeyValuePair<Vector3, Vector3>>();
            
            if (ProbesSeparateOffsetFWD)
            {
                Vector3 offset = new Vector3();
                
                if(UseShadowPivot)
                    offset = ShadowPivot.transform.worldToLocalMatrix.MultiplyVector(Vector3.forward * SeparateShadowSeparateFWD);
                else
                    offset = _lightMatrix.inverse.MultiplyVector(Vector3.forward * SeparateShadowSeparateFWD);

                foreach (ShadowEdgesAndHitsStruct edge in _shadowEdges)
                {
                    AddedProbesList.Add(edge.Destination+offset);
                    //AddedProbesList.Add(edge.Destination-offset);
                    NewDirectionsList.Add(new KeyValuePair<Vector3, Vector3>(edge.Source, (edge.Destination + offset - edge.Source).normalized));
                    //NewDirectionsList.Add(new KeyValuePair<Vector3, Vector3>(edge.Source, (edge.Destination - offset - edge.Source).normalized));
                }
            }

            if (ProbesSeparateScale)
            {
                Matrix4x4 scaleMatrixPos = new Matrix4x4();
                Matrix4x4 scaleMatrixPosDouble = new Matrix4x4();
        
                Vector3 scaleVectorPos = new Vector3(SeparateShadowScaleX,SeparateShadowScaleY,SeparateShadowScaleZ);
                Vector3 scaleVectorPosDouble = new Vector3(2-SeparateShadowScaleX,2-SeparateShadowScaleY,2-SeparateShadowScaleZ);
                
                if(UseShadowPivot)
                {
                    scaleMatrixPos.SetTRS(ShadowPivot.transform.position, ShadowPivot.transform.rotation,
                        scaleVectorPos);
                    scaleMatrixPos = scaleMatrixPos * ShadowPivot.transform.worldToLocalMatrix;
                    if (SeparateScaleDouble)
                    {
                        scaleMatrixPosDouble.SetTRS(ShadowPivot.transform.position, ShadowPivot.transform.rotation,
                            scaleVectorPosDouble);
                        scaleMatrixPosDouble = scaleMatrixPosDouble * ShadowPivot.transform.worldToLocalMatrix;
                    }
                        
                }
                else
                {
                    scaleMatrixPos.SetTRS(CasterObject.transform.position, CasterObject.transform.rotation,
                        scaleVectorPos);
                    scaleMatrixPos = scaleMatrixPos * CasterObject.transform.worldToLocalMatrix;
                    if (SeparateScaleDouble)
                    {
                        scaleMatrixPosDouble.SetTRS(CasterObject.transform.position, CasterObject.transform.rotation,
                            scaleVectorPosDouble);
                        scaleMatrixPosDouble = scaleMatrixPosDouble * CasterObject.transform.worldToLocalMatrix;
                    }
                }

                foreach (ShadowEdgesAndHitsStruct edge in _shadowEdges)
                {
                    AddedProbesList.Add(scaleMatrixPos.MultiplyPoint(edge.Destination));
                    NewDirectionsList.Add(new KeyValuePair<Vector3, Vector3>(edge.Source,
                        (scaleMatrixPos.MultiplyPoint(edge.Destination) - edge.Source).normalized));

                    if (SeparateScaleDouble)
                    {
                        AddedProbesList.Add(scaleMatrixPosDouble.MultiplyPoint(edge.Destination));
                        NewDirectionsList.Add(new KeyValuePair<Vector3, Vector3>(edge.Source,
                            (scaleMatrixPos.MultiplyPoint(edge.Destination) - edge.Source).normalized));
                    }
                }
            }
            
            if (CheckCollidersOnLPs)
                castedLightProbes = GetAdditionalLPHits(NewDirectionsList);
            else
                castedLightProbes = AddedProbesList;
            
            if (LiftAboveGround > 0.1f)
                _newProbes = castedLightProbes.Select(v => v+Vector3.up*LiftAboveGround).ToList();
            else
                _newProbes = castedLightProbes;
        }



        public void AddProbesAlongEdge()
        {
            List<Vector3> AddedAlongEdge = new List<Vector3>();
            float distance = AddUpwardsDistance;
            int numberEvenlyAdjusted = AddUpwardsNumber;
            RaycastHit tempHit;
        
            foreach (Vector3 probe in _newProbes)
            {

                float distanceToColl = distance * numberEvenlyAdjusted;
                
                Debug.DrawRay(probe,-_lightForwardDirection,Color.cyan,0.2f,false);
                if (Physics.Raycast(probe, -_lightForwardDirection, out tempHit, ShadowRaycastDistance))
                {
                    if (AdjustToCasterDistance || PlaceEvenly)
                    distance = tempHit.distance / AddUpwardsDistance;
                       
                    if (PlaceEvenly)
                    {
                        numberEvenlyAdjusted = (int) Math.Round(distance / AddUpwardsDistance);
                        AdjustToCasterDistance = false;
                    }

                    if (StopOnCollider)
                        distanceToColl = tempHit.distance;
                }
                

                if (numberEvenlyAdjusted <= 0) continue;
                
                
                for (int i = 1; i <= numberEvenlyAdjusted; i++)
                {
                    if(distanceToColl/numberEvenlyAdjusted*i > distanceToColl)
                        break;
                    AddedAlongEdge.Add(probe - distance * _lightForwardDirection * i);
                }
            }

            _newProbes.AddRange(AddedAlongEdge);
        }
        
        
        
        private void GetAllVerticesFromAllCasterRenderers()
        {
            _renderersVertices.Clear();
            foreach (MeshRenderer casterRenderer in _casterRenderers)
            {
                if (ReferenceEquals(casterRenderer, null)) continue;

                Vector3[] verts = casterRenderer.GetComponent<MeshFilter>().sharedMesh.vertices;
                foreach (Vector3 vert in verts)
                {
                    _renderersVertices.Add(casterRenderer.transform.TransformPoint(vert));
                }
            }

            if (RemoveDuplicates)
                _renderersVertices = _renderersVertices.Distinct().ToList();
        }

        
        
        private List<Vector3> OptimizeVerticesByDistance(List<Vector3> vertList, float threshold)
        {
            List<Vector3> verticesOptimizedByDistance;
            if (vertList != null)
                verticesOptimizedByDistance = vertList;
            else
            {
                verticesOptimizedByDistance = _renderersVertices;
                threshold = RemoveDistanceThreshold;
            }


            List<int> removeIndexes = new List<int>();
            
            for (int i = 0; i < verticesOptimizedByDistance.Count; i++)
            {
                for (int j = i+1; j < verticesOptimizedByDistance.Count; j++)
                {
                    if (Vector3.Distance(verticesOptimizedByDistance[i], verticesOptimizedByDistance[j]) <
                        threshold)
                        removeIndexes.Add(j);  
                }

                if (removeIndexes.Count <= 0) continue;
                
                removeIndexes = removeIndexes.OrderByDescending(x => x).ToList();
                foreach (int removeIndex in removeIndexes)
                {
                    verticesOptimizedByDistance.RemoveAt(removeIndex);
                }
                removeIndexes.Clear();
            }
            
            if(vertList==null)
                _renderersVertices = verticesOptimizedByDistance;
            
            return verticesOptimizedByDistance;
        }

        
        
        private void MakeShadowEdges()
        {
            if(_shadowEdges.Count>0)
                _shadowEdges.Clear();
            
            if (_renderersVertices.Count > 0)
            {
                if(_shadowRaycastHits.Count>0)
                    _shadowRaycastHits.Clear();
             
                foreach (Vector3 vertex in _renderersVertices)
                {
                    RaycastHit tempHit;
                    if (Physics.Raycast(vertex, _lightForwardDirection, out tempHit, ShadowRaycastDistance))
                    {
                        _shadowEdges.Add(new ShadowEdgesAndHitsStruct(vertex, tempHit.point,tempHit));
                        if (AccurateRecievers)
                            _shadowRaycastHits.Add(tempHit);
                    }
                }

                if (!AccurateRecievers) return;

                MakeCollidersFromMeshes();
                MakeShadowEdgesOnMeshColliders();
            }
        }

        

        private void MakeCollidersFromMeshes()
        {
            if(!_tempMeshCollidersGO)
                _tempMeshCollidersGO = GameObject.Find("_MeshShadowReceivers_");
            
            if(_tempMeshCollidersGO && !MeshCollidersAreDirty)
                return;
            
            if(_tempMeshCollidersGO && MeshCollidersAreDirty)
                DestroyImmediate(_tempMeshCollidersGO.gameObject);

            _tempMeshCollidersGO = new GameObject("_MeshShadowReceivers_")
            {
                hideFlags = HideFlags.DontSaveInEditor, layer = _castLayerMask
            };

            List<MeshRenderer> allHitMeshRenderers = new List<MeshRenderer>();


            foreach (RaycastHit currentHit in _shadowRaycastHits)
            {
                allHitMeshRenderers.AddRange(currentHit.transform.root.transform.GetComponentsInChildren<MeshRenderer>().ToList());
            }


            if (AccurateCasters)
            {
                allHitMeshRenderers.AddRange(GetAllCastingRenderersInGOHierarchy());
            }

            if (allHitMeshRenderers.Count <= 0) return;

            allHitMeshRenderers = allHitMeshRenderers.Distinct().ToList();
            allHitMeshRenderers.RemoveAll(x => x.shadowCastingMode == ShadowCastingMode.Off || !x.gameObject.isStatic);
            
            foreach (MeshRenderer foundMeshRenderer in allHitMeshRenderers)
            {
                Mesh hitMeshFilterMesh = foundMeshRenderer.GetComponent<MeshFilter>().sharedMesh;
                
                if (!hitMeshFilterMesh)
                    continue;
             
                GameObject meshCollGO = new GameObject("_collFromMeshRenderer_");
                
                MeshCollider newMeshCollider = meshCollGO.AddComponent<MeshCollider>();
                    
                newMeshCollider.sharedMesh = new Mesh();
                
                Vector3[] transformedVerts = hitMeshFilterMesh.vertices.Select(vertex => foundMeshRenderer.transform.TransformPoint(vertex)).ToArray();

                newMeshCollider.sharedMesh.vertices = transformedVerts;
                newMeshCollider.sharedMesh.triangles = hitMeshFilterMesh.triangles;
                
                meshCollGO.transform.SetParent(_tempMeshCollidersGO.transform);
                meshCollGO.layer = _castLayerMask;
                
            }

            MeshCollidersAreDirty = false;
        }


        
        private void MakeShadowEdgesOnMeshColliders()
        {
            _shadowEdges.Clear();
            _shadowRaycastHits.Clear();
            
            foreach (Vector3 renderersVertex in _renderersVertices)
            {
                RaycastHit tempHit;
                if (Physics.Raycast(renderersVertex, _lightForwardDirection, out tempHit, ShadowRaycastDistance))
                {
                    _shadowEdges.Add(new ShadowEdgesAndHitsStruct(renderersVertex,tempHit.point,tempHit));
                    _shadowRaycastHits.Add(tempHit);
                }
            }
        }
        
        private List<Vector3> GetAdditionalLPHits(IEnumerable<KeyValuePair<Vector3, Vector3>> additionalEdges)
        {
            List<Vector3> hitsCoords = new List<Vector3>();
            
            foreach (KeyValuePair<Vector3, Vector3> valuePair in additionalEdges)
            {
                RaycastHit tempHit;  
                if(Physics.Raycast(valuePair.Key,valuePair.Value, out tempHit, ShadowRaycastDistance))
                //if (Physics.Raycast(valuePair.Key, valuePair.Value, out tempHit, ShadowRaycastDistance, _castLayerMask))
                {
                    hitsCoords.Add(tempHit.point);
                    _shadowRaycastHits.Add(tempHit);
                }
                
                Debug.DrawRay(valuePair.Key,valuePair.Value,Color.green,0.1f,false);
            }

            return hitsCoords;
        }
        
        private void RemoveBackCastedEdges()
        {
            RaycastHit tempHit;
            Vector3 edgeVectAdd = new Vector3();
            List<ShadowEdgesAndHitsStruct> backCastedShadowEdges = new List<ShadowEdgesAndHitsStruct>();
            
            foreach (var edgesAndHitsStruct in _shadowEdges)
            {
                edgeVectAdd = _lightForwardDirection*BackCastDistanceOffset;
                
                if (Physics.Raycast(edgesAndHitsStruct.Destination + edgeVectAdd, _lightMatrix.MultiplyVector(Vector3.back),
                    out tempHit, ShadowRaycastDistance+edgeVectAdd.magnitude))
                {
                    _shadowEdgesBackCastDebug.Add(new KeyValuePair<Vector3, Vector3>(edgesAndHitsStruct.Destination+edgeVectAdd, tempHit.point));
                    if(Vector3.Distance(edgesAndHitsStruct.Source,tempHit.point)<BackCastRemoveDistance)
                        backCastedShadowEdges.Add(new ShadowEdgesAndHitsStruct(edgesAndHitsStruct.Source,edgesAndHitsStruct.Destination, tempHit));
                }
              
            }

            _shadowEdges = backCastedShadowEdges;
        }
        
        


        private List<MeshRenderer> GetAllCastingRenderersInGOHierarchy()
        {
            List<MeshRenderer> renderers = CasterObject.GetComponentsInChildren<MeshRenderer>().ToList();
            renderers.RemoveAll(x => x.shadowCastingMode == ShadowCastingMode.Off || !x.gameObject.isStatic);

            return renderers;
        }
        
        
        private List<Vector3> TransformVerticesToLightProjection()
        {
            if (_renderersVertices.Count <= 0) return _renderersVertices;
			
            List<Vector3> transformedVertices = new List<Vector3>();

            foreach (Vector3 vertex in _renderersVertices)
            {
                transformedVertices.Add(_lightMatrix.MultiplyPoint(vertex));
            }

            return transformedVertices;
        }

        
        private void OnDrawGizmos()
        {
            
            //Vertex gizmos
            if (_renderersVertices.Count > 0 && DisplayVertexGizmos)
            {
                Gizmos.color = Color.cyan;
                foreach (Vector3 vertex in _renderersVertices)
                {
                    Gizmos.DrawSphere(vertex, VertexGizmoSize);
                }
            }

            //Shadow gizmos
            if (_shadowEdges.Count > 0 && DisplayShadowGizmos)
            {
                foreach (var hitsStruct in _shadowEdges)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(hitsStruct.Source, ShadowGizmoSize);
                    Gizmos.DrawLine(hitsStruct.Source,hitsStruct.Destination);
                    Gizmos.color = Color.black;
                    Gizmos.DrawSphere(hitsStruct.Destination, ShadowGizmoSize);
                }
            }

            //BackCast debug gizmos
            if (_shadowEdgesBackCastDebug.Count > 0 && DisplayBackCastDebug && RemoveBackCast)
            {
                foreach (var keyValuePair in _shadowEdgesBackCastDebug)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(keyValuePair.Key, BackCastDebugGizmoSize);
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(keyValuePair.Value, BackCastDebugGizmoSize);
                    Gizmos.DrawLine(keyValuePair.Key,keyValuePair.Value);
                }
            }
            
            //Light Probes
            if (_newProbes.Count > 0 && LowerLightProbes)
            {
                foreach (Vector3 newProbe in _newProbes)
                {
                    
                    Gizmos.color = Color.white;
                    if(OptimizeLightProbes)
                        Gizmos.color = Color.red;
                    Gizmos.DrawSphere(newProbe, LightProbesGizmoSize);
                    
                }
            }
            
            if(_upperProbes.Count>0 && UpperLightProbes)
                foreach (Vector3 upperProbe in _upperProbes)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(upperProbe, UpperLightProbesGizmoSize);
                    
                }    
        }

        private void OnDestroy()
        {
            SwitchCasterColliders(true);
            SwitchAllColliders(true);
            DestroyImmediate(ShadowPivot);
        }
    }
}

#endif