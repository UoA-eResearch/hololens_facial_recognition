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

	public GameObject textPrefab;
	public GameObject status;

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
			status.GetComponent<TextMesh>().text = "camera ready";
			photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
		}
		else
		{
			Debug.LogError("Unable to start photo mode!");
		}
	}

	IEnumerator<object> PostToFaceAPI(byte[] imageData, Matrix4x4 cameraToWorldMatrix, Matrix4x4 projectionMatrix) {

		var url = "https://api.projectoxford.ai/face/v1.0/detect?returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses";
		var headers = new Dictionary<string, string>() {
			{ "Ocp-Apim-Subscription-Key", "54a11f7e7e3047f481f8e285a7ce5059" },
			{ "Content-Type", "application/octet-stream" }
		};
		status.GetComponent<TextMesh>().text = "loading...";
		WWW www = new WWW(url, imageData, headers);
		yield return www;
		string responseString = www.text;
		
		JSONObject j = new JSONObject(responseString);
		Debug.Log(j);
		var existing = GameObject.FindGameObjectsWithTag("text");

		foreach (var go in existing ) {
			Destroy(go);
		}

		status.SetActive(false);

		Vector3 cameraPosition = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
		Debug.Log(string.Format("cam {0}", cameraPosition));
		Debug.Log(string.Format("proj {0}", projectionMatrix));
		Debug.Log(string.Format("cam2world {0}", cameraToWorldMatrix));
		Matrix4x4 inverseMVP = (projectionMatrix * cameraToWorldMatrix.inverse).inverse; // the projectionMatrix and worldToCameraMatrix are from the photoCapture information
		Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

		foreach (var result in j.list) {
			GameObject txtObject = (GameObject)Instantiate(textPrefab);
			TextMesh txtMesh = txtObject.GetComponent<TextMesh>();
			var a = result.GetField("faceAttributes");
			var f = a.GetField("facialHair");
			var p = result.GetField("faceRectangle");
			var top = p.GetField("top").i;
			var left = p.GetField("left").i;
			var width = p.GetField("width").i;
			var height = p.GetField("height").i;

			Vector3 infoOffsetPoint = new Vector3(top, left + width, 100);
			Debug.Log(string.Format("info offset {0}", infoOffsetPoint));
			Vector3 offset = inverseMVP.MultiplyPoint3x4(infoOffsetPoint);
			Debug.Log(offset);

			Vector3 position = cameraPosition + offset;

			txtObject.transform.position = position;
			txtObject.transform.rotation = rotation;
			txtObject.tag = "face";

			txtMesh.text = string.Format("Gender: {0}\nAge: {1}\nMoustache: {2}\nBeard: {3}\nSideburns: {4}\nGlasses: {5}\nSmile: {6}", a.GetField("gender").str, a.GetField("age"), f.GetField("moustache"), f.GetField("beard"), f.GetField("sideburns"), a.GetField("glasses").str, a.GetField("smile"));
		}
	}

	void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
	{
		if (result.success)
		{
			Debug.Log("photo captured");
			status.GetComponent<TextMesh>().text = "photo captured";
			status.SetActive(true);
			List<byte> imageBufferList = new List<byte>();
			// Copy the raw IMFMediaBuffer data into our empty byte list.
			photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);

			var cameraToWorldMatrix = new Matrix4x4();
			photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);

			var projectionMatrix = new Matrix4x4();
			photoCaptureFrame.TryGetProjectionMatrix(out projectionMatrix);

			StartCoroutine(PostToFaceAPI(imageBufferList.ToArray(), cameraToWorldMatrix, projectionMatrix));
		}
	}

	void Destroy() {
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
			if (photoCaptureObject == null) {
				Debug.LogError("Camera not yet ready!");
			} else {
				photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
			}
		};
		recognizer.StartCapturingGestures();

		PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
	}
}