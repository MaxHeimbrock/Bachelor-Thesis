
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
#if !UNITY_EDITOR
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using HoloLensCameraStream;
#endif
/// <summary>
/// This example gets the video frames at 30 fps and displays them on a Unity texture,
/// which is locked the User's gaze.
/// </summary>
/// 
//=========================================================================
//
//=========================================================================
public class VideoSource : MonoBehaviour{
    public Boolean videoCaptureRunning =false;
    public HoloLensCameraStream.Resolution _resolution;
    public Queue<byte[]> frames;

    private byte[] _latestImageBytes;

#if !UNITY_EDITOR
    VideoCapture _videoCapture;
    IntPtr _spatialCoordinateSystemPtr; 
#endif
    //=========================================================================
    void Start(){
#if !UNITY_EDITOR
        frames = new Queue<byte[]>();
        //cameraStreamHelper = CameraStreamHelper.GetInstance();

        //Fetch a pointer to Unity's spatial coordinate system if you need pixel mapping
        _spatialCoordinateSystemPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
        
        _videoCapture = new VideoCapture();
        _videoCapture.CreateAsync( MediaStreamType.VideoRecord, MediaFrameSourceKind.Depth, "D16");

#endif
    }

    //=========================================================================
    private void OnDestroy(){

#if !UNITY_EDITOR
        /*
        if (_videoCapture != null){
            _videoCapture.FrameSampleAcquired -= OnFrameSampleAcquired;
            _videoCapture.Dispose();
        }
        */
#endif
    }

    // ==========================================================================================================
#if !UNITY_EDITOR
    void OnVideoModeStarted(VideoCaptureResult result){
        if (result.success == false){
            Debug.LogWarning("Could not start video mode.");
            return;
        }

        Debug.Log("Video capture started.");
        videoCaptureRunning=true;
    }
#endif

    //=========================================================================

    public int Get_Resolution(string type, ref int x, ref int y){

#if !UNITY_EDITOR
        if(type=="color"){
            return _videoCapture.get_color_resolution(ref x, ref y);
        }

        if(type=="infrared"){
             return -1;// _videoCapture.depth_image.CopyTo(image);
        }
        if(type=="depth"){
            return _videoCapture.get_depth_resolution(ref x, ref y);
        } 
        return -1;
#else
        x = 0;
        y = 0;
        return 0;
#endif
    }

    //=========================================================================

    public void Get_Image(string type, byte[] image){
#if !UNITY_EDITOR
        if(type=="color"){
            _videoCapture.get_color_image(image);
        }
        if(type=="infrared"){
             // _videoCapture.depth_image.CopyTo(image);
        }
        if(type=="depth"){
            _videoCapture.get_depth_image(image);
        } 
#endif
    }


}
