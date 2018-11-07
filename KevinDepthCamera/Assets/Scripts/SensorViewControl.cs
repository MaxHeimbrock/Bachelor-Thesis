/*
 * Author: Kevin Yu
 * Email: kevin.yu@tum.de
 * Date: 05. November 2018
 * 
 * Make sure the research mode of the Hololens is activated.
 * Additionally, enable "Unity C# Projects" during building the solution
 * and enable research mode inside the built solution.
 * For that, open "Package.appxmanifest" as code and add
 * xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
 * and
 * "rescap" without quotes to IgnorableNamespaces.
 * Also, add <rescap:Capability Name="perceptionSensorsExperimental" /> to the capabilities, but before the webcam capability.
*/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.WSA.Input;
using System.Linq;

#if UNITY_UWP
using System;
using System.Runtime.InteropServices;
using UnityEngine.XR.WSA;
using Windows.Perception.Spatial;
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Graphics.Imaging;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

public class SensorViewControl : MonoBehaviour
{

    public RawImage DisplayImage = null;
    [HideInInspector]
    public byte[] currentDepthData;
    [HideInInspector]
    public Matrix4x4 currentDepthPose;


    // If UseDepth is false, infrared is used instead of depth.
    // It might be that infrared and color frame reader cannot run at the same time (confirmation needed)
    private bool _useDepth = true;
    public bool UseDepth
    {
        set
        {
            _useDepth = value;

#if UNITY_UWP
            Task.Run(async () =>
            {
                if (_useDepth)
                {
                    await _infraredReader.StopAsync();
                    await _depthReader.StartAsync();
                }
                else
                {
                    await _depthReader.StopAsync();
                    await _infraredReader.StartAsync();
                }
            });
#endif
        }
        get
        {
            return _useDepth;
        }
    }

    public bool GrabColor = true;

    private GestureRecognizer gestureRecognizer;
#if UNITY_UWP
    static readonly Guid MFSampleExtension_Spatial_CameraCoordinateSystem = new Guid("9D13C82F-2199-4E67-91CD-D1A4181F2534");
    static readonly Guid MFSampleExtension_Spatial_CameraProjectionTransform = new Guid("47F9FCB5-2A02-4F26-A477-792FDF95886A");
    static readonly Guid MFSampleExtension_Spatial_CameraViewTransform = new Guid("4E251FA4-830F-4770-859A-4B8D99AA809B");
    private SpatialCoordinateSystem _unityWorldCoordinateSystem;

    private MediaFrameReader _depthReader;
    private MediaFrameReader _infraredReader;
    private UndistortionDepthImage _undistort;


    private System.Numerics.Matrix4x4 zInvert =
            new System.Numerics.Matrix4x4(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, -1, 0,
                0, 0, 0, 1
            );
#endif
    // Use this for initialization
    void Start()
    {
#if UNITY_UWP

        _unityWorldCoordinateSystem =
                Marshal.GetObjectForIUnknown(WorldManager.GetNativeISpatialCoordinateSystemPtr()) as SpatialCoordinateSystem;

        Task.Run(() =>
        {
            _undistort = new UndistortionDepthImage();
            _undistort.InitLookup(
                448, 450,
                new UndistortionDepthImage.CameraIntrinics()
                {
                    fx = 197.707225374208f,
                    fy = 201.581311460880f,
                    ppx = 226.208683510427f,
                    ppy = 225.819548268168f,
                    k1 = -0.271283991007049f,
                    k2 = 0.0806828103078386f,
                    k3 = -0.0109236654954672f,
                    p1 = 0,
                    p2 = 0
                }
            );

            InitSensor();
        });

        gestureRecognizer = new GestureRecognizer();
        gestureRecognizer.SetRecognizableGestures(GestureSettings.Tap);
        gestureRecognizer.Tapped += OnTapped;
        //gestureRecognizer.StartCapturingGestures();
#endif

    }

