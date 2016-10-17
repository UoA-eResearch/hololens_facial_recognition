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

	IEnumerator<object> PostToFaceAPI(byte[] imageData, Matrix4x4 cameraToWorldMatrix, Matrix4x4 pixelToCameraMatrix) {

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
			yield break;
		}
		else
		{
			status.SetActive(false);
		}

		var faceRectangles = "";
		Dictionary<string, TextMesh> textmeshes = new Dictionary<string, TextMesh>();

		foreach (var result in j.list) {
			GameObject txtObject = (GameObject)Instantiate(textPrefab);
			TextMesh txtMesh = txtObject.GetComponent<TextMesh>();
			var a = result.GetField("faceAttributes");
			var f = a.GetField("facialHair");
			var p = result.GetField("faceRectangle");
			float top = -(p.GetField("top").f / cameraResolution.height -.5f);
			float left = p.GetField("left").f / cameraResolution.width - .5f;
			float width = p.GetField("width").f / cameraResolution.width;
			float height = p.GetField("height").f / cameraResolution.height;

			string id = string.Format("{0},{1},{2},{3}", p.GetField("left"), p.GetField("top"), p.GetField("width"), p.GetField("height"));
			textmeshes[id] = txtMesh;

			if (faceRectangles == "") {
				faceRectangles = id;
			} else {
				faceRectangles += ";" + id;
			}

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
				txtObject.transform.localScale /= 5;
			}

			txtMesh.text = string.Format("Gender: {0}\nAge: {1}\nMoustache: {2}\nBeard: {3}\nSideburns: {4}\nGlasses: {5}\nSmile: {6}", a.GetField("gender").str, a.GetField("age"), f.GetField("moustache"), f.GetField("beard"), f.GetField("sideburns"), a.GetField("glasses").str, a.GetField("smile"));
		}

		// Emotion API

		url = "https://api.projectoxford.ai/emotion/v1.0/recognize?faceRectangles=" + faceRectangles;

		headers["Ocp-Apim-Subscription-Key"] = "6c72ec57a32c460d9419f56eeca77368";

		www = new WWW(url, imageData, headers);
		yield return www;
		responseString = www.text;

		j = new JSONObject(responseString);
		Debug.Log(j);
		existing = GameObject.FindGameObjectsWithTag("emoteText");

		foreach (var go in existing)
		{
			Destroy(go);
		}

		foreach (var result in j.list) {
			var p = result.GetField("faceRectangle");
			string id = string.Format("{0},{1},{2},{3}", p.GetField("left"), p.GetField("top"), p.GetField("width"), p.GetField("height"));
			var txtMesh = textmeshes[id];
			var obj = result.GetField("scores");
			string highestEmote = "Unknown";
			float highestC = 0;
			for (int i = 0; i < obj.list.Count; i++)
			{
				string key = obj.keys[i];
				float c = obj.list[i].f;
				if (c > highestC) {
					highestEmote = key;
					highestC = c;
				}
			}
			txtMesh.text += "\nEmotion: " + highestEmote;
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