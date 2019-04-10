//  
// Copyright (c) 2017 Vulcan, Inc. All rights reserved.  
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.
//

#if !UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Media.Effects;
using Windows.Perception.Spatial;
using Windows.Foundation.Collections;
using Windows.Foundation;
using System.Diagnostics;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.UI.Core;
using Windows.Media.Core;
using Windows.Media;
using Windows.Media.Devices;
using System.Threading;

[ComImport]
[Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
unsafe interface IMemoryBufferByteAccess{
    void GetBuffer(out byte* buffer, out uint capacity);
}

namespace HoloLensCameraStream{  // HoloLensCameraStream
    /// <summary>
    /// Called when a VideoCapture resource has been created.
    /// If the instance failed to be created, the instance returned will be null.
    /// </summary>
    /// <param name="captureObject">The VideoCapture instance.</param>
    public delegate void OnVideoCaptureResourceCreatedCallback(VideoCapture captureObject);

    /// <summary>
    /// Called when the web camera begins streaming video.
    /// </summary>
    /// <param name="result">Indicates whether or not video recording started successfully.</param>
    public delegate void OnVideoModeStartedCallback(VideoCaptureResult result);

    /// <summary>
    /// This is called every time there is a new frame sample available.
    /// See VideoCapture.FrameSampleAcquired and the VideoCaptureSample class for more information.
    /// </summary>
    /// <param name="videoCaptureSample">The recently captured frame sample.
    /// It contains methods for accessing the bitmap, as well as supporting information
    /// such as transform and projection matrices.</param>
    public delegate void FrameSampleAcquiredCallback(VideoCaptureSample videoCaptureSample);

    /// <summary>
    /// Called when video mode has been stopped.
    /// </summary>
    /// <param name="result">Indicates whether or not video mode was successfully deactivated.</param>
    public delegate void OnVideoModeStoppedCallback(VideoCaptureResult result);

    /// <summary>
    /// Streams video from the camera and makes the buffer available for reading.
    /// </summary>
    public sealed class VideoCapture{
        /// <summary>
        /// Note: This function is not yet implemented. Help us out on GitHub!
        /// There is an instance method on VideoCapture called GetSupportedResolutions().
        /// Please use that until we can get this method working.
        /// </summary>
        public static IEnumerable<Resolution> SupportedResolutions{
            get{
                throw new NotImplementedException("Please use the instance method VideoCapture.GetSupportedResolutions() for now.");
            }
        }

        /// <summary>
        /// Returns the supported frame rates at which a video can be recorded given a resolution.
        /// Use VideoCapture.SupportedResolutions to get the supported web camera recording resolutions.
        /// </summary>
        /// <param name="resolution">A recording resolution.</param>
        /// <returns>The frame rates at which the video can be recorded.</returns>
        public static IEnumerable<float> SupportedFrameRatesForResolution(Resolution resolution){
            throw new NotImplementedException();
        }

        /// <summary>
        /// This is called every time there is a new frame sample available.
        /// You must properly initialize the VideoCapture object, including calling StartVideoModeAsync()
        /// before this event will begin firing.
        /// 
        /// You should not subscribe to FrameSampleAcquired if you do not need access to most
        /// of the video frame samples for your application (for instance, if you are doing image detection once per second),
        /// because there is significant memory management overhead to processing every frame.
        /// Instead, you can call RequestNextFrameSample() which will respond with the next available sample only.
        /// 
        /// See the VideoFrameSample class for more information about dealing with the memory
        /// complications of the BitmapBuffer.
        /// </summary>
        public event FrameSampleAcquiredCallback FrameSampleAcquired;



        internal SpatialCoordinateSystem worldOrigin { get; private set; }
        public IntPtr WorldOriginPtr{
            set{
                worldOrigin = (SpatialCoordinateSystem)Marshal.GetObjectForIUnknown(value);
            }
        }

        static readonly MediaStreamType STREAM_TYPE = MediaStreamType.VideoRecord;
        static readonly Guid ROTATION_KEY = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");

        MediaCapture     _mediaCapture;
        MediaCapture     _mediaCapturePreview;
        MediaFrameReader _mediaFrameReaderColor;
        MediaFrameReader _mediaFrameReaderDepth;
        MediaFrameReader _mediaFrameReaderInfrared;

        public SoftwareBitmap infrared_image;
        public object infrared_image_lock ;

        public SoftwareBitmap color_image;
        public object color_image_lock ;

        public SoftwareBitmap depth_image;
        public object depth_image_lock ;

        //================================================================================================================================
        public VideoCapture(){
            infrared_image_lock = new object();
            color_image_lock = new object();
            depth_image_lock = new object();
        }

        //================================================================================================================================
        /// <summary>
        /// Asynchronously creates an instance of a VideoCapture object that can be used to stream video frames from the camera to memory.
        /// If the instance failed to be created, the instance returned will be null. Also, holograms will not appear in the video.
        /// </summary>
        /// <param name="onCreatedCallback">This callback will be invoked when the VideoCapture instance is created and ready to be used.</param>
        public async void CreateAsync(  MediaStreamType stream_type, 
                                        MediaFrameSourceKind frame_source_kind, 
                                        string encoding_type){


            var allGroups = await MediaFrameSourceGroup.FindAllAsync();
            int i = allGroups.Count;
            foreach(var group in allGroups ){
                var n = group.SourceInfos.Count;
            }

            var selected_group = allGroups[0];

            // --------------- Create CameraRecord stream reader ------------------------
            // Create a Media Capture instance using the frame provider
            _mediaCapture = new MediaCapture();
            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings(){
                SourceGroup = allGroups[0],
                SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                StreamingCaptureMode = StreamingCaptureMode.Video
            };
            try{
                await _mediaCapture.InitializeAsync(settings);
            }catch (Exception ex){
                System.Diagnostics.Debug.WriteLine("MediaCapture initialization failed: " + ex.Message);
                _mediaCapture.Dispose();
                _mediaCapture = null;
                return;
            }
            
            // Now go through the group and look which FrameSource would be the best
            MediaFrameFormat  preferredFormatColor    = null;
            MediaFrameFormat  preferredFormatDepth    = null;
            MediaFrameFormat  preferredFormatInfrared = null;

            MediaFrameSource preferredFrameSourceColor    = null;
            MediaFrameSource preferredFrameSourceDepth    = null;
            MediaFrameSource preferredFrameSourceInfrared = null;

            foreach(var frame_source_pair in _mediaCapture.FrameSources){
                var frame_source_name = frame_source_pair.Key;
                var frame_source      = frame_source_pair.Value;
                if( frame_source.Info.SourceKind  == MediaFrameSourceKind.Depth  ){
                       foreach(var format in frame_source.SupportedFormats){
                            preferredFrameSourceDepth = frame_source;
                            preferredFormatDepth = format;
                       }
                }
                if( frame_source.Info.SourceKind  == MediaFrameSourceKind.Infrared  ){
                       foreach(var format in frame_source.SupportedFormats){
                            preferredFrameSourceInfrared = frame_source;
                            preferredFormatInfrared = format;
                       }
                }
            }

            // -------------------- INFRARED --------------------
            /*
            // Set the preferred format for the frame source
            // configure the source with the desired format
            await preferredFrameSourceInfrared.SetFormatAsync(preferredFormatInfrared);
            // Create a frame reader for the frame source
            _mediaFrameReaderInfrared= await _mediaCapture.CreateFrameReaderAsync(preferredFrameSourceInfrared, MediaEncodingSubtypes.L8);
            _mediaFrameReaderInfrared.FrameArrived += InfraredFrameReader_FrameArrived;
            await _mediaFrameReaderInfrared.StartAsync();
            */

            /*            
            // -------------------- COLOR --------------------
            // Set the preferred format for the frame source
            // configure the source with the desired format
            await preferredFrameSourceColor.SetFormatAsync(preferredFormatColor);
            // Create a frame reader for the frame source
            _mediaFrameReaderColor = await _mediaCapture.CreateFrameReaderAsync(preferredFrameSourceColor, MediaEncodingSubtypes.Argb32);
            _mediaFrameReaderColor.FrameArrived += ColorFrameReader_FrameArrived;
            await _mediaFrameReaderColor.StartAsync();
            */

            // -------------------- DEPTH --------------------
            // Set the preferred format for the frame source
            // configure the source with the desired format
            await preferredFrameSourceDepth.SetFormatAsync(preferredFormatDepth);   //NULLPOINTER
            // Create a frame reader for the frame source
            _mediaFrameReaderDepth = await _mediaCapture.CreateFrameReaderAsync(preferredFrameSourceDepth, MediaEncodingSubtypes.D16);
            _mediaFrameReaderDepth.FrameArrived += DepthFrameReader_FrameArrived;
            await _mediaFrameReaderDepth.StartAsync();
        }


        //================================================================================================================================
        private void InfraredFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args){
            var mediaFrameReference = sender.TryAcquireLatestFrame();
            if(mediaFrameReference==null){
                return;
            }
            mediaFrameReference.Dispose();
        }

        //================================================================================================================================
        private void ColorFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args){
            var mediaFrameReference = sender.TryAcquireLatestFrame();
            if(mediaFrameReference==null){
                return;
            }
            var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
            var softwareBitmap = videoMediaFrame?.SoftwareBitmap;
    
            if(softwareBitmap==null){
                mediaFrameReference.Dispose();
                return;
            }

            if (softwareBitmap.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8 ||
                softwareBitmap.BitmapAlphaMode != Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied){
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
            }

            if (color_image==null){
                color_image = new SoftwareBitmap(BitmapPixelFormat.Rgba8,
                                                 softwareBitmap.PixelWidth,
                                                 softwareBitmap.PixelHeight,
                                                 BitmapAlphaMode.Premultiplied);
            }


            lock(color_image_lock){
                softwareBitmap.CopyTo(color_image);
            }            

            mediaFrameReference.Dispose();
            return;
        }

        //================================================================================================================================
        private void DepthFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args){
            var mediaFrameReference = sender.TryAcquireLatestFrame();
            if(mediaFrameReference==null){
                return;
            }
            var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
            var softwareBitmap = videoMediaFrame?.SoftwareBitmap;
    
            if(softwareBitmap==null){
                mediaFrameReference.Dispose();
                return;
            }

            if (depth_image==null){
                depth_image = new SoftwareBitmap(BitmapPixelFormat.Gray16,
                                                 softwareBitmap.PixelWidth,
                                                 softwareBitmap.PixelHeight,
                                                 BitmapAlphaMode.Ignore);
            }


            lock(depth_image_lock){
                softwareBitmap.CopyTo(depth_image);
            }            

            mediaFrameReference.Dispose();
            return;
        }

        //===============================================================================================
        public unsafe int get_color_resolution(ref int pixelWidth, ref int pixelHeight){
            if(color_image==null){
                return -1;
            }
            pixelWidth  = color_image.PixelWidth;
            pixelHeight = color_image.PixelHeight;
            return 0;
        }

        //===============================================================================================
        public unsafe int get_depth_resolution(ref int pixelWidth, ref int pixelHeight){
            if(depth_image==null){
                return -1;
            }
            pixelWidth= depth_image.PixelWidth;
            pixelHeight= depth_image.PixelHeight;
            return 0;
        }

        //===============================================================================================
        public unsafe void get_depth_image(byte[] image){
            if(depth_image==null){
                return;
            }

            lock( depth_image_lock ){
                using(var input = depth_image.LockBuffer(BitmapBufferAccessMode.Read)){
                    int pixelWidth  = depth_image.PixelWidth;
                    int pixelHeight = depth_image.PixelHeight;
                    using (var inputReference = input.CreateReference()){
                        // Get input and output byte access buffers.
                        byte* inputBytes;
                        uint inputCapacity;
                        ((IMemoryBufferByteAccess) inputReference).GetBuffer(out inputBytes, out inputCapacity);
                        byte* inputRow = (byte*) inputBytes;
                        // Iterate over all pixels and store converted value.
                        for (int y = 0; y < pixelWidth*pixelHeight; y++){
                            ushort dist = (ushort) (256*inputRow[2*y+1] + inputRow[2*y]);
                            image[2*y]   = (byte) (dist/256/4);
                            image[2*y+1] = (byte) (dist/4);
                        }

                    }   
                }
            }
            return;
        }


        //===============================================================================================
        public unsafe void get_color_image(byte[] image){
            if(color_image==null){
                return;
            }

            lock( color_image_lock ){
                using(var input = color_image.LockBuffer(BitmapBufferAccessMode.Read)){

                    // Get stride values to calculate buffer position for a given pixel x and y position.
                    //int inputStride = input.GetPlaneDescription(0).Stride;
                    int pixelWidth  = color_image.PixelWidth;
                    int pixelHeight = color_image.PixelHeight;
                    using (var inputReference = input.CreateReference()){
                        // Get input and output byte access buffers.
                        byte* inputBytes;
                        uint inputCapacity;
                        ((IMemoryBufferByteAccess) inputReference).GetBuffer(out inputBytes, out inputCapacity);
                        // Iterate over all pixels and store converted value.
                        for (int y = 0; y < pixelWidth*pixelHeight; y++){
                            // Map invalid depth values to transparent pixels.
                            // This happens when depth information cannot be calculated, e.g. when objects are too close.
                            image[4*y + 0] = inputBytes[4*y+0];
                            image[4*y + 1] = inputBytes[4*y+1];
                            image[4*y + 2] = inputBytes[4*y+2];
                            image[4*y + 3] = inputBytes[4*y+3];
                        }
                    }   
                }
            }
            return;
        }


        //================================================================================================================================
        /// <summary>
        /// Dispose must be called to shutdown the PhotoCapture instance.
        /// 
        /// If your VideoCapture instance successfully called VideoCapture.StartVideoModeAsync,
        /// you must make sure that you call VideoCapture.StopVideoModeAsync before disposing your VideoCapture instance.
        /// </summary>
        public async void Dispose(){
            await _mediaFrameReaderColor.StopAsync();
            _mediaFrameReaderColor.FrameArrived -= ColorFrameReader_FrameArrived;
            
            await _mediaFrameReaderDepth.StopAsync();
            _mediaFrameReaderDepth.FrameArrived -= DepthFrameReader_FrameArrived;

            if(_mediaCapture != null){
                _mediaCapture?.Dispose();
            }
        }

        //================================================================================================================================
        VideoEncodingProperties GetVideoEncodingPropertiesForCameraParams(CameraParameters cameraParams){

            var allPropertySets = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(STREAM_TYPE).Select((x) => x as VideoEncodingProperties)
                .Where((x) =>
            {
                if (x == null) return false;
                if (x.FrameRate.Denominator == 0) return false;

                double calculatedFrameRate = (double)x.FrameRate.Numerator / (double)x.FrameRate.Denominator;
                
                return
                x.Width == (uint)cameraParams.cameraResolutionWidth &&
                x.Height == (uint)cameraParams.cameraResolutionHeight &&
                (int)Math.Round(calculatedFrameRate) == cameraParams.frameRate;
            }); //Returns IEnumerable<VideoEncodingProperties>
 

            if (allPropertySets.Count() == 0){
                throw new Exception("Could not find an encoding property set that matches the given camera parameters.");
            }
            
            var chosenPropertySet = allPropertySets.FirstOrDefault();
            return chosenPropertySet;
        }

        //================================================================================================================================
        static bool IsColorVideo(MediaFrameSourceInfo sourceInfo){
            //TODO: Determine whether 'VideoPreview' or 'VideoRecord' is the appropriate type. What's the difference?
            return (sourceInfo.MediaStreamType == STREAM_TYPE &&
                sourceInfo.SourceKind == MediaFrameSourceKind.Color);
        }

        //================================================================================================================================
        static bool IsDepthVideo(MediaFrameSourceInfo sourceInfo){
            //TODO: Determine whether 'VideoPreview' or 'VideoRecord' is the appropriate type. What's the difference?
            return (sourceInfo.MediaStreamType == STREAM_TYPE &&
                sourceInfo.SourceKind == MediaFrameSourceKind.Depth);
        }

        //================================================================================================================================
        static bool IsInfraredVideo(MediaFrameSourceInfo sourceInfo){
            //TODO: Determine whether 'VideoPreview' or 'VideoRecord' is the appropriate type. What's the difference?
            return (sourceInfo.MediaStreamType == STREAM_TYPE &&
                sourceInfo.SourceKind == MediaFrameSourceKind.Infrared);
        }
        //================================================================================================================================
        static string ConvertCapturePixelFormatToMediaEncodingSubtype(CapturePixelFormat format){
            switch (format){
                case CapturePixelFormat.BGRA32:
                    return MediaEncodingSubtypes.Bgra8;
                case CapturePixelFormat.D16:
                    return MediaEncodingSubtypes.D16;
                case CapturePixelFormat.NV12:
                    return MediaEncodingSubtypes.Nv12;
                case CapturePixelFormat.JPEG:
                    return MediaEncodingSubtypes.Jpeg;
                case CapturePixelFormat.PNG:
                    return MediaEncodingSubtypes.Png;
                default:
                    return MediaEncodingSubtypes.Bgra8;
                
            }
        }
    }

    //================================================================================================================================
	//	from https://forums.hololens.com/discussion/2009/mixedrealitycapture
	public class VideoMRCSettings : IVideoEffectDefinition{
        public string ActivatableClassId{
            get{
                return "Windows.Media.MixedRealityCapture.MixedRealityCaptureVideoEffect";
            }
        }

        public IPropertySet Properties{
            get; private set;
        }
        
        public VideoMRCSettings(bool HologramCompositionEnabled, bool VideoStabilizationEnabled, int VideoStabilizationBufferLength, float GlobalOpacityCoefficient){
            Properties = (IPropertySet)new PropertySet();
            Properties.Add("HologramCompositionEnabled", HologramCompositionEnabled);
            Properties.Add("VideoStabilizationEnabled", VideoStabilizationEnabled);
            Properties.Add("VideoStabilizationBufferLength", VideoStabilizationBufferLength);
            Properties.Add("GlobalOpacityCoefficient", GlobalOpacityCoefficient);
        }
    }   
}

#endif