using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if !UNITY_EDITOR

using Windows.Media.Capture.Frames;
using System;
using System.Linq;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Graphics.Imaging;

#endif

public class GetDepthImage : MonoBehaviour {

#if !UNITY_EDITOR
    private MediaCapture mediaCapture;
    private MediaFrameReader mediaFrameReader;
    public SoftwareBitmap depth_image;
#endif

    public object depth_image_lock;

    public GetDepthImage()
    {
        depth_image_lock = new object();
    }

    // Use this for initialization
    void Start () {
    #if !UNITY_EDITOR
        initAsync();
    #endif
    }
	
	// Update is called once per frame
	void Update () {
		
	}

#if !UNITY_EDITOR


    public async void initAsync()
    {
        var allGroups = await MediaFrameSourceGroup.FindAllAsync();
        var eligibleGroups = allGroups.Select(g => new
        {
            Group = g,

            // For each source kind, find the source which offers that kind of media frame,
            // or null if there is no such source.
            SourceInfos = new MediaFrameSourceInfo[]
            {
        g.SourceInfos.FirstOrDefault(info => info.SourceKind == MediaFrameSourceKind.Color),
        g.SourceInfos.FirstOrDefault(info => info.SourceKind == MediaFrameSourceKind.Infrared),
        g.SourceInfos.FirstOrDefault(info => info.SourceKind == MediaFrameSourceKind.Depth),
            }
        }).Where(g => g.SourceInfos.Any(info => info != null)).ToList();

        if (eligibleGroups.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("No source group with color, depth or infrared found.");
            return;
        }

        var selectedGroupIndex = 0; // Select the first eligible group
        MediaFrameSourceGroup selectedGroup = eligibleGroups[selectedGroupIndex].Group;
        MediaFrameSourceInfo colorSourceInfo = eligibleGroups[selectedGroupIndex].SourceInfos[0];
        MediaFrameSourceInfo infraredSourceInfo = eligibleGroups[selectedGroupIndex].SourceInfos[1];
        MediaFrameSourceInfo depthSourceInfo = eligibleGroups[selectedGroupIndex].SourceInfos[2];

        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        mediaCapture = new MediaCapture();
        var settings = new MediaCaptureInitializationSettings()
        {
            SourceGroup = selectedGroup,
            SharingMode = MediaCaptureSharingMode.ExclusiveControl,
            MemoryPreference = MediaCaptureMemoryPreference.Cpu,
            StreamingCaptureMode = StreamingCaptureMode.Video
        };
        try
        {
            await mediaCapture.InitializeAsync(settings);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("MediaCapture initialization failed: " + ex.Message);
            return;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////

        var depthFrameSource = mediaCapture.FrameSources[depthSourceInfo.Id];
        var preferredFormat = depthFrameSource.SupportedFormats.Where(format =>
        {
            return format.Subtype == MediaEncodingSubtypes.D16;

        }).FirstOrDefault();

        if (preferredFormat == null)
        {
            // Our desired format is not supported
            return;
        }

        await depthFrameSource.SetFormatAsync(preferredFormat);

        mediaFrameReader = await mediaCapture.CreateFrameReaderAsync(depthFrameSource, MediaEncodingSubtypes.Argb32);
        mediaFrameReader.FrameArrived += DepthFrameReader_FrameArrived;
        await mediaFrameReader.StartAsync();
    }

    private void DepthFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        var mediaFrameReference = sender.TryAcquireLatestFrame();
        if (mediaFrameReference == null)
        {
            return;
        }
        var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
        var softwareBitmap = videoMediaFrame?.SoftwareBitmap;

        if (softwareBitmap == null)
        {
            mediaFrameReference.Dispose();
            return;
        }

        if (depth_image == null)
        {
            depth_image = new SoftwareBitmap(BitmapPixelFormat.Gray16,
                                             softwareBitmap.PixelWidth,
                                             softwareBitmap.PixelHeight,
                                             BitmapAlphaMode.Ignore);
        }


        lock (depth_image_lock)
        {
            softwareBitmap.CopyTo(depth_image);
        }

        mediaFrameReference.Dispose();
        return;
    }

#endif

}
