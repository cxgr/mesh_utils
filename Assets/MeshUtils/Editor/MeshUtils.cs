using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace MeshUtils
{
    class MeshUtilsWindow : EditorWindow
    {
        GameObject objWithMesh;

        bool isPivotEditModeActive;
        Transform pivotAuxTransform;
        bool weightedAverage;

        readonly string pivotHandleName = "__meshutils_pivot_handle_";

        [MenuItem("Window/MeshUtils")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(MeshUtilsWindow));
        }

        #region GUI

        void OnGUI()
        {
            GUI.color = Color.red;
            if (GUILayout.Button("RESET", GUILayout.Width(60)))
                Reset();
            GUI.color = Color.white;

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("OBJ: ", (null != objWithMesh ? objWithMesh.name : "no mesh filter"));

            EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);

            if (null != objWithMesh)
            {
                if (!isPivotEditModeActive)
                {
                    GUI.color = (Color.red + Color.yellow) / 2f;
                    if (GUILayout.Button("EDIT"))
                        Edit_Begin();
                }
                else
                {
                    EditorGUILayout.LabelField("PIVOT:");
                    EditorGUILayout.Separator();

                    EditorGUILayout.BeginHorizontal();
                    {
                        weightedAverage = GUILayout.Toggle(weightedAverage, "weighted avg");

                        if (GUILayout.Button("C HOR"))
                            Edit_MovePivotCenterHor();

                        if (GUILayout.Button("C VER"))
                            Edit_MovePivotCenterVer();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Separator();

                    EditorGUILayout.BeginVertical();
                    {
                        GUI.color = Color.cyan;
                        if (GUILayout.Button("TOP", GUILayout.Width(100f)))
                            Edit_MovePivotTop();
                        GUI.color = Color.yellow;
                        if (GUILayout.Button("BTM", GUILayout.Width(100f)))
                            Edit_MovePivotBottom();
                        GUI.color = Color.white;
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);

                    EditorGUILayout.BeginHorizontal();
                    {
                        GUI.color = Color.green;
                        if (GUILayout.Button("SAVE AS"))
                            Edit_SaveAsAsset();
                        GUI.color = (Color.red + Color.yellow) / 2f;
                        if (GUILayout.Button("OVERWRITE"))
                            Edit_SaveOverwrite();
                        GUI.color = Color.red;
                        if (GUILayout.Button("ABORT"))
                            Edit_Abort();
                        GUI.color = Color.white;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        #endregion

        void OnSelectionChange()
        {
            var selGO = Selection.activeGameObject;

            if (!isPivotEditModeActive)
            {
                if (null == selGO || null == selGO.GetComponent<MeshFilter>() || null == selGO.GetComponent<MeshFilter>().sharedMesh)
                    objWithMesh = null;
                else
                    objWithMesh = selGO;
            }

            Repaint();
        }

        void Update()
        {
            if (isPivotEditModeActive)
            {
                if (null != pivotAuxTransform)
                {
                    if (Selection.activeGameObject != pivotAuxTransform.gameObject)
                    {
                        Selection.activeGameObject = pivotAuxTransform.gameObject;
                        EditorWindow.GetWindow<SceneView>().ShowNotification(new GUIContent("pivot editing in progress"));
                    }
                }
                else
                    Selection.activeGameObject = CreatePivotObj();
            }
        }

        void Reset()
        {
            isPivotEditModeActive = false;
            objWithMesh = null;
            if (null != pivotAuxTransform)
                DestroyImmediate(pivotAuxTransform.gameObject);
            Selection.activeGameObject = null;

            GameObject leakT;
            while (null != (leakT = GameObject.Find(pivotHandleName)))
                DestroyImmediate(leakT);

            System.GC.Collect();

            Repaint();
        }

        #region PIVOT

        void Edit_Begin()
        {
            isPivotEditModeActive = true;

            if (null != pivotAuxTransform)
                DestroyImmediate(pivotAuxTransform.gameObject);

            Selection.activeGameObject = CreatePivotObj();
        }

        GameObject CreatePivotObj()
        {
            var pivotGO = new GameObject(pivotHandleName);
            pivotGO.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
            pivotAuxTransform = pivotGO.transform;
            pivotAuxTransform.parent = objWithMesh.transform;
            pivotAuxTransform.localPosition = Vector3.zero;
            pivotAuxTransform.localRotation = Quaternion.identity;
            return pivotGO;
        }

        void Edit_MovePivotCenterHor()
        {
            var verts = objWithMesh.GetComponent<MeshFilter>().sharedMesh.vertices;

            var pivotPos = pivotAuxTransform.position;

            if (weightedAverage)
            {
                var sum = Vector3.zero;
                for (int i = 0; i < verts.Length; ++i)
                    sum += objWithMesh.transform.TransformPoint(verts[i]);
                sum /= verts.Length;

                pivotPos.x = sum.x;
                pivotPos.z = sum.z;
            }
            else
            {
                var xMinVert = verts.OrderBy((v) => objWithMesh.transform.TransformPoint(v).x).FirstOrDefault();
                var xMaxVert = verts.OrderByDescending((v) => objWithMesh.transform.TransformPoint(v).x).FirstOrDefault();

                var zMinVert = verts.OrderBy((v) => objWithMesh.transform.TransformPoint(v).z).FirstOrDefault();
                var zMaxVert = verts.OrderByDescending((v) => objWithMesh.transform.TransformPoint(v).z).FirstOrDefault();

                pivotPos.x = objWithMesh.transform.TransformPoint((xMinVert + xMaxVert) / 2f).x;
                pivotPos.z = objWithMesh.transform.TransformPoint((zMinVert + zMaxVert) / 2f).z;
            }

            pivotAuxTransform.position = pivotPos;
        }

        void Edit_MovePivotCenterVer()
        {
            var verts = objWithMesh.GetComponent<MeshFilter>().sharedMesh.vertices;

            var pivotPos = pivotAuxTransform.position;

            if (weightedAverage)
            {
                var sum = Vector3.zero;
                for (int i = 0; i < verts.Length; ++i)
                    sum += objWithMesh.transform.TransformPoint(verts[i]);
                sum /= verts.Length;

                pivotPos.y = sum.y;
            }
            else
            {
                var yMinVert = verts.OrderBy((v) => objWithMesh.transform.TransformPoint(v).y).FirstOrDefault();
                var yMaxVert = verts.OrderByDescending((v) => objWithMesh.transform.TransformPoint(v).y).FirstOrDefault();

                pivotPos.y = objWithMesh.transform.TransformPoint((yMinVert + yMaxVert) / 2f).y;
            }

            pivotAuxTransform.position = pivotPos;
        }

        void Edit_MovePivotTop()
        {
            var verts = objWithMesh.GetComponent<MeshFilter>().sharedMesh.vertices;
            var pivotPos = pivotAuxTransform.position;
            var yMaxVert = verts.OrderByDescending((v) => objWithMesh.transform.TransformPoint(v).y).FirstOrDefault();
            pivotPos.y = objWithMesh.transform.TransformPoint(yMaxVert).y;
            pivotAuxTransform.position = pivotPos;
        }

        void Edit_MovePivotBottom()
        {
            var verts = objWithMesh.GetComponent<MeshFilter>().sharedMesh.vertices;
            var pivotPos = pivotAuxTransform.position;
            var yMinVert = verts.OrderBy((v) => objWithMesh.transform.TransformPoint(v).y).FirstOrDefault();
            pivotPos.y = objWithMesh.transform.TransformPoint(yMinVert).y;
            pivotAuxTransform.position = pivotPos;
        }

        void Edit_SaveAsAsset()
        {
            var mf = objWithMesh.GetComponent<MeshFilter>();
            if (null != mf)
            {
                var newMesh = Instantiate<Mesh>(mf.sharedMesh);

                var oldVerts = newMesh.vertices;
                var vertCount = oldVerts.Length;
                var newVerts = new Vector3[vertCount];

                for (int i = 0; i < vertCount; ++i)
                    newVerts[i] = oldVerts[i] - pivotAuxTransform.localPosition;

                newMesh.SetVertices(newVerts.ToList());

                var path = EditorUtility.SaveFilePanelInProject("save modified mesh as",
                                newMesh.name + "_pivot_fix",
                                "asset",
                                "Please enter a file name to save the texture to");

                if (path.Length > 0)
                {
                    AssetDatabase.CreateAsset(newMesh, path);
                    AssetDatabase.SaveAssets();

                    mf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

                    EditorUtility.SetDirty(objWithMesh.gameObject);
                    Reset();
                }
                else
                    DestroyImmediate(newMesh);
            }
        }

        void Edit_SaveOverwrite()
        {
            var mf = objWithMesh.GetComponent<MeshFilter>();
            if (null != mf)
            {
                var newMesh = mf.sharedMesh;

                var oldVerts = newMesh.vertices;
                var vertCount = oldVerts.Length;
                var newVerts = new Vector3[vertCount];

                for (int i = 0; i < vertCount; ++i)
                    newVerts[i] = oldVerts[i] - pivotAuxTransform.localPosition;

                newMesh.SetVertices(newVerts.ToList());
                mf.sharedMesh = newMesh;

                EditorUtility.SetDirty(objWithMesh.gameObject);
                Reset();
            }
        }

        void Edit_Abort()
        {
            Reset();
        }

        #endregion
    }
}
