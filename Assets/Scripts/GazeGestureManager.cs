using UnityEngine;
using UnityEngine.VR.WSA.Input;
using UnityEngine.VR.WSA.WebCam;
using System.Linq;
using System.Collections.Generic;
using SharpConfig;
using System.Text;
using System.Threading;
using System.IO;
using System;

public class GazeGestureManager : MonoBehaviour
{
	public static GazeGestureManager Instance { get; private set; }

	GestureRecognizer recognizer;

	Resolution cameraResolution;

	PhotoCapture photoCaptureObject = null;

	Vector3 cameraPosition;
	Quaternion cameraRotation;

	bool _busy = false;

	public GameObject textPrefab;
	public GameObject status;
	public GameObject framePrefab;

	public string faceApiUrl = "http://faceapi.us.to/";

	void OnPhotoCaptureCreated(PhotoCapture captureObject)
	{
		photoCaptureObject = captureObject;

		cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

		CameraParameters c = new CameraParameters();
		c.hologramOpacity = 0.0f;
		c.cameraResolutionWidth = cameraResolution.width;
		c.cameraResolutionHeight = cameraResolution.height;
		c.pixelFormat = CapturePixelFormat.PNG;

		captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
	}

	private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
	{
		if (result.success)
		{
			Debug.Log("Camera ready");
			photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
		}
		else
		{
			Debug.LogError("Unable to start photo mode!");
			_busy = false;
		}
	}

	IEnumerator<object> PostToFaceAPI(byte[] imageData, Matrix4x4 cameraToWorldMatrix, Matrix4x4 pixelToCameraMatrix) {
		WWW www = new WWW(faceApiUrl, imageData);
		yield return www;
		string responseString = www.text;
		Debug.Log(responseString);
		JSONObject j = new JSONObject(responseString);
		var existing = GameObject.FindGameObjectsWithTag("faceText");

		foreach (var go in existing ) {
			Destroy(go);
		}

		existing = GameObject.FindGameObjectsWithTag("faceBounds");

		foreach (var go in existing)
		{
			Destroy(go);
		}

		if (j.list.Count == 0)
		{
			status.GetComponent<TextMesh>().text = "no faces found";
			yield break;
		}
		else
		{
			status.SetActive(false);
		}

		foreach (var result in j.list) {
			GameObject txtObject = (GameObject)Instantiate(textPrefab);
			TextMesh txtMesh = txtObject.GetComponent<TextMesh>();
			var r = result["kairos"];
			float top = -(r["top"].f / cameraResolution.height -.5f);
			float left = r["left"].f / cameraResolution.width - .5f;
			float width = r["width"].f / cameraResolution.width;
			float height = r["height"].f / cameraResolution.height;

			GameObject faceBounds = (GameObject)Instantiate(framePrefab);
			faceBounds.transform.position = cameraToWorldMatrix.MultiplyPoint3x4(pixelToCameraMatrix.MultiplyPoint3x4(new Vector3(left + width / 2, top, 0)));
			faceBounds.transform.rotation = cameraRotation;
			Vector3 scale = pixelToCameraMatrix.MultiplyPoint3x4(new Vector3(width, height, 0));
			scale.z = .1f;
			faceBounds.transform.localScale = scale;
			faceBounds.tag = "faceBounds";

			Vector3 origin = cameraToWorldMatrix.MultiplyPoint3x4(pixelToCameraMatrix.MultiplyPoint3x4(new Vector3(left + width + .1f, top, 0)));
			txtObject.transform.position = origin;
			txtObject.transform.rotation = cameraRotation;
			txtObject.tag = "faceText";
			if (j.list.Count > 1) {
				txtObject.transform.localScale /= 2;
			}

			txtMesh.text = result["text"].str;
		}
	}

	void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
	{
		if (result.success)
		{
			Debug.Log("photo captured");
			List<byte> imageBufferList = new List<byte>();
			// Copy the raw IMFMediaBuffer data into our empty byte list.
			photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);

			var cameraToWorldMatrix = new Matrix4x4();
			photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);
			
			cameraPosition = cameraToWorldMatrix.MultiplyPoint3x4(new Vector3(0,0,-1));
			cameraRotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

			Matrix4x4 projectionMatrix;
			photoCaptureFrame.TryGetProjectionMatrix(Camera.main.nearClipPlane, Camera.main.farClipPlane, out projectionMatrix);
			Matrix4x4 pixelToCameraMatrix = projectionMatrix.inverse;

			status.GetComponent<TextMesh>().text = "photo captured, processing...";
			status.transform.position = cameraPosition;
			status.transform.rotation = cameraRotation;

			StartCoroutine(PostToFaceAPI(imageBufferList.ToArray(), cameraToWorldMatrix, pixelToCameraMatrix));
		}
		photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
	}

	void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
	{
		photoCaptureObject.Dispose();
		photoCaptureObject = null;
		_busy = false;
	}

	// Use this for initialization
	void Awake()
	{
		Instance = this;

		// Set up a GestureRecognizer to detect Select gestures.
		recognizer = new GestureRecognizer();
		recognizer.TappedEvent += (source, tapCount, ray) =>
		{
			Debug.Log("tap");
			if (!_busy)
			{
				_busy = true;
				status.GetComponent<TextMesh>().text = "taking photo...";
				status.SetActive(true);
				PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
			}
		};
		recognizer.StartCapturingGestures();
		status.GetComponent<TextMesh>().text = "taking photo...";
		_busy = true;

		PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
	}
}