using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ButtonHandler : MonoBehaviour
{

    public bool filter0;
    public bool filter1;
    public bool filter2;
    public bool filter3;
    public bool filter4;

    public ControllerFilter controller;

    public VolumeRendering.VolumeRendering v;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "VrController")
        {
            if (filter0)
            {
                v.filter = VolumeRendering.FilterType.Basic;
                controller.buttonHolder.SetActive(false);
                controller.buttonEnabled = false;
            }

            if (filter1)
            {
                v.filter = VolumeRendering.FilterType.Square;
                controller.buttonHolder.SetActive(false);
                controller.buttonEnabled = false;
            }

            if (filter2)
            {
                v.filter = VolumeRendering.FilterType.Sigmoid;
                controller.buttonHolder.SetActive(false);
                controller.buttonEnabled = false;
            }

            if (filter3)
            {
                v.filter = VolumeRendering.FilterType.Linear;
                controller.buttonHolder.SetActive(false);
                controller.buttonEnabled = false;
            }

            if (filter4)
            {
                v.filter = VolumeRendering.FilterType.Frequency;
                controller.buttonHolder.SetActive(false);
                controller.buttonEnabled = false;
            }
        }
    }

}
