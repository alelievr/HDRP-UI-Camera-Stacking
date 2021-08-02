# HDRP UI Camera Stacking

The HDRP UI Camera Stacking package allows you to stack multiple camera rendering UI only at a fraction of the cost of a standard camera.

This is achieved by taking advantage of the [customRender](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@12.0/api/UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.html#UnityEngine_Rendering_HighDefinition_HDAdditionalCameraData_customRender) feature to render only the GUI elements and nothing else, with the only downside to not be able to render Lit objects which are generally found for UI.

https://user-images.githubusercontent.com/6877923/127684238-1f149a4f-1677-4428-b3f8-7ba51c6c93d6.mp4


This implementation also provides all the benefits of standard camera stacking:
- No post process bleeding on UI (motion vectors, ect.)
- No clipping in the geometry

## Installation

TODO

## Tutorial

First, create a UI Camera gameobject with the menu **UI > Camera (HDRP)**

![image](https://user-images.githubusercontent.com/6877923/127682755-234353a1-9562-4d1e-b659-ac61928632d4.png)

In this new UI Camera there is a component called **HD Camera UI**, this is the component that will do the rendering of the UI. It's important to correctly configure this component because the camera depth and culling mask are ignored when this component is added.

![image](https://user-images.githubusercontent.com/6877923/127683260-89a0060a-02d5-4612-ac7c-94f95e6f1879.png)

The **Ui Layer Mask** parameter is the layer mask of your UI objects, by default it's set to **UI**. And the priority is used to define a draw order between the UI cameras, a high priority means rendered in front of the other cameras.

In the UI Camera gameobject a canvas was also created and correctly configured with the "Screen Space - Camera" mode. You can add your UI in this canvas.
