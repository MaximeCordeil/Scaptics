using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using System.Diagnostics;

namespace VolumeRendering
{

    public enum FilterType
    {
        Basic,
        Linear,
        Square,
        Sigmoid,
        Frequency
    }

    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class VolumeRendering : MonoBehaviour
    {

        [SerializeField] protected Shader shader;
        protected Material material;

        [SerializeField] Color color = Color.white;
        [Range(0f, 1f)] public float threshold = 0.5f;
        [Range(1f, 5f)] public float intensity = 1.5f;

        [SerializeField] protected Texture3D volumeGaus;
        [SerializeField] protected Texture3D volumeLoG;
        public IATK.DataSource data;
        private Vector3[] data2;

        // controller of which the position is taken
        //[SerializeField]
        //public GameObject brush;

        [SerializeField]
        private GameObject HapticPointer;

        //[SerializeField]
        //public OculusHaptics haptics;

        //private fields
        private Color[] textureColors;
        //public TextMesh tm = null;
        //public TextMesh tm2 = null;
        //public TextMesh tm3 = null;

        //parameters
        [SerializeField]
        public float intensityMultiplier;
        //public List<ushort> vibrationValues = new List<ushort>();
        //public ushort noVibration = 0;
        //public ushort lowVibration = 300;
        //public ushort mediumVibration = 600;
        //public ushort highVibration = 1000;
        //public ushort veryHighVibration = 2000;
        //public ushort maxVibration = 3000;

        [SerializeField]
        private ushort[] VibrationStep;


        //gaussian parameters
        public float mu = 0f;
        public float sigma = 0.15f;
        public int derivative = 0;
        public int kernelSize = 5;

        //gaussian or LoG
        public bool isLog;

        //Filter
        //public string filter;
        public FilterType filter = FilterType.Basic;

        public float coefIntensity;

        int derivativeMemory = 0;
        bool isLogMemory;
        Vector3[] positions;
        int count = 0;

        Color[] _shape3;
        Color[,,] _shape2;
        Color[] _shapeGauss;

        float _intensity;
        float _intensity2;
        float _intensity3;
        ushort vib = 0;

        private System.TimeSpan time;
        Stopwatch stopWatch = new Stopwatch();
        int frequency;

        // number of points create randomly
        private int size = 100;

        private float intensityRegulator = 1f;

        // controller which use the trigger to change isLog
        // public SteamVR_TrackedController trackedController;
        // controller which is going to vibrate
        public SteamVR_TrackedObject vibrateController;


        [Range(0f, 1f)] public float sliceXMin = 0.0f, sliceXMax = 1.0f;
        [Range(0f, 1f)] public float sliceYMin = 0.0f, sliceYMax = 1.0f;
        [Range(0f, 1f)] public float sliceZMin = 0.0f, sliceZMax = 1.0f;

        protected void Start()
        {
            //Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            time = stopWatch.Elapsed;
            //UnityEngine.Random.InitState(DateTime.Now.Millisecond);
            UnityEngine.Random.InitState(42);
            material = new Material(shader);
            GetComponent<MeshFilter>().sharedMesh = Build();
            GetComponent<MeshRenderer>().sharedMaterial = material;
            volumeGaus = CreateTexture3D(256);
            volumeLoG = CreateTexture3D(256);
            // populateTexture(new Vector3[] { Vector3.one / 2f}, ref volume, 30);

            float[] xs = data["x"].Data;
            float[] ys = data["y"].Data;
            float[] zs = data["z"].Data;

            data2 = InitialiseRandomData(size);

            positions = new Vector3[100];

            positions = new Vector3[data[0].Data.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = new Vector3(xs[i], ys[i], zs[i]);
            }

            //transparent volume shading
            material.renderQueue = 3500;

            //filter = "no filter";
            isLogMemory = isLog;

            LogPipeline();

            /*stopWatch.Stop();
            print(stopWatch.Elapsed);*/

        }

        // create the gaussian and laplacian of gaussian distributions
        private void LogPipeline()
        {
            //creation of the gaussian distribution
            gaussSpheres(positions, ref volumeGaus, kernelSize);

            //computing the LoG
            _shape2 = applyLpoGConvolutionColor();

            _shape3 = volumeLoG.GetPixels();

            for (int i = 0; i < volumeLoG.depth; i++)
            {
                for (int j = 0; j < volumeLoG.depth; j++)
                {
                    for (int k = 0; k < volumeLoG.depth; k++)
                    {
                        _shape3[(k * volumeLoG.depth * volumeLoG.depth) + j * volumeLoG.depth + i] = _shape2[i, j, k];
                    }
                }
            }

            //apply color array to the Volume texture according to the filter

            IsLoG(isLog);
        }

        protected void Update()
        {
            material.SetColor("_Color", color);
            material.SetFloat("_Threshold", threshold);
            material.SetFloat("_Intensity", intensity * intensityMultiplier);
            material.SetVector("_SliceMin", new Vector3(sliceXMin, sliceYMin, sliceZMin));
            material.SetVector("_SliceMax", new Vector3(sliceXMax, sliceYMax, sliceZMax));

            if (isLog)
            {
                VolumeHandling(ref volumeLoG);
            }
            else
            {
                VolumeHandling(ref volumeGaus);
            }
        }

        // load the texture and apply the vibriation
        private void VolumeHandling(ref Texture3D volume)
        {
            Vector3 positionInVolume = transform.InverseTransformPoint(HapticPointer.transform.position);

            /*Vector3 positionInVolume = transform.InverseTransformPoint(brush.transform.localPosition);
            Vector3 positionInVolume2 = transform.InverseTransformPoint(brush.transform.Find("ViewFinder").transform.Find("Sphere").gameObject.transform.position);
            Vector3 positionInVolume3 = transform.InverseTransformPoint(brush.transform.Find("ViewFinder").transform.Find("Sphere2").gameObject.transform.position);*/

            /*if (trackedController.triggerPressed)
            {
                if (isLog)
                {
                    isLog = false;
                }
                else
                {
                    isLog = true;
                }
            }*/

            if (isLogMemory != isLog)
            {
                resetColor(ref volume);
                IsLoG(isLog);
                isLogMemory = isLog;
            }

            if (positionInVolume.x >= -0.5f && positionInVolume.x <= 0.5f
                && positionInVolume.y >= -0.5f && positionInVolume.y <= 0.5f
                && positionInVolume.z >= -0.5f && positionInVolume.z <= 0.5f)
            {

                _intensity = intensityBrush(positionInVolume, volume.depth) * intensityMultiplier * intensityRegulator;

                /*_intensity2 = intensityBrush(positionInVolume2, volume.depth) * intensityMultiplier * intensityRegulator;
                _intensity3 = intensityBrush(positionInVolume3, volume.depth) * intensityMultiplier * intensityRegulator;
                _intensity = (_intensity2 + _intensity3) / 2;*/

                //tm.text = _intensity.ToString();

                Filter(filter);

                //print(_intensity);

                //tm3.text = vib.ToString();

                //tm2.text = filter;

                // ca ca fait vibrer le controller
                if (stopWatch.Elapsed.TotalMilliseconds >= time.TotalMilliseconds + frequency)
                {
                    var device = SteamVR_Controller.Input((int)vibrateController.index);
                    device.TriggerHapticPulse(vib);
                    time = stopWatch.Elapsed;
                }
            }

            derivativeMemory = derivative;

            for (int i = 0; i < 10; i++)
            {
                print(positions[i]);
            }

        }

        // load the texture according to isLog
        public void IsLoG(bool b)
        {
            switch (b)
            {
                case true:
                    intensityRegulator = 1f;
                    volumeLoG.SetPixels(_shape3);
                    volumeLoG.Apply();
                    material.SetTexture("_Volume", volumeLoG);
                    textureColors = _shape3;
                    break;
                default:
                    intensityRegulator = 0.0125f;
                    volumeGaus.SetPixels(_shapeGauss);
                    volumeGaus.Apply();
                    material.SetTexture("_Volume", volumeGaus);
                    textureColors = _shapeGauss;
                    break;
            }
        }

        // apply the selected filter
        public void Filter(FilterType index)
        {
            switch (index)
            {
                case FilterType.Square:
                    frequency = 0;
                    if (_intensity < 0.2f) { vib = VibrationStep[0]; }
                    if (_intensity >= 0.2f) { vib = VibrationStep[3]; }
                    break;
                case FilterType.Sigmoid:
                    frequency = 0;
                    if (_intensity < 0.2f) { vib = VibrationStep[0]; }
                    if (_intensity >= 0.2f) { vib = (ushort)(2000 / (1 + Math.Exp(-((_intensity - 0.5) * 10)))); }
                    break;
                case FilterType.Linear:
                    frequency = 0;
                    if (_intensity <= 0)
                    {
                        vib = 0;
                    }
                    else
                    {
                        vib = (ushort)(_intensity * 1500);
                    }
                    break;
                case FilterType.Frequency:
                    vib = VibrationStep[4];
                    if (_intensity <= 0f) { frequency = 1000; }
                    if (_intensity <= 0.3f && _intensity > 0f) { frequency = 500; }
                    if (_intensity <= 0.6f && _intensity > 0.3f) { frequency = 200; }
                    if (_intensity <= 1f && _intensity > 0.6f) { frequency = 100; }
                    if (_intensity <= 2f && _intensity > 1f) { frequency = 50; }
                    if (_intensity > 2f) { frequency = 10; }
                    break;
                case FilterType.Basic:
                    frequency = 0;
                    if (_intensity <= 0f) { vib = VibrationStep[0]; }
                    if (_intensity <= 0.3f && _intensity > 0f) { vib = VibrationStep[1]; }
                    if (_intensity <= 0.6f && _intensity > 0.3f) { vib = VibrationStep[2]; }
                    if (_intensity <= 1f && _intensity > 0.6f) { vib = VibrationStep[3]; }
                    if (_intensity <= 2f && _intensity > 1f) { vib = VibrationStep[4]; }
                    if (_intensity > 2f) { vib = VibrationStep[5]; }
                    break;
            }
        }

        // create points randomly 
        public Vector3[] InitialiseRandomData(int size)
        {
            Vector3[] data = new Vector3[size];

            // create random points
            for (int i = 0; i < size - 60; i++)
            {
                data[i] = new Vector3(UnityEngine.Random.Range(0f, 1f),
                                      UnityEngine.Random.Range(0f, 1f),
                                      UnityEngine.Random.Range(0f, 1f));
            }

            // create clusters
            for (int i = 0; i < 20; i++)
            {
                data[size - 40 + i] = new Vector3(1f / (2.5f + UnityEngine.Random.Range(0f, 2f)),
                                      1f / (2.5f + UnityEngine.Random.Range(0f, 2f)),
                                      1f / (2.5f + UnityEngine.Random.Range(0f, 2f)));
            }

            for (int i = 0; i < 20; i++)
            {
                data[size - 20 + i] = new Vector3(1f / (2f + UnityEngine.Random.Range(0f, 1f)),
                                      1f / (1.33f + UnityEngine.Random.Range(0f, 0.33f)),
                                      1f / (1.2f + UnityEngine.Random.Range(0f, 0.2f)));
            }

            for (int i = 0; i < 20; i++)
            {
                data[size - 60 + i] = new Vector3(1f / (1.2f + UnityEngine.Random.Range(0f, 0.2f)),
                                      1f / (1.75f + UnityEngine.Random.Range(0f, 0.75f)),
                                      1f / (2.5f + UnityEngine.Random.Range(0f, 2f)));
            }

            return data;
        }

        // Normal (or gauss) distribution function
        public double gauss(double x, double μ, double σ)
        {
            return 1d / Math.Sqrt(2 * σ * σ * Math.PI) * Math.Exp(-((x - μ) * (x - μ)) / (2 * σ * σ));
        }

        //returns the intensity of the kernel at the position
        public float intensityBrush(Vector3 position, int texDepth)
        {
            int x = Mathf.RoundToInt((position.x + 0.5f) * texDepth);
            int y = Mathf.RoundToInt((position.y + 0.5f) * texDepth);
            int z = Mathf.RoundToInt((position.z + 0.5f) * texDepth);

            //test we are still in the matrix:
            if (z >= 0 && z < texDepth && x >= 0 && x < texDepth && y >= 0 && y < texDepth)
            {
                float intensityValue = textureColors[x + (y * texDepth) + (z * texDepth * texDepth)].r;
                return intensityValue;
            }
            else return -1f;
        }

        // compute the gaussian distribution ant apply it to the texture
        public void gaussSpheres(Vector3[] pointCloud, ref Texture3D tex3D, int kernelSize)
        {
            int texDepth = tex3D.depth;

            //pull out the color array from the texture
            textureColors = tex3D.GetPixels();

            for (int i = 0; i < pointCloud.Length; i++)
            {
                // find the depth bin
                int depthBin = Mathf.RoundToInt(pointCloud[i].z * texDepth);
                int xCenter = Mathf.RoundToInt(pointCloud[i].x * texDepth);
                int yCenter = Mathf.RoundToInt(pointCloud[i].y * texDepth);
                Vector3 center = new Vector3(xCenter, yCenter, depthBin);

                float distanceMax = Vector3.Distance(new Vector3(xCenter - kernelSize, yCenter - kernelSize, depthBin - kernelSize), center);

                for (int z = depthBin - kernelSize; z < depthBin + kernelSize; z++)
                {
                    for (int x = xCenter - kernelSize; x < xCenter + kernelSize; x++)
                    {
                        for (int y = yCenter - kernelSize; y < yCenter + kernelSize; y++)
                        {
                            //test we are still in the matrix:
                            if (z >= 0 && z < texDepth && x >= 0 && x < texDepth && y >= 0 && y < texDepth)
                            {
                                Vector3 kernelPoint = new Vector3(x, y, z);
                                //test if in the sphere of radius kernel
                                if (brushSphere(center, (float)kernelSize, kernelPoint))
                                {
                                    float distance = Vector3.Distance(kernelPoint, center);
                                    distance /= distanceMax;
                                    // print(distance);
                                    //apply kernel

                                    float gaussianValue = (float)gauss((double)distance, mu, sigma);

                                    textureColors[x + (y * texDepth) + (z * texDepth * texDepth)].r += gaussianValue * coefIntensity;
                                    textureColors[x + (y * texDepth) + (z * texDepth * texDepth)].g += gaussianValue * coefIntensity;
                                    textureColors[x + (y * texDepth) + (z * texDepth * texDepth)].b += gaussianValue * coefIntensity;


                                }
                            }
                        }
                    }
                }
            }

            _shapeGauss = textureColors;
        }

        // return an array of color after application of the LoG
        public Color[,,] applyLpoGConvolutionColor()
        {
            int size = volumeLoG.depth;

            float[,,] LoG_Kernel = new float[3, 3, 3]
            {
                {{0,0,0},{0,1,0},{0,0,0}},
                {{0,1,0},{1,-6,1},{0,1,0}},
                {{0,0,0},{0,1,0},{0,0,0}},

            };

            // clement work
            //   textureColors
            Color[,,] shape = new Color[size, size, size];
            for (int x = 1; x < size - 1; x++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    float color1 = textureColors[x + y * size].r;
                    shape[x, y, 0] = new Color(color1, color1, color1, 1f);
                    //   shape[x, y, (size-1)] = new Color(textureColors[x + y * size + size * size * size].r, textureColors[x + y * size + size * size * size].r, textureColors[x + y * size + size * size * size].r, 1f);

                    for (int z = 1; z < size - 1; z++)
                    {
                        float color = LoG_Kernel[0, 0, 0] * textureColors[(x - 1) + ((y - 1) * size) + ((z - 1) * size * size)].r + LoG_Kernel[0, 0, 1] * textureColors[(x - 1) + ((y - 1) * size) + (z * size * size)].r +
                            LoG_Kernel[0, 0, 2] * textureColors[(x - 1) + ((y - 1) * size) + ((z + 1) * size * size)].r + LoG_Kernel[0, 1, 0] * textureColors[(x - 1) + (y * size) + ((z - 1) * size * size)].r +
                            LoG_Kernel[0, 1, 1] * textureColors[(x - 1) + (y * size) + (z * size * size)].r + LoG_Kernel[0, 1, 2] * textureColors[(x - 1) + (y * size) + ((z + 1) * size * size)].r +
                            LoG_Kernel[0, 2, 0] * textureColors[(x - 1) + ((y + 1) * size) + ((z - 1) * size * size)].r + LoG_Kernel[0, 2, 1] * textureColors[(x - 1) + ((y + 1) * size) + (z * size * size)].r +
                            LoG_Kernel[0, 0, 2] * textureColors[(x - 1) + ((y + 1) * size) + ((z + 1) * size * size)].r +
                            LoG_Kernel[1, 0, 0] * textureColors[x + ((y - 1) * size) + ((z - 1) * size * size)].r + LoG_Kernel[1, 0, 1] * textureColors[x + ((y - 1) * size) + (z * size * size)].r +
                            LoG_Kernel[1, 0, 2] * textureColors[x + ((y - 1) * size) + ((z + 1) * size * size)].r + LoG_Kernel[1, 1, 0] * textureColors[x + (y * size) + ((z - 1) * size * size)].r +
                            LoG_Kernel[1, 1, 1] * textureColors[x + (y * size) + (z * size * size)].r + LoG_Kernel[1, 1, 2] * textureColors[x + (y * size) + ((z + 1) * size * size)].r +
                            LoG_Kernel[1, 2, 0] * textureColors[x + ((y + 1) * size) + ((z - 1) * size * size)].r + LoG_Kernel[1, 2, 1] * textureColors[x + ((y + 1) * size) + (z * size * size)].r +
                            LoG_Kernel[1, 0, 2] * textureColors[x + ((y + 1) * size) + ((z + 1) * size * size)].r +
                            LoG_Kernel[2, 0, 0] * textureColors[(x + 1) + ((y - 1) * size) + ((z - 1) * size * size)].r + LoG_Kernel[2, 0, 1] * textureColors[(x + 1) + ((y - 1) * size) + (z * size * size)].r +
                            LoG_Kernel[2, 0, 2] * textureColors[(x + 1) + ((y - 1) * size) + ((z + 1) * size * size)].r + LoG_Kernel[2, 1, 0] * textureColors[(x + 1) + (y * size) + ((z - 1) * size * size)].r +
                            LoG_Kernel[2, 1, 1] * textureColors[(x + 1) + (y * size) + (z * size * size)].r + LoG_Kernel[2, 1, 2] * textureColors[(x + 1) + (y * size) + ((z + 1) * size * size)].r +
                            LoG_Kernel[2, 2, 0] * textureColors[(x + 1) + ((y + 1) * size) + ((z - 1) * size * size)].r + LoG_Kernel[2, 2, 1] * textureColors[(x + 1) + ((y + 1) * size) + (z * size * size)].r +
                            LoG_Kernel[2, 0, 2] * textureColors[(x + 1) + ((y + 1) * size) + ((z + 1) * size * size)].r;
                        shape[x, y, z] = new Color(color, color, color, 1f);


                    }
                }
            }
            return shape;
        }