    private void OnTapped(TappedEventArgs obj)
    {
        UseDepth = !UseDepth;
    }


#if UNITY_UWP
    private async void InitSensor()
    {
        var mediaFrameSourceGroupList = await MediaFrameSourceGroup.FindAllAsync();
        var mediaFrameSourceGroup = mediaFrameSourceGroupList[0];

        var mediaCapture = new MediaCapture();
        var mediaCaptureColor = new MediaCapture();

        try
        {
            await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings()
            {
                SourceGroup = mediaFrameSourceGroup,
                SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            });

            var mediaFrameSourceDepth = mediaCapture.FrameSources[mediaFrameSourceGroup.SourceInfos[0].Id];
            var mediaFrameSourceInfrared = mediaCapture.FrameSources[mediaFrameSourceGroup.SourceInfos[1].Id];

            _depthReader = await mediaCapture.CreateFrameReaderAsync(mediaFrameSourceDepth, mediaFrameSourceDepth.CurrentFormat.Subtype);
            _depthReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
            _infraredReader = await mediaCapture.CreateFrameReaderAsync(mediaFrameSourceInfrared, mediaFrameSourceInfrared.CurrentFormat.Subtype);
            _infraredReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;

            _depthReader.FrameArrived += FrameArrivedDepth;
            _infraredReader.FrameArrived += FrameArrivedDepth;

            if (UseDepth)
            {
                await _depthReader.StartAsync();
            }
            else
            {
                await _infraredReader.StartAsync();
            }

            // Color image
            var sourceColor = mediaFrameSourceGroupList.SelectMany(group => group.SourceInfos)
             .LastOrDefault(
                si =>
                (si.MediaStreamType == MediaStreamType.VideoRecord) &&
                (si.SourceKind == MediaFrameSourceKind.Color) &&
                (si.VideoProfileMediaDescription.Any(
                    desc =>
                    desc.Width == 1280 && desc.Height == 720 && desc.FrameRate == 30))); // only this stream seems to be available (?)

            await mediaCaptureColor.InitializeAsync
                (new MediaCaptureInitializationSettings() {
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                    SourceGroup = sourceColor.SourceGroup,
                    StreamingCaptureMode = StreamingCaptureMode.Video });

            var mediaFrameSourceColor = mediaCaptureColor.FrameSources[sourceColor.Id];
            var colorReader = await mediaCaptureColor.CreateFrameReaderAsync(mediaFrameSourceColor, mediaFrameSourceColor.CurrentFormat.Subtype);
            colorReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
            colorReader.FrameArrived += FrameArrivedColor;
            await colorReader.StartAsync();

        }
        catch (Exception e)
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() => Debug.Log("Error InitSensor" + e.Message) , true);
        }
    }

    private bool halfLimiter = false; // only take halve of the color images to reduce processing
    private byte[] _rawBytesColor;
    private void FrameArrivedColor(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (!GrabColor) return;

        halfLimiter = !halfLimiter;
        if (halfLimiter) return;

        var mediaframereference = sender.TryAcquireLatestFrame();
        if (mediaframereference != null)
        {
            // Retrieve depth camera pose
            byte[] HoloColorPoseBuffer = new byte[16 * sizeof(float)];
            if (!RetrieveCameraPose(mediaframereference, out var HoloPose)) return;
            // Serialize pose
            for (int i = 0; i < 16; i++)
            {
                Buffer.BlockCopy(GetBytes(HoloPose[i]), 0, HoloColorPoseBuffer, i * 4, 4);
            }

            var videomediaframe = mediaframereference?.VideoMediaFrame;
            var softwarebitmap = videomediaframe?.SoftwareBitmap;

            if (softwarebitmap != null)
            {
                ResizeBitmapNV12(ref softwarebitmap, 0.5f);

                int w = softwarebitmap.PixelWidth;
                int h = softwarebitmap.PixelHeight;
                if (_rawBytesColor == null || _rawBytesColor.Length != w * h * 1.5f) // NV12 format in yuv
                {
                    _rawBytesColor = new byte[w * h + (w * h) / 2];
                }

                softwarebitmap.CopyToBuffer(_rawBytesColor.AsBuffer());
                softwarebitmap.Dispose();

                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    try
                    {
                        byte[] HoloVCamPoseBuffer = new byte[16 * sizeof(float)];

                        Matrix4x4 cameraPose = Matrix4x4.TRS(Camera.main.transform.position, Camera.main.transform.rotation, Vector3.one);
                        for (int i = 0; i < 16; i++)
                        {
                            Buffer.BlockCopy(GetBytes(cameraPose[i]), 0, HoloVCamPoseBuffer, i * 4, 4);
                        }

                        byte[] bytebuffer = new byte[HoloColorPoseBuffer.Length + HoloVCamPoseBuffer.Length + _rawBytesColor.Length];
                        Buffer.BlockCopy(HoloColorPoseBuffer, 0, bytebuffer, 0, HoloColorPoseBuffer.Length);
                        Buffer.BlockCopy(HoloVCamPoseBuffer, 0, bytebuffer, HoloColorPoseBuffer.Length, HoloVCamPoseBuffer.Length);
                        Buffer.BlockCopy(_rawBytesColor, 0, bytebuffer, HoloColorPoseBuffer.Length + HoloVCamPoseBuffer.Length, _rawBytesColor.Length);

                    }
                    catch (Exception e)
                    {
                       Debug.Log("Error FrameArrivedColor: " + e.Message);
                    }
                }, true);
            }
            mediaframereference.Dispose();
        }
    }

    private byte[] _rawBytesDepth = null;
    private void FrameArrivedDepth(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        var mediaframereference = sender.TryAcquireLatestFrame();
        if (mediaframereference != null)
        {
            // Retrieve depth camera pose
            byte[] HoloDepthPoseBuffer = new byte[16 * sizeof(float)];
            if (!RetrieveCameraPose(mediaframereference, out var depthPose)) return;
            currentDepthPose = depthPose;

            // Serialize pose
            for (int i = 0; i < 16; i++)
            {
                Buffer.BlockCopy(GetBytes(currentDepthPose[i]), 0, HoloDepthPoseBuffer, i * 4, 4);
            }

            var videomediaframe = mediaframereference?.VideoMediaFrame;
            var softwarebitmap = videomediaframe?.SoftwareBitmap;

            if (softwarebitmap != null)
            {

                int w = softwarebitmap.PixelWidth;
                int h = softwarebitmap.PixelHeight;
                if (_rawBytesDepth == null || _rawBytesDepth.Length != w * h * (UseDepth ? 2 : 1))
                {
                    _rawBytesDepth = new byte[w * h * (UseDepth ? 2 : 1)];
                }
                softwarebitmap.CopyToBuffer(_rawBytesDepth.AsBuffer());
                softwarebitmap.Dispose();
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    try
                    {
                        byte[] HoloVCamPoseBuffer = new byte[16 * sizeof(float)];

                        Matrix4x4 cameraPose = Matrix4x4.TRS(Camera.main.transform.position, Camera.main.transform.rotation, Vector3.one);
                        for (int i = 0; i < 16; i++)
                        {
                            Buffer.BlockCopy(GetBytes(cameraPose[i]), 0, HoloVCamPoseBuffer, i * 4, 4);
                        }

                        currentDepthData = UseDepth ? _undistort.UndistortDepthImage(_rawBytesDepth) : _undistort.UndistortInfraredImage(_rawBytesDepth);

                        // Serialize and pack data. This buffer can be used for TCP packages 
                        byte[] bytebuffer = new byte[HoloDepthPoseBuffer.Length + HoloVCamPoseBuffer.Length + currentDepthData.Length];
                        Buffer.BlockCopy(HoloDepthPoseBuffer, 0, bytebuffer, 0, HoloDepthPoseBuffer.Length);
                        Buffer.BlockCopy(HoloVCamPoseBuffer, 0, bytebuffer, HoloDepthPoseBuffer.Length, HoloVCamPoseBuffer.Length);
                        Buffer.BlockCopy(currentDepthData, 0, bytebuffer, HoloDepthPoseBuffer.Length + HoloVCamPoseBuffer.Length, currentDepthData.Length);

                        if (DisplayImage && DisplayImage.enabled && DisplayImage.texture)
                        {
                            Texture2D tex = DisplayImage.texture as Texture2D;
                            if (tex.width != w || tex.format != (UseDepth ? TextureFormat.R16 : TextureFormat.R8))
                                tex.Resize(w, h, UseDepth ? TextureFormat.R16 : TextureFormat.R8, false);

                            tex.LoadRawTextureData(currentDepthData);
                            tex.Apply();
                        }

                    }
                    catch (Exception e)
                    {
                        Debug.Log("Error FrameArrived: " + e.Message);
                    }
                }, true);
            }
            mediaframereference.Dispose();
        }
    }

    public void GetDepthIntrinsics(out int width, out int height, out float fx, out float fy, out float ppx, out float ppy)
    {
        width = _undistort.width;
        height = _undistort.height;
        var m = _undistort.UndistIntrinsics;
        fx = m.fx;
        fy = m.fy;
        ppx = m.ppx;
        ppy = m.ppy;
    }

    /// <summary>
    /// Get pose of the video frame in world space
    /// </summary>
    private bool RetrieveCameraPose(MediaFrameReference m, out Matrix4x4 pose)
    {
        // Retrieve pose of depth camera belonging to the frame (in world coordinates)
        pose = new Matrix4x4();
        if (m.Properties.TryGetValue(MFSampleExtension_Spatial_CameraCoordinateSystem, out var coords))
        {
            var camposition = coords as SpatialCoordinateSystem;
            var c = camposition.TryGetTransformTo(_unityWorldCoordinateSystem);

            Matrix4x4 camPose = new Matrix4x4();
            var camMat = c.Value * zInvert;
            {
                System.Numerics.Matrix4x4.Decompose(camMat, out var s, out var r, out var t);

                camPose.SetTRS(new Vector3(t.X, t.Y, t.Z), new Quaternion(r.X, r.Y, r.Z, r.W), new Vector3(s.X, s.Y, s.Z));
            }

            if (m.Properties.TryGetValue(MFSampleExtension_Spatial_CameraViewTransform, out var view))
            {
                var matrix = System.Numerics.Matrix4x4.Identity;

                var handle = GCHandle.Alloc(view as byte[], GCHandleType.Pinned);
                matrix = Marshal.PtrToStructure<System.Numerics.Matrix4x4>(handle.AddrOfPinnedObject());
                handle.Free();

                Matrix4x4 viewpose = new Matrix4x4();
                System.Numerics.Matrix4x4.Invert(matrix * zInvert, out var viewMat);
                {
                    System.Numerics.Matrix4x4.Decompose(viewMat, out var s, out var r, out var t);

                    viewpose.SetTRS(new Vector3(t.X, t.Y, t.Z), new Quaternion(r.X, r.Y, r.Z, r.W), new Vector3(s.X, s.Y, s.Z));
                }
                pose = camPose * viewpose;

                return true;
            }

        }

        return false;
    }

    private void ResizeBitmapNV12(ref SoftwareBitmap image, float scalefactor)
    {
        if (image.BitmapPixelFormat != BitmapPixelFormat.Nv12) return;

        int swbWidth = image.PixelWidth;
        int swbHeight = image.PixelHeight;

        byte[] buffer = new byte[swbHeight * swbWidth + swbHeight * swbWidth / 2];
        image.CopyToBuffer(buffer.AsBuffer());

        int width = (int)(image.PixelWidth * scalefactor);
        int height = (int)(image.PixelHeight * scalefactor);
        byte[] newBuffer = new byte[width * height + width * height / 2];

        int UVOffset = width * height;
        int swbUVOffset = swbWidth * swbHeight;
        for (int y = 0; y < height; y++)
        {
            int stepY = (int)(y / scalefactor + 0.5f);
            for (int x = 0; x < width; x++)
            {
                int stepX = (int)(x / scalefactor + 0.5f);

                newBuffer[y * width + x] = buffer[stepY * swbWidth + stepX];

                int swbUVIndex = swbUVOffset + (stepY / 2 * swbWidth / 2 + stepX / 2) * 2;
                int UVIndex = UVOffset + (y / 2 * width / 2 + x / 2) * 2;
                newBuffer[UVIndex] = buffer[swbUVIndex];
                newBuffer[UVIndex + 1] = buffer[swbUVIndex + 1];
            }
        }

        image.Dispose();
        image = SoftwareBitmap.CreateCopyFromBuffer(newBuffer.AsBuffer(), BitmapPixelFormat.Nv12, width, height);
    }


    /// <summary>
    /// Faster conversion (compared to BitConverter) from float to byte array.
    /// Requires "csc.rsp", "gmcs.rsp", "smcs.rsp" in Assets folder, each with content "-unsafe"
    /// </summary>
    [System.Security.SecuritySafeCritical]
    public unsafe static byte[] GetBytes(float value)
    {
        int rawBits = *(int*)&value;
        byte[] bytes = new byte[4];
        fixed (byte* b = bytes)
            *((int*)b) = rawBits;
        return bytes;
    }
