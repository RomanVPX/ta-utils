using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TechArtUtils
{
#if (UNITY_EDITOR)

	internal class LightProbesUtils : EditorWindow
	{

		public GameObject GroupToOperateOnGo;
		public GameObject GroupToAddGo;
		public GameObject MergeBoundingBox;
		
		private LightProbeGroup _groupToOperateOn;
		private LightProbeGroup _groupToAdd;

		private float _mergeByDistanceDistance;
		
		private Vector3[] _tempArray;
		private bool _dontAddCloserThanSwitch;
		private float _dontAddCloserThanDistance;
		private bool _deleteCloseProbesOnAdded;

		private bool _showAllProbesInGroup;
		private bool _showProbesMergingInsideBb;
		private bool _showMergingClustering;

		private List<Vector3> _probesToDistanceMerge;
		private List<Vector3> _probesAfterDistanceMerge;
		private List<Vector3> _mergedOnlyProbesAfterMerge;
		private List<List<Vector3>> _sortedProbesClusters;
		
		
		private static readonly Color PaleGreyColor = new Color(0.4f,0.4f,0.4f,0.5f);
		private static readonly Color PaleGreenColor = new Color(0f,0.6f,0f,0.2f);
		private static readonly Color PaleRedColor = new Color(0.8f,0f,0f,0.8f);
		private static readonly Color PaleBlueColor = new Color(0f,0f,0.6f,0.8f);

		
		private bool _forceSceneGuiRefresh = true;

		[MenuItem("TA Tools/Light probes utils")]
		public static void ShowLpUtilsWindow()
		{
			GetWindow<LightProbesUtils>();
		}


		
		private void OnValidate()
		{
			if (SceneView.onSceneGUIDelegate == this.OnSceneGUI)
			{
				if (SceneView.onSceneGUIDelegate != null)
				{
					SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
				}
			}
			else
			{
				SceneView.onSceneGUIDelegate += this.OnSceneGUI;
			}
		}

		
		
		
		//-------- Handles and gizmos -----------//
		
		void OnSceneGUI(SceneView sceneView) {
			
			if(_groupToOperateOn==null)
				return;
			
			if (_showAllProbesInGroup && _groupToOperateOn.probePositions.Any())
			{
				Handles.color = PaleGreyColor;
				
				foreach (Vector3 probePosition in _groupToOperateOn.probePositions)
				{
					Handles.DrawSolidDisc(_groupToOperateOn.transform.TransformPoint(probePosition),-sceneView.camera.transform.forward, 2f);
				}
			}

			
			if (_showProbesMergingInsideBb)
			{
				if (_probesToDistanceMerge != null)
				{
					if(_forceSceneGuiRefresh && MergeBoundingBox.transform.hasChanged)
						_probesToDistanceMerge = GetProbesInsideCollider(_groupToOperateOn, MergeBoundingBox).ToList();
					
					Handles.color = PaleGreenColor;
					foreach (Vector3 gizmo in _probesToDistanceMerge)
					{
						Handles.DrawSolidDisc(_groupToOperateOn.transform.TransformPoint(gizmo), -sceneView.camera.transform.forward, 3f);
					}
				}

				if (_probesAfterDistanceMerge != null)
				{
					if(_forceSceneGuiRefresh && MergeBoundingBox.transform.hasChanged)
						_probesAfterDistanceMerge = GetDistanceMergedProbes(_probesToDistanceMerge, _mergeByDistanceDistance);
					
					_mergedOnlyProbesAfterMerge.Clear();
					if (_probesToDistanceMerge != null)
						_mergedOnlyProbesAfterMerge = _probesToDistanceMerge.Intersect(_probesAfterDistanceMerge).ToList();
					
					Handles.color = PaleBlueColor;
					foreach (Vector3 mergedProbe in _mergedOnlyProbesAfterMerge)
					{
						Handles.DrawSolidDisc(_groupToOperateOn.transform.TransformPoint(mergedProbe), -sceneView.camera.transform.forward, 1.5f);
					}

					_mergedOnlyProbesAfterMerge = _probesAfterDistanceMerge.Except(_mergedOnlyProbesAfterMerge).ToList();
					
					Handles.color = PaleRedColor;
					foreach (Vector3 mergedProbe in _mergedOnlyProbesAfterMerge)
					{
						Handles.DrawSolidDisc(_groupToOperateOn.transform.TransformPoint(mergedProbe), -sceneView.camera.transform.forward, 1.5f);
					}

					if (_showMergingClustering && _sortedProbesClusters!=null)
					{
						//int maxProbesCount = _sortedProbesClusters[0].Count;
						
						foreach (List<Vector3> probesCluster in _sortedProbesClusters)
						{
							//Handles.color = Handles.color / maxProbesCount * probesCluster.Count;
							int i = 1;
							while (i<probesCluster.Count)
							{
								Handles.DrawLine(_groupToOperateOn.transform.TransformPoint(probesCluster[0]),_groupToOperateOn.transform.TransformPoint(probesCluster[i]));
								i++;
							}
						}
						
					}
				}

				if (MergeBoundingBox)
					MergeBoundingBox.transform.hasChanged = false;
				
			}

			Handles.BeginGUI();
			// Do your drawing here using GUI.
			Handles.EndGUI();    
		}
		
		
		
		private void OnGUI()
		{			
			
			GameObject lpgToAddToGoCheck =
				EditorGUILayout.ObjectField("Light Probe Group to modify:", GroupToOperateOnGo, typeof(GameObject), true, GUILayout.MinWidth(200)) as
					GameObject;
			
			if (lpgToAddToGoCheck != null && lpgToAddToGoCheck.GetComponent<LightProbeGroup>())
			{
				GroupToOperateOnGo = lpgToAddToGoCheck;
				_groupToOperateOn = lpgToAddToGoCheck.GetComponent<LightProbeGroup>();
				_showAllProbesInGroup = EditorGUILayout.Toggle("Show all probes", _showAllProbesInGroup, GUILayout.MinWidth(400));
			}
			else
			{
				GroupToAddGo = null;
				_groupToOperateOn = null;
				return;
			}
			

			//-------- LP Merge Inside Bounding Box -----------//
			
			GUILayout.Box("",GUILayout.ExpandWidth(true), GUILayout.Height(1)); //разделитель
			GUILayout.Label("Merge Light Probes", EditorStyles.boldLabel);
			EditorGUILayout.Space();
			

			GameObject mergeBbToCheck = EditorGUILayout.ObjectField("Bounding box:", MergeBoundingBox, typeof(GameObject), true, GUILayout.MinWidth(200)) as GameObject;
			if (mergeBbToCheck != null && mergeBbToCheck.GetComponent<BoxCollider>())
			{
				MergeBoundingBox = mergeBbToCheck;
				
				EditorGUI.BeginChangeCheck();
				
				_mergeByDistanceDistance = EditorGUILayout.Slider("Distance", _mergeByDistanceDistance, 0.1f, 50f);
				
				_showProbesMergingInsideBb = EditorGUILayout.Toggle("Show gizmos", _showProbesMergingInsideBb,GUILayout.MinWidth(400));
				_showMergingClustering = EditorGUILayout.Toggle("Show clustering", _showMergingClustering,GUILayout.MinWidth(400));

				if (EditorGUI.EndChangeCheck())
				{
					_probesToDistanceMerge = GetProbesInsideCollider(_groupToOperateOn, MergeBoundingBox).ToList();
					_probesAfterDistanceMerge = GetDistanceMergedProbes(_probesToDistanceMerge, _mergeByDistanceDistance);

					_forceSceneGuiRefresh = _showProbesMergingInsideBb;
				}

				if (GUILayout.Button("Merge"))
				{
					Undo.RegisterCompleteObjectUndo(GroupToOperateOnGo, "Merge LPs by distance");
					MergeLpInsideBb();
				}
			}

			
			//-------- LP Merge Light Probe Groups -----------//
			
			EditorGUILayout.Space();
			GUILayout.Box("",GUILayout.ExpandWidth(true), GUILayout.Height(1)); //разделитель
			GUILayout.Label("Light Probe Groups Combine", EditorStyles.boldLabel);
			EditorGUILayout.Space();
			
			GameObject lpgToAddGoCheck =
				EditorGUILayout.ObjectField("Group to add:", GroupToAddGo, typeof(GameObject), true, GUILayout.MinWidth(200)) as
					GameObject;

			if (lpgToAddGoCheck != null && lpgToAddGoCheck.GetComponent<LightProbeGroup>())
			{
				GroupToAddGo = lpgToAddGoCheck;
				_groupToAdd = lpgToAddGoCheck.GetComponent<LightProbeGroup>();
			}

			if(!_groupToOperateOn || !_groupToAdd || _groupToOperateOn.probePositions.Length<0 || _groupToAdd.probePositions.Length <=0)
				return;

			_dontAddCloserThanSwitch = EditorGUILayout.Toggle("Don't add probes closer than", _dontAddCloserThanSwitch, GUILayout.MinWidth(600));

			if (_dontAddCloserThanSwitch)
			{
				_dontAddCloserThanDistance = EditorGUILayout.Slider("Distance", _dontAddCloserThanDistance, 0.1f, 50f);
				_deleteCloseProbesOnAdded = EditorGUILayout.Toggle("Delete close probes among added", _deleteCloseProbesOnAdded, GUILayout.MinWidth(600));
			}

			if (GUILayout.Button("Combine"))
			{
				Undo.RegisterCompleteObjectUndo(GroupToOperateOnGo, "Combine LP Groups");
				if (_dontAddCloserThanSwitch)
					CombineLightProbeGroupsUsingDistance();
				else
					CombineLightProbeGroups();
			}
		}

		//---------- END OF GUI -------------//



		private List<Vector3> GetDistanceMergedProbes(List<Vector3> probesToDistanceMerge, float mergeDistance)
		{

			List<Vector3> probesAfterMerge = new List<Vector3>();
			List<List<Vector3>> probesClusters = new List<List<Vector3>>();

			foreach (Vector3 probeToClusterAround in probesToDistanceMerge)
			{
				List<Vector3> localCluster = new List<Vector3>();
				localCluster.Add(probeToClusterAround);
				
				foreach (Vector3 probeToCheckIfClusters in probesToDistanceMerge)
				{
					if(Vector3.Distance(probeToClusterAround,probeToCheckIfClusters)<=_mergeByDistanceDistance && !probeToCheckIfClusters.AlmostEquals(probeToClusterAround, 0.1f))
						localCluster.Add(probeToCheckIfClusters);
				}
				probesClusters.Add(localCluster);
			}

			List<Vector3>[] sortedProbesClusters = probesClusters.OrderByDescending(clusters => clusters.Count()).ToArray();

			if (_showMergingClustering)
			{
				_sortedProbesClusters = new List<List<Vector3>>();
				_sortedProbesClusters = sortedProbesClusters.ToList();
			}
				
			
			for (int i = 0; i < sortedProbesClusters.Length; i++)
			{
				if (sortedProbesClusters[i].Count <= 0) continue;

				foreach (Vector3 vectorToCompare in sortedProbesClusters[i])
				{
					int j = i + 1;
					while (j < sortedProbesClusters.Length)
					{
						if (sortedProbesClusters[j].Count <= 0)
						{
						}
						else
						{
							for (int k = 0; k < sortedProbesClusters[j].Count; k++)
							{
								if (sortedProbesClusters[j][k] != vectorToCompare)
									continue;
								if (k==0)
									sortedProbesClusters[j].Clear();
								else
									sortedProbesClusters[j].RemoveAt(k);
							}
						}
						j++;
					}
				}
			}

			probesClusters = sortedProbesClusters.ToList();
			probesClusters.RemoveAll(cluster => cluster.Count <= 0);
			
			foreach (List<Vector3> probesCluster in probesClusters)
			{
				probesAfterMerge.Add(probesCluster.Aggregate(new Vector3(0,0,0), (s,v) => s+v)/probesCluster.Count);
			}
			
			return probesAfterMerge;
		}
		
		
		
		private void MergeLpInsideBb()
		{
			List<Vector3> groupProbes = _groupToOperateOn.probePositions.ToList();
			List<Vector3> probesToMerge = GetProbesInsideCollider(_groupToOperateOn, MergeBoundingBox).ToList();

			groupProbes = groupProbes.Except(probesToMerge).ToList();
			groupProbes.AddRange(_probesAfterDistanceMerge);
			_groupToOperateOn.probePositions = groupProbes.ToArray();

		}
		
		
		private IEnumerable<Vector3> GetProbesInsideCollider(LightProbeGroup LPG, GameObject CollGo)
		{
			if (CollGo == null)
				return null;
			BoxCollider colliderToCheck = CollGo.GetComponent<BoxCollider>();
			IEnumerable<Vector3> containedProbes = LPG.probePositions.Where(probePosition => PointInOABB(LPG.transform.TransformPoint(probePosition), colliderToCheck));
			return containedProbes;
		}

		
		bool PointInOABB (Vector3 point, BoxCollider box )
		{
			point = box.transform.InverseTransformPoint( point ) - box.center;
         
			float halfX = (box.size.x * 0.5f);
			float halfY = (box.size.y * 0.5f);
			float halfZ = (box.size.z * 0.5f);
			return point.x < halfX && point.x > -halfX && 
			       point.y < halfY && point.y > -halfY && 
			       point.z < halfZ && point.z > -halfZ;
		}

		
		
		
		//-------- LP Merge Light Probe Groups Methods -----------//
		
		
		private void CombineLightProbeGroupsUsingDistance()
		{
			List<Vector3> tempList = _groupToOperateOn.probePositions.Select(probePosition => _groupToOperateOn.transform.localToWorldMatrix.MultiplyPoint3x4(probePosition)).ToList();

			if (_deleteCloseProbesOnAdded)
			{
				foreach (Vector3 vToAdd in _groupToAdd.probePositions)
				{
					bool distanceIsOkToAdd = true;
					Vector3 currentVectorToAdd = _groupToAdd.transform.localToWorldMatrix.MultiplyPoint3x4(vToAdd);

					foreach (Vector3 t in tempList)
					{
						if (Vector3.Distance(currentVectorToAdd, t) <= _dontAddCloserThanDistance)
						{
							distanceIsOkToAdd = false;
							break;
						}
					}

					if (distanceIsOkToAdd)
						tempList.Add(currentVectorToAdd);
				}
			}

			else
			{
				foreach (Vector3 vToAdd in _groupToAdd.probePositions)
				{
					bool distanceIsOkToAdd = true;
					Vector3 currentVectorToAdd = _groupToAdd.transform.localToWorldMatrix.MultiplyPoint3x4(vToAdd);

					foreach (Vector3 t in _groupToOperateOn.probePositions)
					{
						if (Vector3.Distance(currentVectorToAdd, _groupToOperateOn.transform.localToWorldMatrix.MultiplyPoint3x4(t)) <= _dontAddCloserThanDistance)
						{
							distanceIsOkToAdd = false;
							break;
						}
					}

					if (distanceIsOkToAdd)
						tempList.Add(currentVectorToAdd);
				}
			}

			_groupToOperateOn.probePositions = tempList.Select(probePos => _groupToOperateOn.transform.worldToLocalMatrix.MultiplyPoint3x4(probePos)).ToArray();
		}

		
		
		private void CombineLightProbeGroups()
		{
			_tempArray = new Vector3[_groupToAdd.probePositions.Length+_groupToOperateOn.probePositions.Length];

			for (int i = 0; i < _tempArray.Length; i++)
			{
				if(_groupToOperateOn.probePositions.Length > i)
				{
					_tempArray[i] = _groupToOperateOn.probePositions[i];
				}
				else
				{
					_tempArray[i] = _groupToAdd.transform.localToWorldMatrix.MultiplyPoint3x4(_groupToAdd.probePositions[i-_groupToOperateOn.probePositions.Length]);
				}
			}

			_groupToOperateOn.probePositions = _tempArray;
		}
	}
	
#endif
}

