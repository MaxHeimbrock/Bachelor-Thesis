using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if !UNITY_EDITOR
using System;
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

public class SensorViewControlOld : MonoBehaviour {

    private Texture2D tex = null;
    private byte[] bytes = null;
    private byte[] my_bytes = null;
    // Use this for initialization
    void Start() {
#if !UNITY_EDITOR
        Task.Run(() => { InitSensor(); });
		
		Debug.Log("Script started");
#endif

        Debug.Log("NOT UNITY_UWP");
    }
	
	// Update is called once per frame
	void Update () {
		
	}

#if !UNITY_EDITOR

    private async void InitSensor()
    {
        var mediaFrameSourceGroupList = await MediaFrameSourceGroup.FindAllAsync();
        var mediaFrameSourceGroup = mediaFrameSourceGroupList[0];
        var mediaFrameSourceInfo = mediaFrameSourceGroup.SourceInfos[0];
        var mediaCapture = new MediaCapture();
        var settings = new MediaCaptureInitializationSettings()
        {
            SourceGroup = mediaFrameSourceGroup,
            SharingMode = MediaCaptureSharingMode.SharedReadOnly,
            StreamingCaptureMode = StreamingCaptureMode.Video,
            MemoryPreference = MediaCaptureMemoryPreference.Cpu,
        };
        try
        {
			Debug.Log("Get frame");
			
            await mediaCapture.InitializeAsync(settings);
            var mediaFrameSource = mediaCapture.FrameSources[mediaFrameSourceInfo.Id];
            var mediaframereader = await mediaCapture.CreateFrameReaderAsync(mediaFrameSource, mediaFrameSource.CurrentFormat.Subtype);
            mediaframereader.FrameArrived += FrameArrived;
            await mediaframereader.StartAsync();
        }
        catch (Exception e)
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() => { Debug.Log(e); }, true);
        }
    }

    private void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        var mediaframereference = sender.TryAcquireLatestFrame();
        if (mediaframereference != null)
        {
            var videomediaframe = mediaframereference?.VideoMediaFrame;

            float depthScaleInMeters = (float)videomediaframe.DepthMediaFrame.DepthFormat.DepthScaleInMeters;

            var softwarebitmap = videomediaframe?.SoftwareBitmap;
            if (softwarebitmap != null)
            {
                // dont want convert to RGBA8
                //softwarebitmap = SoftwareBitmap.Convert(softwarebitmap, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
                int w = softwarebitmap.PixelWidth;
                int h = softwarebitmap.PixelHeight;

                // my code
                
                if (my_bytes == null)
                {
                    my_bytes = new byte[w * h * 2];
                }

                softwarebitmap.CopyToBuffer(my_bytes.AsBuffer());
                softwarebitmap.Dispose();
                UInt16 depth_at_center = BitConverter.ToUInt16(my_bytes, (w / 2) * (h / 2));

                Debug.Log("Depth at Center: " + depth_at_center);

                /*
                if (bytes==null)
                {
                    bytes = new byte[w * h * 4];
                }
                softwarebitmap.CopyToBuffer(bytes.AsBuffer());
                softwarebitmap.Dispose();
                UnityEngine.WSA.Application.InvokeOnAppThread(() => {
                    if (tex == null)
                    {
                        tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                        GetComponent<Renderer>().material.mainTexture = tex;
                    }
                
                //-
                //Test
                //Debug.Log("Red Value is" + bytes[(w/2) * (h/2) * 4]);
                //Debug.Log("Green Value is" + bytes[(w/2) * (h/2) * 4 + 1]);
                //Debug.Log("Blue Value is" + bytes[(w/2) * (h/2) * 4 + 2]);
                //Debug.Log("Alpha Value is" + bytes[(w/2) * (h/2) * 4 + 3]);
                //Debug.Log("Scale is " + depthScaleInMeters);

                //-
                    for (int i = 0; i < bytes.Length / 4; ++i)
                    {
                        byte b = bytes[i * 4];
                        bytes[i * 4 + 0] = 0;
                        bytes[i * 4 + 1] = 0;
                        bytes[i * 4 + 2] = 0;
                        bytes[i * 4 + 3] = 255;
                        if (b==0)
                        {
                            bytes[i * 4 + 0] = 128;
                        }
                        if (b==1)
                        {
                            bytes[i * 4 + 0] = 255;
                        }
                    }
                    tex.LoadRawTextureData(bytes);
                    tex.Apply();
                }, true);
                */
            }
            mediaframereference.Dispose();
        }
    }
#endif
}
