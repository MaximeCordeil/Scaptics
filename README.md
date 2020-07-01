# Scaptics

Scaptics is a Unity library to provide Haptic Feedback to explore 3D scatterplots.
It has been presented at CHI 2019: [Paper](https://hal.archives-ouvertes.fr/hal-02018632/document/) [Video](https://www.youtube.com/watch?v=-LR1fZIVUWw&feature=youtu.be)

## Set up

Scaptics for now only support SteamVR (One version is provided in this repository and can be directly copied in your project).

To set up:
1. Download this repository
2. Either open it with Unity 2017.* or copy the repository "VolumeRendering" in your project
3. Include the prefab "VolumeRendering" in your scene
4. To control the visual of the density of the 3D scatterplots, you can include the prefab "CanvasControl"

## Settings of the Volume Rendering Prefab

This prefab is mostly already setup, there are still a few fields that can be modified. Below is an explaination for fws of those.

![Volume Rendering Settings](https://i.ibb.co/QcQPdcY/Volume-Rendering-Prefab.png)

- Color: Color of the density visualisation
- Data: Data Source of the scatterplot. For now it reads the CSV data source of [IATK](https://github.com/MaximeCordeil/IATK), alkready included in this repository
- Haptic Pointer: Game Object that is used to point at a specific point in the scatterplot
- Vibration Step: The different vibration step for the "Basic" filter
- Mu, Sigma, Derivative, Kernel Size: Parameters of the Kernel Density Estimation used to calculate the 3D density of the scatterplot
- Is Log: Check if you want to map vibration to cluster boundary instead of just density of clusters (Use of the derivative of the density)
- Filter: Filter to map the density to the vibration. Basic: discretization of the density with 6 vibration steps, Linear: linear mapping between the density and the vibration, Square: Use of one density threshold to go from one vibration to another (step 0 and 1), Sigmoid: Continuous Square filter
- Vibrate Controller: SteamVR controller to vibrate
- Slice X Min/Max, Y Min/Max, Z Min/Max: Cutting plane for the visualisation of the density
