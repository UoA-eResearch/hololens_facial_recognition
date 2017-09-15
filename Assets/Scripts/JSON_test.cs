using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class JSON_test : MonoBehaviour
{
	// Use this for initialization
	void Start()
	{
		string json = @"[
	{
			""kairos"": {
				""eyeDistance"": 16, 
            ""topLeftX"": 29, 
            ""topLeftY"": 30, 
            ""chinTipX"": 45, 
            ""width"": 36, 
            ""yaw"": 7, 
            ""chinTipY"": 68, 
            ""confidence"": 0.9993, 
            ""height"": 36, 
            ""rightEyeCenterY"": 39, 
            ""rightEyeCenterX"": 37, 
            ""leftEyeCenterY"": 39, 
            ""leftEyeCenterX"": 54, 
            ""pitch"": -4, 
            ""attributes"": {
					""gender"": {
						""femaleConfidence"": 0, 
                    ""type"": ""M"", 
                    ""maleConfidence"": 1
	
				}, 
                ""age"": 27, 
                ""hispanic"": 0.02015, 
                ""lips"": ""Together"", 
                ""other"": 0.00129, 
                ""black"": 2e-05, 
                ""asian"": 9e-05, 
                ""white"": 0.97845, 
                ""glasses"": ""None""

			}, 
            ""face_id"": 1, 
            ""quality"": 0.8038, 
            ""roll"": 0
	
		}, 
        ""text"": ""Age: 27\nGender: M\nEthnicity: white: 98%, hispanic: 2%\nGlasses: None\nLips: Together\nEmotion: happiness: 78%\nSmile: 44%\nBeauty: F:56%, M:56%\nRecognition confidence: 93.96%\nUPI: nyou045\nName: Mr Nick Young\nPosition: Research IT Specialist\nDepartment: Computer Science\nReports to: Mr Marcus Gustafsson\n"", 
        ""fpp"": {
				""attributes"": {
					""emotion"": {
						""neutral"": 20.253, 
                    ""sadness"": 0.673, 
                    ""disgust"": 0.907, 
                    ""anger"": 0.053, 
                    ""surprise"": 0.3, 
                    ""fear"": 0.053, 
                    ""happiness"": 77.761
	
				}, 
                ""beauty"": {
						""female_score"": 56.236, 
                    ""male_score"": 55.571

				}, 
                ""gender"": {
						""value"": ""Male""

				}, 
                ""age"": {
						""value"": 30

				}, 
                ""eyestatus"": {
						""left_eye_status"": {
							""normal_glass_eye_open"": 0.034, 
                        ""no_glass_eye_close"": 0.0, 
                        ""occlusion"": 0.0, 
                        ""no_glass_eye_open"": 99.966, 
                        ""normal_glass_eye_close"": 0.0, 
                        ""dark_glasses"": 0.0
	
					}, 
                    ""right_eye_status"": {
							""normal_glass_eye_open"": 0.397, 
                        ""no_glass_eye_close"": 0.0, 
                        ""occlusion"": 0.0, 
                        ""no_glass_eye_open"": 99.601, 
                        ""normal_glass_eye_close"": 0.002, 
                        ""dark_glasses"": 0.0

					}
					}, 
                ""glass"": {
						""value"": ""None""

				}, 
                ""headpose"": {
						""yaw_angle"": 1.8853482, 
                    ""pitch_angle"": 6.320396, 
                    ""roll_angle"": -2.5211878

				}, 
                ""blur"": {
						""blurness"": {
							""threshold"": 50.0, 
                        ""value"": 1.488
	
					}, 
                    ""motionblur"": {
							""threshold"": 50.0, 
                        ""value"": 1.488

					}, 
                    ""gaussianblur"": {
							""threshold"": 50.0, 
                        ""value"": 1.488

					}
					}, 
                ""smile"": {
						""threshold"": 30.1, 
                    ""value"": 44.069

				}, 
                ""facequality"": {
						""threshold"": 70.1, 
                    ""value"": 92.136

				}, 
                ""ethnicity"": {
						""value"": ""White""

				}
				}, 
            ""face_token"": ""f163a6c3c07461e71f5da1eab755ce36"", 
            ""face_rectangle"": {
					""width"": 37, 
                ""top"": 32, 
                ""height"": 37, 
                ""left"": 27

			}
			}, 
        ""of"": {
				""confidence"": 0.9395938787156883, 
            ""data"": {
					""phoneNumbers"": [
	                    {
                        ""phone"": ""+64 9 923 9740"",
	                        ""type"": ""ddi""

					}, 
                    {
                        ""phone"": ""89740"", 
                        ""type"": ""extension""
                    }
                ], 
                ""orgUnits"": [
                    ""COMSCI"",
                    ""SCIFAC""
                ], 
                ""upi"": ""nyou045"", 
                ""groupProfile"": false, 
                ""positions"": [
                    {
                        ""department"": {
                            ""name"": ""Computer Science"",
                            ""unit"": ""COMSCI""
                        }, 
                        ""position"": ""Research IT Specialist"", 
                        ""orgUnits"": [
                            {
                                ""name"": ""Faculty of Science"",
                                ""unit"": ""SCIFAC""
                            }
                        ], 
                        ""reportsTo"": {
                            ""positionId"": 2, 
                            ""url"": ""m-gustafsson"", 
                            ""name"": ""Mr Marcus Gustafsson""
                        }
                    }
                ], 
                ""image"": ""/people/imageraw/nick-young/10547437/large"", 
                ""profileUrl"": ""nick-young"", 
                ""fullName"": ""Mr Nick Young"", 
                ""emailAddresses"": [
                    ""nick.young@auckland.ac.nz"",
                    ""nyou045@aucklanduni.ac.nz""
                ]
            }, 
            ""uid"": ""nyou045"", 
            ""face_rectangle"": {
                ""width"": 37, 
                ""top"": 30, 
                ""left"": 25, 
                ""height"": 37
            }
        }
    }
]";
		JSONObject j = new JSONObject(json);
		Debug.Log(j[0]["text"].str);
	}
}
