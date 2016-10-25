using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class JSON_test : MonoBehaviour
{

	public GameObject textPrefab;

	// Use this for initialization
	void Start()
	{
		string json = "[{\"faceId\":\"5cfdda25 - 743e-482d - 9d33 - 5806f8bf5056\",\"faceRectangle\":{\"top\":891,\"left\":18,\"width\":64,\"height\":64},\"faceAttributes\":{\"smile\":0.024,\"headPose\":" +
		"{\"pitch\":0.0,\"roll\":-3.1,\"yaw\":-7.3},\"gender\":\"male\",\"age\":25.4,\"facialHair\":{\"moustache\":0.6,\"beard\":0.6,\"sideburns\":0.5},\"glasses\":\"ReadingGlasses\"}}]";
		JSONObject j = new JSONObject(json);
		JSONObject result = j.list[0];
		GameObject txtObject = (GameObject)Instantiate(textPrefab);
		TextMesh txtMesh = txtObject.GetComponent<TextMesh>();
		var a = result.GetField("faceAttributes");
		var f = a.GetField("facialHair");
		//var p = result.GetField("faceRectangle");
		txtMesh.text = string.Format("Gender: {0}\nAge: {1}\nMoustache: {2}\nBeard: {3}\nSideburns: {4}\nGlasses: {5}\nSmile: {6}", a.GetField("gender").str, a.GetField("age"), f.GetField("moustache"), f.GetField("beard"), f.GetField("sideburns"), a.GetField("glasses").str, a.GetField("smile"));
		txtMesh.color = Color.red; // Set the text's color to red
	}

	// Update is called once per frame
	void Update()
	{

	}
}
