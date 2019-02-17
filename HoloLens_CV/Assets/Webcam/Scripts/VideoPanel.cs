//  
// Copyright (c) 2017 Vulcan, Inc. All rights reserved.  
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.
//

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

// Define the functions which can be called from the .dll.

    
internal static class OpenCVInterop{
    [DllImport("HandTrackingDLL")]
    internal unsafe static extern int Init(int w, int h, int channels, int* version);
 
    [DllImport("HandTrackingDLL")]
    internal static extern int Close();
 
    [DllImport("HandTrackingDLL")]
    internal unsafe static extern int Operate(byte* data, byte* data_out, int op, double* position, byte* msg);
}

/*
internal static class OpenCVInterop
{
    [DllImport("opencv_binding_x32_UWP")]
    internal unsafe static extern int Init(int w, int h, int channels, int* version);

    [DllImport("opencv_binding_x32_UWP")]
    internal static extern int Close();

    [DllImport("opencv_binding_x32_UWP")]
    internal unsafe static extern int Operate(byte* data, byte* data_out, int op, double* position, byte* msg);
}
*/

//=========================================================================
//
//=========================================================================
public class VideoPanel : MonoBehaviour{
    //public RawImage rawImage;
    public GameObject camera_owner;
    //public GameObject debug_sphere;
    private VideoSource video_source;
    private bool hasResolution=false;
    private byte[] image;
    private byte[] image_processed;
    public string stream_type;
    public int operation = 5;

    //private Texture2D textureU8;
    //private Texture2D textureRGBA;

    private Vector3 scaling;
    private Vector3 offset;
    private Vector3 tracker_pos;
    public bool show_panel = false;
    private int lib_version = 0;

    //public GameObject debug_sphere2;
    //private GameObject camGameObject;
    //private Transform camTransform;
    //=========================================================================
    public void Start(){
        //camGameObject = GameObject.FindGameObjectWithTag("MainCamera");
        //camTransform = camGameObject.GetComponent<Transform>();

        video_source = camera_owner.GetComponent<VideoSource>();
        scaling.x = 0.3f;
        scaling.y = -0.3f;
        //scaling.z = 0.000f;
        scaling.z = 0.004f;

        offset.x = -0.01f;
        offset.y = -0.15f;
        //offset.z = 1.0f;
        //offset.z = 0.12f;
        offset.z = 0.0f;

    }

    //=========================================================================
    unsafe public void SetResolution(int width, int height){

        //textureU8   = new Texture2D(width, height, TextureFormat.R8, false);
        //textureRGBA = new Texture2D(width, height, TextureFormat.RGBA32, false);
        //if(operation<100){
        //    rawImage.texture = textureU8;   
        //}else{
        //    rawImage.texture = textureRGBA;
        //}
        
        // take the biggest format so we dont have to care later
        int channels    = 4;
        image           = new byte[width*height*channels];
        image_processed = new byte[width*height*channels];

        // init the opencv processor, 2 is CV_U16
        int input_format = 2;

        fixed(int* plib_version = &lib_version){
            OpenCVInterop.Init(width, height, input_format, plib_version);
        }
        hasResolution = true;
    }

    //=========================================================================
    public void Update(){
        // nothing to display if we don't have a source
        if(video_source==null){
            return;
        }

        // if its the first time, create the required texture
        if(!hasResolution){
            int width=0;
            int height=0;
            int ret = video_source.Get_Resolution(stream_type, ref width, ref height);
            if (ret<0){
                return;
            }
            SetResolution(width, height); 
        }

        video_source.Get_Image(stream_type, image);
        if (image == null)
        {
            return;
        }

        double[] position = new double[3];
        byte[] msg     = new byte[1024];
        if(stream_type=="depth"){
            int n = image.Length;

            int ret = 0;
            unsafe{
                // be careful this only works properly if data has the correct size,
                // so select Read/Write option in Unity and select uncompressed RGBA32
                fixed(byte* pimage = image){ //pixels_in
                    fixed(byte* pimage_processed = image_processed){
                        fixed(double* pposition = position){
                            fixed(byte* pmsg = msg){
                                ret =  OpenCVInterop.Operate(pimage, pimage_processed, operation, pposition, pmsg);
                            }
                        }
                    }
                }
            }
            if (ret<0){
                Debug.Log("OpenCVInterop.Operate returned " + ret );
                Debug.Log("Exception:" + System.Text.Encoding.UTF8.GetString(msg) );
                return;
            }

            // set the bytes to be the texture and trigger a GPU update
        }else{
            // set the bytes to be the texture and trigger a GPU update
        }


        //rawImage.enabled = show_panel;
        //if (rawImage.enabled ){
        //
        //    if(operation<100){
        //        rawImage.texture = textureU8;   
        //    }else{
        //        rawImage.texture = textureRGBA;
        //    }
        //
        //    Texture2D texture = rawImage.texture as Texture2D;
        //    texture.LoadRawTextureData( image_processed );
        //    texture.Apply();
        //}


        tracker_pos.x = (float) (scaling.x*position[0] + offset.x);
        tracker_pos.y = (float) (scaling.y*position[1] + offset.y);
        tracker_pos.z = (float) (scaling.z*position[2] + offset.z);

        this.transform.localPosition = tracker_pos;
    }

    //=========================================================================
    public void GetTrackingLocation(ref Vector3 location){

        location = this.transform.position;

        //Debug.Log("x: " + tracker_pos.x + " y:" + tracker_pos.y + " z:" + tracker_pos.z);
    }

    public Vector3 GetLocalTrackingLocation()
    {
        return this.transform.localPosition;
    }
}
