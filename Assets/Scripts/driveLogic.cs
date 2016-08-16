using UnityEngine;
using System.Collections;
using System.Linq;
using GemSDK.Unity;
using UnityEngine.UI;

public class driveLogic : MonoBehaviour {

	//TOFIX

	//Magic constants
	float steeringRatio = 12f;	//amount of times faster steering wheel turns to carfront wheels
	float minTurningRad = 10f; //higher number means the wheel is less sensative
	float maxSteeringWheelAngleDeg = 450f;//wheel can physically turn by +- this amount in degrees
	float maxCarSpeed = 1.8f;
	float noPedalSpeed = 0.005f;
	float dragCoeff = 0.01f;
	float maxGasAcc = 0.1f;
	float maxBreakPedalAngleDeg = 80f;
	float maxGasPedalAngleDeg = 80f;


	//Unity Gameobjects
	public Text steeringWheelGemStateTxt;
	public Transform steeringWheelStateImg;
	public Text gasPedalGemStateTxt;
	public Transform gasPedalStateImg;
	public Text breakPedalGemStateTxt;
	public Transform breakPedalStateImg;
	public Transform steeringWheelModel;
	public Transform[] frontWheels = new Transform[2];
	public GameObject firstPerson;
	public GameObject thirdPerson;

	//Values to keep between FixedUpdate
	IGem steeringWheelGem;
	Quaternion prevSteeringWheelGemRotation = Quaternion.identity;
	Quaternion currentSteeringWheelGemRotation = Quaternion.identity;
	Quaternion inverseStartSteeringWheelGemRotation = Quaternion.identity;
	float steeringWheelAngleDeg = 0f;
	float carSpeed = 0.01f;
	IGem gasPedalGem;
	Quaternion currentGasPedalGemRotation = Quaternion.identity;
	Quaternion inverseStartGasPedalGemRotation = Quaternion.identity;
	IGem breakPedalGem;
	Quaternion currentBreakPedalGemRotation = Quaternion.identity;
	Quaternion inverseStartBreakPedalGemRotation = Quaternion.identity;

	void Start () {
		GemManager.Instance.Connect();
		steeringWheelGem = GemManager.Instance.GetGem("5C:F8:21:9C:FF:C4");
		gasPedalGem =  GemManager.Instance.GetGem("D0:B5:C2:90:7E:65");
		breakPedalGem =  GemManager.Instance.GetGem("D0:B5:C2:90:7E:4B");

	}

	void FixedUpdate () {
		if (steeringWheelGem != null)  {
			if (Input.GetMouseButton(0)){
				resetAll();
				return;
			}
			//if (Input.GetKeyDown(KeyCode.Space)){ //For Computer
			if (Input.GetKeyDown(KeyCode.Escape)){	//For VR
				toggleCameraView();
			}

			printGemState();
			updateGemRotations();

			moveCar();

		}
	}

	void moveCar(){
		updateSteeringWheelAngle();

		animateCarParts();


		if(steeringWheelGem.State == GemState.Connected && gasPedalGem.State == GemState.Connected && breakPedalGem.State == GemState.Connected){
			carSpeed = getCarSpeed();
			float turningRad = getTurningRad();
			updateCarPos(turningRad);
			updateCarRot(turningRad);
		}
	}

	float getCarSpeed(){
		//TOFIX
			//Make sure I implemented acceloration correctly so that the car feels real
			//Make acceloration less when car speed is high (0 - 60) time < (60 - 120) time

		//float angle = Quaternion.Angle(Quaternion.identity, currentGasPedalGemRotation);
		//return noPedalSpeed + ((angle/180f) * (maxCarSpeed - noPedalSpeed));

		float newCarSpeed = carSpeed;

		//gas
		if(newCarSpeed < maxCarSpeed){
			float gasAngle = Quaternion.Angle(Quaternion.identity, currentGasPedalGemRotation);
			float speedRatio = newCarSpeed / maxCarSpeed;
			newCarSpeed += (gasAngle / maxGasPedalAngleDeg) * (1f - speedRatio) * maxGasAcc;
		}

		//drag - this slowly damps the speed to approach the nePedalSpeed
		newCarSpeed -= dragCoeff * (newCarSpeed - noPedalSpeed);

		//break
		float breakAngle = Quaternion.Angle(Quaternion.identity, currentBreakPedalGemRotation);
		newCarSpeed -= (breakAngle / maxBreakPedalAngleDeg) * newCarSpeed;


		//edge cases
		if(newCarSpeed > maxCarSpeed){
			newCarSpeed = maxCarSpeed;
		}
		if(newCarSpeed < 0f){
			newCarSpeed = 0f;
		}
		return newCarSpeed;
	}

