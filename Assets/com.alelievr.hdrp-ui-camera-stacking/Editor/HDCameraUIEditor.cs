using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;

[CustomEditor(typeof(HDCameraUI))]
[CanEditMultipleObjects]
public class HDCameraUIEditor : Editor
{
    SerializedProperty uiLayerMask;
    SerializedProperty priority;
    SerializedProperty compositingMaterial;
    SerializedProperty graphicsFormat;
    SerializedProperty targetCamera;
    SerializedProperty targetCameraLayer;
    SerializedProperty targetCameraObject;
    SerializedProperty compositingMode;
    SerializedProperty compositingMaterialPass;
    SerializedProperty renderInCameraBuffer;
    HDCameraUI cameraUI;

    Editor materialEditor;

    [UnityEditor.MenuItem("GameObject/UI/Camera (HDRP)", false)]
    static void AddUICamera(MenuCommand menuCommand)
    {
        var go = CoreEditorUtils.CreateGameObject("UI Camera", menuCommand.context);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.AddComponent<HDAdditionalCameraData>();
        go.AddComponent<HDCameraUI>();
        go.layer = LayerMask.NameToLayer("UI"); // default UI layer

        var camera = go.GetComponent<Camera>();
        camera.cullingMask = 1 << go.layer;

        var canvasGO = new GameObject("Canvas");
        canvasGO.transform.SetParent(go.transform);
        canvasGO.transform.position = Vector3.zero;
        canvasGO.layer = go.layer;

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;

        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.AddComponent<CanvasScaler>();
    }

    void OnEnable()
    {
        cameraUI = target as HDCameraUI;
        uiLayerMask = serializedObject.FindProperty(nameof(cameraUI.uiLayerMask));
        priority = serializedObject.FindProperty(nameof(cameraUI.priority));
        compositingMaterial = serializedObject.FindProperty(nameof(cameraUI.compositingMaterial));
        graphicsFormat = serializedObject.FindProperty(nameof(cameraUI.graphicsFormat));
        compositingMode = serializedObject.FindProperty(nameof(cameraUI.compositingMode));
        compositingMaterialPass = serializedObject.FindProperty(nameof(cameraUI.compositingMaterialPass));
        renderInCameraBuffer = serializedObject.FindProperty(nameof(cameraUI.renderInCameraBuffer));
        targetCamera = serializedObject.FindProperty(nameof(cameraUI.targetCamera));
        targetCameraLayer = serializedObject.FindProperty(nameof(cameraUI.targetCameraLayer));
        targetCameraObject = serializedObject.FindProperty(nameof(cameraUI.targetCameraObject));
    }

    void OnDisable()
    {
        if (materialEditor != null)
        {
            Object.DestroyImmediate(materialEditor);
            materialEditor = null;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(uiLayerMask);
        EditorGUILayout.PropertyField(priority);

        // Show Mode
        EditorGUILayout.PropertyField(compositingMode);

        var mode = (HDCameraUI.CompositingMode)compositingMode.intValue;

        if (mode == HDCameraUI.CompositingMode.Manual)
            EditorGUILayout.HelpBox("Manual mode disables the compositing. To manually perform the compositing you can use either the camera target texture or the HDCameraUI.renderTexture field.", MessageType.Info, true);

        if (mode == HDCameraUI.CompositingMode.Custom)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(compositingMaterial);

                if (compositingMaterial.objectReferenceValue == null)
                    EditorGUILayout.HelpBox("Compositing Material is null. No compositing will happen.", MessageType.Error, true);
                else
                {
                    if (compositingMaterial.objectReferenceValue is Material mat)
                    {
                        if (mat.passCount > 1)
                        {
                            var compositingMaterialPassRect = EditorGUILayout.GetControlRect(true);
                            EditorGUI.BeginProperty(compositingMaterialPassRect, GUIContent.none, compositingMaterialPass);
                            var passNames = Enumerable.Range(0, mat.passCount).Select(i => mat.GetPassName(i)).ToArray();
                            compositingMaterialPass.intValue = EditorGUI.Popup(compositingMaterialPassRect, compositingMaterialPass.intValue, passNames);
                            EditorGUI.EndProperty();
                        }
                        else
                            compositingMaterialPass.intValue = 0;
                    }
                }
            }
        }

        // Target Camera
        EditorGUILayout.PropertyField(targetCamera);
        var targetCameraMode = (HDCameraUI.TargetCamera)targetCamera.intValue;
        using (new EditorGUI.IndentLevelScope())
        {
            if (targetCameraMode == HDCameraUI.TargetCamera.Layer)
                EditorGUILayout.PropertyField(targetCameraLayer);
            if (targetCameraMode == HDCameraUI.TargetCamera.Specific)
                EditorGUILayout.PropertyField(targetCameraObject);
        }

        // Advanced settings
        cameraUI.showAdvancedSettings = EditorGUILayout.Foldout(cameraUI.showAdvancedSettings, "Advanced Settings");

        if (cameraUI.showAdvancedSettings)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(graphicsFormat);
                if (cameraUI.attachedCamera.targetTexture == null)
                    EditorGUILayout.PropertyField(renderInCameraBuffer);
            }
        }

        serializedObject.ApplyModifiedProperties();

        // Show material UI if not null
        if (cameraUI.compositingMaterial != null)
        {
            if (materialEditor == null)
                materialEditor = CreateEditor(cameraUI.compositingMaterial, typeof(MaterialEditor));

            if (materialEditor.target != cameraUI.compositingMaterial)
                materialEditor.target = cameraUI.compositingMaterial;

            EditorGUILayout.Space();
            materialEditor.DrawHeader();
            materialEditor.OnInspectorGUI();
        }

        // TODO: try to register changes in material property as a property block in the hdcameraUI to make it work with the animation system
    }
}
