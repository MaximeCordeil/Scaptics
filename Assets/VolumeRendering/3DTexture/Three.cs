using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Three : MonoBehaviour {

   Texture3D texture;

    void Start ()
    {
        texture = CreateTexture3D (256);
//        renderer. material.SetTexture("_Volume", texture;
        Renderer r = gameObject.AddComponent<Renderer>();
        Material mt = new Material("DX11/Sample3DTexture");
        mt.SetTexture("_Volume",texture);
        r.material = mt;
    }

    Texture3D CreateTexture3D (int size)
    {
        Color[] colorArray = new Color[size * size * size];
        texture = new Texture3D (size, size, size, TextureFormat.RGBA32, true);
        float r = 1.0f / (size - 1.0f);
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                for (int z = 0; z < size; z++) {
                    if (Mathf.Sin(x) % Mathf.PI > 0f)
                    {
                        Color c = new Color(x * r, y * r, z * r, 1.0f);
                        colorArray[x + (y * size) + (z * size * size)] = c;
                    }
                }
            }
        }
        texture.SetPixels (colorArray);
        texture.Apply ();
        return texture;
    }

    

    // Update is called once per frame
    void Update()
    {

    }
}