	void rotateFrontCarWheelsModel(){
		float wheelsAngleDeg = steeringWheelAngleDeg / steeringRatio;
		frontWheels[0].transform.localRotation = Quaternion.AngleAxis(wheelsAngleDeg, Vector3.up);
		frontWheels[1].transform.localRotation = Quaternion.AngleAxis(wheelsAngleDeg, Vector3.up);
	}

	void animateCarParts(){
		if(thirdPerson.active){
			rotateFrontCarWheelsModel();
		}
		else if (firstPerson.active){
			rotateSteeringWheelModel();
		}
	}

	void rotateSteeringWheelModel(){
		Quaternion q = Quaternion.FromToRotation(Vector3.up, Vector3.back);
		steeringWheelModel.localRotation = Quaternion.Inverse(q) * Quaternion.AngleAxis(steeringWheelAngleDeg, Vector3.up) * q;
	}

	void updateCarPos(float turningRad){
		//1 for circle on right -1 for circle on left
		float circleSign = 1;

		//negative rad means circle center is left of old position
		if(turningRad < 0){
			circleSign = -circleSign;
			turningRad = -turningRad;
		}

		float arcAngle = carSpeed / turningRad;

		float newX = circleSign * (turningRad * (1 - Mathf.Cos(arcAngle)));
		float newZ = turningRad * Mathf.Sin(arcAngle);

		transform.position = transform.position + (transform.rotation * new Vector3(newX, 0f, newZ));
	}

	void updateCarRot(float turningRad){
		float arcAngleDeg = (carSpeed / turningRad) * Mathf.Rad2Deg;
		transform.rotation = transform.rotation *  Quaternion.AngleAxis(arcAngleDeg, Vector3.up);
	}

	void updateSteeringWheelAngle(){
		Vector3 prevSteeringWheelDirection = prevSteeringWheelGemRotation * Vector3.forward;
		Vector3 currentSteeringWheelDirection = currentSteeringWheelGemRotation * Vector3.forward;

		steeringWheelAngleDeg += AngleSigned(prevSteeringWheelDirection, currentSteeringWheelDirection, prevSteeringWheelGemRotation * Vector3.up);
	}

	float getTurningRad(){

		//Do this to avoid divsion by zero
		if(Mathf.Abs(steeringWheelAngleDeg) < 0.00001f){
			return float.MaxValue;
		}

		return minTurningRad * (maxSteeringWheelAngleDeg / steeringWheelAngleDeg);
	}


	void toggleCameraView(){
    thirdPerson.SetActive(!thirdPerson.active);
    firstPerson.SetActive(!firstPerson.active);
  }

	void resetAll(){
		calibrateGems();
		transform.position  = new Vector3(0, 0, 0);
		transform.rotation  = Quaternion.identity;
		steeringWheelAngleDeg = 0f;
		carSpeed = 0.01f;
		currentSteeringWheelGemRotation = Quaternion.identity;
	}

	void updateGemRotations(){
		prevSteeringWheelGemRotation = currentSteeringWheelGemRotation;
		currentSteeringWheelGemRotation = inverseStartSteeringWheelGemRotation * steeringWheelGem.Rotation;

		currentGasPedalGemRotation = inverseStartGasPedalGemRotation * gasPedalGem.Rotation;
		currentBreakPedalGemRotation = inverseStartBreakPedalGemRotation * breakPedalGem.Rotation;

	}

	void printGemState(){
		steeringWheelGemStateTxt.text = "Wheel: " + steeringWheelGem.State;
		Quaternion q = Quaternion.LookRotation(Vector3.right, Vector3.back);
		steeringWheelStateImg.transform.rotation = q * currentSteeringWheelGemRotation * Quaternion.Inverse(q);

		gasPedalGemStateTxt.text = "Gas: " + gasPedalGem.State;
		gasPedalStateImg.transform.rotation = currentGasPedalGemRotation;

		breakPedalGemStateTxt.text = "Break: " + breakPedalGem.State;
		breakPedalStateImg.transform.rotation = currentBreakPedalGemRotation;
	}

	void calibrateGems(){
		inverseStartSteeringWheelGemRotation = Quaternion.Inverse(steeringWheelGem.Rotation);
		inverseStartGasPedalGemRotation = Quaternion.Inverse(gasPedalGem.Rotation);
		inverseStartBreakPedalGemRotation = Quaternion.Inverse(breakPedalGem.Rotation);


	}

	//returns an angle with proper sign given two vectors and a vector orth to them
	float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n){
		return Mathf.Atan2(
		Vector3.Dot(n, Vector3.Cross(v1, v2)),
		Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
	}

	void OnApplicationQuit(){
		GemManager.Instance.Disconnect();
	}

	//For Android to unbind Gem Service when the app is not in focus
	void OnApplicationPause(bool paused){
		if (Application.platform == RuntimePlatform.Android){
			if (paused)
			GemManager.Instance.Disconnect();
			else
			GemManager.Instance.Connect();
		}
	}
}
