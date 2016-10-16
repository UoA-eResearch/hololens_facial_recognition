using UnityEngine;
using UnityEngine.VR.WSA.Input;
using UnityEngine.VR.WSA.WebCam;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;

public class GazeGestureManager : MonoBehaviour
{
	public static GazeGestureManager Instance { get; private set; }

	GestureRecognizer recognizer;

	Resolution cameraResolution;

	PhotoCapture photoCaptureObject = null;

	Vector3 cameraPosition;
	Quaternion cameraRotation;

	public GameObject textPrefab;
	public GameObject status;
	public GameObject framePrefab;

	void OnPhotoCaptureCreated(PhotoCapture captureObject)
	{
		photoCaptureObject = captureObject;

		cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

		CameraParameters c = new CameraParameters();
		c.hologramOpacity = 0.0f;
		c.cameraResolutionWidth = cameraResolution.width;
		c.cameraResolutionHeight = cameraResolution.height;
		c.pixelFormat = CapturePixelFormat.PNG;

		captureObject.StartPhotoModeAsync(c, false, OnPhotoModeStarted);
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
		}
	}

	IEnumerator<object> PostToFaceAPI(byte[] imageData, Matrix4x4 cameraToWorldMatrix) {

		var url = "https://api.projectoxford.ai/face/v1.0/detect?returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses";
		var headers = new Dictionary<string, string>() {
			{ "Ocp-Apim-Subscription-Key", "54a11f7e7e3047f481f8e285a7ce5059" },
			{ "Content-Type", "application/octet-stream" }
		};

		WWW www = new WWW(url, imageData, headers);
		yield return www;
		string responseString = www.text;
		
		JSONObject j = new JSONObject(responseString);
		Debug.Log(j);
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
		}
		else
		{
			status.SetActive(false);
		}

		foreach (var result in j.list) {
			GameObject txtObject = (GameObject)Instantiate(textPrefab);
			TextMesh txtMesh = txtObject.GetComponent<TextMesh>();
			var a = result.GetField("faceAttributes");
			var f = a.GetField("facialHair");
			var p = result.GetField("faceRectangle");
			var top = p.GetField("top").f / cameraResolution.height -.5f;
			var left = p.GetField("left").f / cameraResolution.width - .5f;
			var width = p.GetField("width").f / cameraResolution.width;
			var height = p.GetField("height").f / cameraResolution.height;

			GameObject faceBounds = (GameObject)Instantiate(framePrefab);
			faceBounds.transform.position = cameraToWorldMatrix.MultiplyPoint3x4(new Vector3(left + width/2, top + height/2, -1));
			faceBounds.transform.rotation = cameraRotation;
			faceBounds.transform.localScale = new Vector3(width, height, .1f);
			faceBounds.tag = "faceBounds";

			Vector3 origin = cameraToWorldMatrix.MultiplyPoint3x4(new Vector3(left + width, top, -1));
			txtObject.transform.position = origin;
			txtObject.transform.rotation = cameraRotation;
			txtObject.tag = "faceText";

			txtMesh.text = string.Format("Gender: {0}\nAge: {1}\nMoustache: {2}\nBeard: {3}\nSideburns: {4}\nGlasses: {5}\nSmile: {6}", a.GetField("gender").str, a.GetField("age"), f.GetField("moustache"), f.GetField("beard"), f.GetField("sideburns"), a.GetField("glasses").str, a.GetField("smile"));
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

			status.GetComponent<TextMesh>().text = "photo captured, processing...";
			status.transform.position = cameraPosition;
			status.transform.rotation = cameraRotation;

			StartCoroutine(PostToFaceAPI(imageBufferList.ToArray(), cameraToWorldMatrix));
		}
		photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
	}

	void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
	{
		photoCaptureObject.Dispose();
		photoCaptureObject = null;
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
			status.GetComponent<TextMesh>().text = "taking photo...";
			status.SetActive(true);
			PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
		};
		recognizer.StartCapturingGestures();

		PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
	}
}