#endif
}

#if UNITY_UWP
class UndistortionDepthImage
{
    public struct CameraIntrinics
    {
        public float fx, fy, ppx, ppy;
        public float k1, k2, k3;
        public float p1, p2;
    };

    public int width;
    public int height;
    public CameraIntrinics UndistIntrinsics;

    private int[,] xd;
    private int[,] yd;
    private float[,] depthCorrection;
    private float[,] XCorrection;
    private float[,] YCorrection;


    public void InitLookup(int width, int height, CameraIntrinics rawImageParam)
    {
        UndistIntrinsics = rawImageParam;

        this.width = width;
        this.height = height;

        xd = new int[width, height];
        yd = new int[width, height];

        depthCorrection = new float[width, height];
        XCorrection = new float[width, height];
        YCorrection = new float[width, height];
        float betaX = Mathf.Atan2(width, 2 * rawImageParam.fx);   // half of field of view
        float betaY = Mathf.Atan2(height, 2 * rawImageParam.fy);

        for (int xCounter = 0; xCounter < width; xCounter++)
        {
            for (int yCounter = 0; yCounter < height; yCounter++)
            {

                float x = ((float)xCounter - rawImageParam.ppx) / rawImageParam.fx;
                float y = ((float)yCounter - rawImageParam.ppy) / rawImageParam.fy;

                float r2 = x * x + y * y;

                //Distortion
                float dr =
                    rawImageParam.k1 * r2 +
                    rawImageParam.k2 * r2 * r2 +
                    rawImageParam.k3 * r2 * r2 * r2;

                //Tangents
                float dtx = 2 * rawImageParam.p1 * x * y + rawImageParam.p2 * (r2 + 2 * x * x);
                float dty = rawImageParam.p1 * (r2 + 2 * y * y) + 2 * rawImageParam.p2 * x * y;

                x = x + dr * x + dtx;
                y = y + dr * y + dty;

                xd[xCounter, yCounter] = (int)(x * rawImageParam.fx + rawImageParam.ppx + 0.5f);
                yd[xCounter, yCounter] = this.height - (int)(y * rawImageParam.fy + rawImageParam.ppy + 0.5f); // Also flip image on horizontal axis

                // Calculating depth correction
                float X = (1.0f * xCounter - rawImageParam.ppx) / rawImageParam.fx;
                float Y = (1.0f * yCounter - rawImageParam.ppy) / rawImageParam.fy;

                // Xf and Yf refer to the maximum value for a point to be still inside the camera frustrum
                float Xf = Mathf.Sin(betaX) / Mathf.Sin(0.5f * (float)Math.PI - betaX);
                float Yf = Mathf.Sin(betaY) / Mathf.Sin(0.5f * (float)Math.PI - betaY);

                // normalized distance to center, 2D projection
                float Xr = X * Xf;
                float Yr = Y * Yf;

                // Diagonal 3D distance in a normalized space with depth 1
                float Xd = Mathf.Sqrt(Xr * Xr + 1);
                float Yd = Mathf.Sqrt(Yr * Yr + 1);

                // Angle corresponding to the normalized distance
                float betaXr = Mathf.Acos((Xr * Xr - Xd * Xd - 1) / (-2 * Xd));
                float betaYr = Mathf.Acos((Yr * Yr - Yd * Yd - 1) / (-2 * Yd));

                // Xc and Yc refer to the 3D point in the projective space of the depth camera
                float Xc = Mathf.Sign(X) * Mathf.Sin(betaXr) / Mathf.Sin(0.5f * (float)Math.PI - betaXr);
                float Yc = Mathf.Sign(Y) * Mathf.Sin(betaYr) / Mathf.Sin(0.5f * (float)Math.PI - betaYr);

                Xc /= Xf;
                Yc /= Xf;

                depthCorrection[xCounter, yCounter] = 1 + Xc * Xc + Yc * Yc;
                XCorrection[xCounter, yCounter] = Xc;
                YCorrection[xCounter, yCounter] = Yc;
            }
        }
    }

