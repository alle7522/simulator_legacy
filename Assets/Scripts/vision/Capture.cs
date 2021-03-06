using System;
using UnityEngine;
using System.Collections;
using System.Threading;
using System.IO;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class Capture : MonoBehaviour
{
    [DllImport ("libSharedImage")]
    unsafe private static extern int UpdateShared(string name, int rows, int cols, IntPtr buf);
    [DllImport ("libSharedImage")]
    unsafe private static extern int ShutdownShared(string name);

    Camera cam;
    int fps;
    [SerializeField]
    public int width;
    [SerializeField]
    public int height;
    private RenderTexture rendertex;
    private Texture2D texture;

    void Start()
    {
        this.width = 500;
        this.height = 500;
        this.fps = 5;
        cam = GetComponent<Camera>();
        rendertex = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
		rendertex.filterMode = FilterMode.Bilinear;
		rendertex.antiAliasing = 2;
        cam.targetTexture = rendertex;
        texture = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.RGB24, false);
        InvokeRepeating("UpdateCaller", 0, 1.0f / fps);
    }

    void UpdateCaller()
    {
        StartCoroutine("UpdateCamera");
    }

    void UpdateCamera()
    {
        // set target textures
        cam.targetTexture = rendertex;
        RenderTexture.active = cam.targetTexture;

        // disable camera and render
        cam.enabled = false;
        cam.Render();

        // download gpu framebuffer
        texture.ReadPixels(new Rect(0, 0, rendertex.width, rendertex.height), 0, 0);
        texture.Apply();

        // convert to opencv mat memory layout
        byte[] data = texture.GetRawTextureData();
        IntPtr ptr = Marshal.AllocHGlobal(data.Length);
        Marshal.Copy(data, 0, ptr, data.Length);
        UpdateShared(name, width, height, ptr);
    }

    void OnDestroy()
    {
        ShutdownShared(name);
    }
}