        //brush sphere
        bool brushSphere(Vector3 center, float radius, Vector3 testPoint)
        {
            Vector3 displacementToCenter = testPoint - center;
            float radiusSqr = radius;
            float magnitude = Vector3.Magnitude(displacementToCenter);
            bool intersects = magnitude < radiusSqr;

            return intersects;
        }

        void resetColor(ref Texture3D tex3D)
        {
            int _size = tex3D.width;

            Color[] colorArray = new Color[_size * _size * _size];
            float r = 1.0f / (_size - 1.0f);
            for (int x = 0; x < _size; x++)
            {
                for (int y = 0; y < _size; y++)
                {
                    for (int z = 0; z < _size; z++)
                    {
                        colorArray[x + (y * _size) + (z * _size * _size)] = new Color(0f, 0f, 0f, 0f);
                    }
                }
            }
            tex3D.SetPixels(colorArray);
            tex3D.Apply();
        }

        Texture3D CreateTexture3D(int size)
        {
            Color[] colorArray = new Color[size * size * size];
            Texture3D texture = new Texture3D(size, size, size, TextureFormat.RGBA32, true);
            float r = 1.0f / (size - 1.0f);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        colorArray[x + (y * size) + (z * size * size)] = new Color(0f, 0f, 0f, 0f);
                    }
                }
            }
            texture.SetPixels(colorArray);
            texture.Apply();
            return texture;
        }

        Mesh Build()
        {
            var vertices = new Vector3[] {
                new Vector3 (-0.5f, -0.5f, -0.5f),
                new Vector3 ( 0.5f, -0.5f, -0.5f),
                new Vector3 ( 0.5f,  0.5f, -0.5f),
                new Vector3 (-0.5f,  0.5f, -0.5f),
                new Vector3 (-0.5f,  0.5f,  0.5f),
                new Vector3 ( 0.5f,  0.5f,  0.5f),
                new Vector3 ( 0.5f, -0.5f,  0.5f),
                new Vector3 (-0.5f, -0.5f,  0.5f),
            };
            var triangles = new int[] {
                0, 2, 1,
                0, 3, 2,
                2, 3, 4,
                2, 4, 5,
                1, 2, 5,
                1, 5, 6,
                0, 7, 4,
                0, 4, 3,
                5, 4, 7,
                5, 7, 6,
                0, 6, 7,
                0, 1, 6
            };

            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }

        void OnValidate()
        {
            Constrain(ref sliceXMin, ref sliceXMax);
            Constrain(ref sliceYMin, ref sliceYMax);
            Constrain(ref sliceZMin, ref sliceZMax);
        }

        void Constrain(ref float min, ref float max)
        {
            const float threshold = 0.025f;
            if (min > max - threshold)
            {
                min = max - threshold;
            }
            else if (max < min + threshold)
            {
                max = min + threshold;
            }
        }

        void OnDestroy()
        {
            Destroy(material);
        }

        // change the value of isLog
        // called when the trigger id pressed
        public void ChangeIsLog()
        {
            if (isLog)
            {
                isLog = false;
            }
            else
            {
                isLog = true;
            }
        }

    }

}