    public byte[] UndistortDepthImage(byte[] img)
    {
        byte[] newImg = new byte[width * height * 2];

        if (newImg.Length != img.Length) throw new Exception("UndistortDepthImage: The format of the input image is not R16");

        int numThreads = 3;
        int range = height / (numThreads + 1);

        int residual = height - (range * numThreads);

        Task[] tasks = new Task[numThreads];

        for (int taskIndex = 0; taskIndex < numThreads; taskIndex++)
        {
            tasks[taskIndex] = Task.Factory.StartNew((taskStart) =>
            {
                int myIndex = (int)taskStart;
                UndistortPartial(img, ref newImg, myIndex * range, range);
            }, taskIndex);
        }
        UndistortPartial(img, ref newImg, numThreads * range, residual);

        Task.WaitAll(tasks);

        return newImg;
    }

    private void UndistortPartial(byte[] buffer, ref byte[] output, int startIndex, int range)
    {

        int endIndex = startIndex + range;
        for (int y = startIndex; y < endIndex; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (yd[x, y] * width + xd[x, y]) * 2;

                // Convert bytes to depth (short)
                byte b0 = buffer[index];
                byte b1 = buffer[index + 1];
                float fvalue = ToShort(b0, b1);

                if (depthCorrection[x, y] != 0)
                {
                    if (fvalue > 3500) // Out of range values are set to 0
                    {
                        fvalue = 0;
                    }
                    else
                    {
                        fvalue /= depthCorrection[x, y];
                        float Xc = XCorrection[x, y] * fvalue;
                        float Yc = YCorrection[x, y] * fvalue;

                        fvalue = Mathf.Sqrt(Xc * Xc + Yc * Yc + fvalue * fvalue);
                    }
                }

                ushort value = (ushort)(fvalue < 0 ? 0 : fvalue);
                // Convert depth back to bytes
                FromShort(
                    value,
                    out output[(y * width + x) * 2],
                    out output[(y * width + x) * 2 + 1]);
            }
        }
    }

    public byte[] UndistortInfraredImage(byte[] img)
    {

        byte[] newImg = new byte[width * height];
        if (newImg.Length != img.Length) throw new Exception("UndistortDepthImage: The format of the input image is not R8");

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {

                int index = yd[x, y] * width + xd[x, y];
                byte b0 = img[index];
                newImg[y * width + x] = b0;
            }
        }

        return newImg;
    }

    ushort ToShort(byte byte1, byte byte2)
    {
        return (ushort)((byte2 << 8) + byte1);
    }

    void FromShort(ushort number, out byte byte1, out byte byte2)
    {
        byte2 = (byte)(number >> 8);
        byte1 = (byte)(number & 255);
    }
}

#endif