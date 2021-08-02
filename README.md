# HDRP UI Camera Stacking

The HDRP UI Camera Stacking package allows you to stack multiple camera rendering UI only at a fraction of the cost of a standard camera.

This is achieved by taking advantage of the [customRender](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@12.0/api/UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.html#UnityEngine_Rendering_HighDefinition_HDAdditionalCameraData_customRender) feature to render only the GUI elements and nothing else, with the only downside to not be able to render Lit objects which are generally found for UI.

https://user-images.githubusercontent.com/6877923/127684238-1f149a4f-1677-4428-b3f8-7ba51c6c93d6.mp4


This implementation also provides all the benefits of standard camera stacking:
- No post process bleeding on UI (motion vectors, ect.)
- No clipping in the geometry

## Installation

The UI Camera Stacking package supports the following versions:

Unity Version | HDRP Version | Compatible
--- | --- | ---
2019.4.x | 7.x | ❌
2020.1.x | 8.x | ❌
2020.2.x and 2020.3.x | 10.x | ✔️
2021.1.x | 11.x | ✔️
2021.2.x | 12.x | ✔️

<details><summary>Instructions</summary>

HDRP UI Camera stacking is available on the [OpenUPM](https://openupm.com/packages/com.alelievr.hdrp-ui-camera-stacking/) package registry, to install it in your project, follow the instructions below.

1. Open the `Project Settings` and go to the `Package Manager` tab.
2. In the `Scoped Registry` section, click on the small `+` icon to add a new [scoped registry](https://docs.unity3d.com/2020.2/Documentation/Manual/upm-scoped.html) and fill the following information:
```
Name:     Open UPM
URL:      https://package.openupm.com
Scope(s): com.alelievr
```
3. Next, open the `Package Manager` window, select `My Registries` in the top left corner and you should be able to see the **HDRP UI Camera Stacking** package.
4. Click the `Install` button and you can start using the package :)

![PackageManager](https://user-images.githubusercontent.com/6877923/127833767-8ffcaa0d-a655-4abd-820e-c08182eb51f8.png)
  
:warning: If you don't see `My Registries` in the dropdown for some reason, click on the `+` icon in the top left corner of the package manager window and select `Add package from Git URL`, then paste `com.alelievr.hdrp-ui-camera-stacking` and click `Add`.

Note that sometimes, the package manager can be slow to update the list of available packages. In that case, you can force it by clicking the circular arrow button at the bottom of the package list.

</details>

## Tutorial

First, create a UI Camera gameobject with the menu **UI > Camera (HDRP)**

![image](https://user-images.githubusercontent.com/6877923/127682755-234353a1-9562-4d1e-b659-ac61928632d4.png)

In this new UI Camera there is a component called **HD Camera UI**, this is the component that will do the rendering of the UI. It's important to correctly configure this component because the camera depth and culling mask are ignored when this component is added.

![image](https://user-images.githubusercontent.com/6877923/127683260-89a0060a-02d5-4612-ac7c-94f95e6f1879.png)

The **Ui Layer Mask** parameter is the layer mask of your UI objects, by default it's set to **UI**. And the priority is used to define a draw order between the UI cameras, a high priority means rendered in front of the other cameras.

In the UI Camera gameobject a canvas was also created and correctly configured with the "Screen Space - Camera" mode. You can add your UI in this canvas.

## Performances

In HDRP using more than one camera have a very high performance cost. While you can avoid most of the performance issue on the GPU side with correct culling settings and disabling almost every in the frame settings, you won't be able to escape the CPU cost.

The scenes used for the performance test are available in the Benchmark folder of the project. For HDRP, the [Graphics Compositor](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@12.0/manual/Compositor-Main.html) was used to perform the UI camera stacking. The UI camera had custom frame settings optimized to render GUI (transparent unlit objects).

Setup | CPU Time (ms) | GPU Time (ms)
--- | --- | --- 
HDRP camera stacking | 0.80 | 0.23
Custom UI camera stacking | 0.05 | 0.19

Without much surprise, we can see a big difference on CPU side, mostly because we're skipping all the work of a standard HDRP camera. On the GPU side things are pretty even except a slight overhead due to the compute shader work that can't be disabled in the frame settings. 

## Limitations

Rendering Lit objects is not supported. Currently the UI rendering happen before the rendering of the main camera, thus before any lighting structure is built so it's not possible to access the lighting data when rendering the UI for camera stacking.

## Future Improvements

- Fullscreen effect applied after rendering the UI
- Custom compositing shader
