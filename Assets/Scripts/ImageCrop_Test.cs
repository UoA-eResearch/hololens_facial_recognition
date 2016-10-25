using UnityEngine;
using System.Collections;
using System;

public class ImageCrop_Test : MonoBehaviour {

	void Start()
	{
		var bytes = System.IO.File.ReadAllBytes("example.jpg");
		var source = new Texture2D(0,0);
		source.LoadImage(bytes);
		var dest = new Texture2D(50, 50);
		dest.SetPixels(source.GetPixels(200, 200, 50, 50));
		var cropped = dest.EncodeToJPG();
		Debug.Log(cropped);
		System.IO.File.WriteAllBytes("cropped.jpg", cropped);
	}
}
