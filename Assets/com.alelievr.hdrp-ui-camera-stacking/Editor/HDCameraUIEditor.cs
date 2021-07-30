using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEditor.Rendering;

[CustomEditor(typeof(HDCameraUI))]
[CanEditMultipleObjects]
public class HDCameraUIEditor : Editor
{
    SerializedProperty uiLayerMask;
    SerializedProperty priority;
    SerializedProperty customPostEffect;
    SerializedProperty oneTimeCulling;
    SerializedProperty graphicsFormat;
    HDCameraUI cameraUI;

    Editor materialEditor;

    [UnityEditor.MenuItem("GameObject/UI/Camera (HDRP)", false)]
    static void AddUICamera(MenuCommand menuCommand)
    {
        var go = CoreEditorUtils.CreateGameObject("UI Camera", menuCommand.context);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
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
        customPostEffect = serializedObject.FindProperty(nameof(cameraUI.customPostEffect));
        oneTimeCulling = serializedObject.FindProperty(nameof(cameraUI.oneTimeCulling));
        graphicsFormat = serializedObject.FindProperty(nameof(cameraUI.graphicsFormat));
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

        // TODO: implement custom post effects
        // EditorGUILayout.PropertyField(customPostEffect);

        cameraUI.showAdvancedSettings = EditorGUILayout.Foldout(cameraUI.showAdvancedSettings, "Show Advanced Options");

        if (cameraUI.showAdvancedSettings)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(oneTimeCulling);
                EditorGUILayout.PropertyField(graphicsFormat);
            }
        }

        serializedObject.ApplyModifiedProperties();

        // Show material UI if not null
        if (cameraUI.customPostEffect != null)
        {
            if (materialEditor == null)
                Editor.CreateEditor(cameraUI.customPostEffect);

            if (materialEditor.target != cameraUI.customPostEffect)
                materialEditor.target = cameraUI.customPostEffect;
        }

        // TODO: try to register changes in material property as a property block in the hdcameraUI to make it work with the animation system
    }
}
