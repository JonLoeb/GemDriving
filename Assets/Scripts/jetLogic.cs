using UnityEngine;
using System.Collections;
using System.Linq;
using GemSDK.Unity;
using UnityEngine.UI;

public class jetLogic : MonoBehaviour{

  public float counter = 0f;
  int highScore = 0;
  int currentScore = 0;

  public float jetSpeed = 0.15f;
  Quaternion wheelRotation = Quaternion.identity;

  private bool safetyBubbleIsOn = false;
  bool miniPower = false;
  bool maxiPower = false;
  bool superSpeedPower = false;

  private IGem wheelGem;

  public Transform steeringWheel;
  public Transform jetBody;
  public Transform camera;
  public GameObject protectionBubble;

  public Text superPower;
  public Text score;
  public Text wheelStatus;

  private Quaternion inverseStartRotationWheel = Quaternion.identity;


  public GameObject[] shape;

  // Use this for initialization
  void Start()
  {
    GemManager.Instance.Connect();

    //To get gem by number instead of address, on Android the Gem should be paired to Gem SDK Utility app
    //gem = GemManager.Instance.GetGem(0);

    // gem[3] =  GemManager.Instance.GetGem("98:7B:F3:5A:5C:DD");//white
    // gem[1] =  GemManager.Instance.GetGem("98:7B:F3:5A:5C:E6");//orange
    // gem[2] =  GemManager.Instance.GetGem("98:7B:F3:5A:5C:3A");//green
    // gem[0] =  GemManager.Instance.GetGem("D0:B5:C2:90:78:E4");//red
    // gem[4] =  GemManager.Instance.GetGem("D0:B5:C2:90:7C:4D");//blue
    // gem[5] =  GemManager.Instance.GetGem("98:7B:F3:5A:5C:6D");//yellow
    wheelGem = GemManager.Instance.GetGem("5C:F8:21:9C:FF:C4");


  }

  void FixedUpdate(){
    if (wheelGem != null){
      if (Input.GetMouseButton(0)){
        calibrateGems();

        transform.position  = new Vector3(0, 0, 0);
        transform.rotation  = Quaternion.identity;
        wheelRotation = Quaternion.identity;
        resetAll();
      }

      //Show all the data
      printGemStatus();

      spawnShapes();
      updateScore();

      wheelRotation = getWheelRotation();

      updateSuperPowers();

      //rotateWheel();

      movejet();


    }
  }

  void updateScore(){
    if(wheelGem.State == GemState.Connected){
      currentScore++;
    }

    if (currentScore > highScore) {
      highScore = currentScore;
    }

    score.text = "HighScore: " + highScore.ToString() + "\nCurrent: " + currentScore.ToString();

  }

  void updateSuperPowers(){
    counter = counter + 1f;
    if (counter > 500f){
      resetAll();
    }
    else{
      float scale;
      if(miniPower){
        float miniSize = 0.4f;
        scale  = miniSize + ((counter / 500f) * (1f - miniSize));
        jetBody.transform.localScale = new Vector3(scale, scale, scale);
      }
      else if(maxiPower){
        float maxiSize = 1.3f;
        scale  = maxiSize - ((counter / 500f) * (maxiSize - 1f));
        jetBody.transform.localScale = new Vector3(scale, scale, scale);
      }
      if(safetyBubbleIsOn){
        scale  = 2.6f - ((counter / 500f) * 2.6f);
        protectionBubble.transform.localScale  = new Vector3(scale, scale, scale);
      }
      if(superSpeedPower){
        float topSpeed = 1.5f;
        float minSpeed = 0.1f;
        float currentSpeed = topSpeed - ((counter / 500f) * (topSpeed - minSpeed));
      }


    }
  }

  void resetAll(){
    currentScore = 0;
    superPower.text = "Mode: Normal";
    counter = 0f;
    jetSpeed = 0.15f;
    jetBody.transform.localScale = new Vector3(1f, 1f, 1f);
    turnOffAllPowers();
    protectionBubble.SetActive(false);
    //(GetComponent("Halo") as Behaviour).enabled = false;
  }

  void turnOffAllPowers(){
    miniPower = false;
    maxiPower = false;
    superSpeedPower = false;
    safetyBubbleIsOn = false;
  }

  void printGemStatus(){
    wheelStatus.text = "wheel: " + wheelGem.State;
    if(wheelGem.State.ToString() == "Connected"){
      wheelStatus.text = "";
    }
  }

  void spawnShapes(){
    for (int i = 0; i < shape.Length; i++){
      if(shape[i].GetComponent<Renderer>().isVisible == false){

        Vector3 pos = (transform.rotation * getRandomPos()) + transform.position;
        shape[i].transform.position = pos;
        //shape[i].transform.LookAt(jetBody);
      }
      shape[i].transform.LookAt(camera);

    }

  }

  void OnTriggerEnter(Collider other) {
    if(!safetyBubbleIsOn){
      if (other.tag == "cone"){
        transform.position  = new Vector3(0, 0, 0);
        resetAll();
        for (int i = 0; i < shape.Length; i++){
          Vector3 shapePos = (transform.rotation * getRandomPos()) + transform.position;
          shape[i].transform.position = shapePos;
          shape[i].transform.LookAt(camera);
        }
      }
      else{
        (other.GetComponent("Halo") as Behaviour).enabled = true;
      }
    }

    else{
      Vector3 pos = (transform.rotation * getRandomPos()) + transform.position;
      other.transform.parent.position = pos;
      other.transform.parent.LookAt(camera);
    }
  }

  void OnTriggerExit(Collider other) {
    (other.GetComponent("Halo") as Behaviour).enabled = false;

    if(!safetyBubbleIsOn){
      if(other.tag == "cylinder"){
        startSuperSpeed();
      }
      else if(other.tag == "dodecahedron"){
        startSuperSpeed();

      }
      else if(other.tag == "sphere"){
        //startSuperSpeed();

        startHugePower();
      }
      else if(other.tag == "cube"){
        //startSuperSpeed();

        makeImmortal();
      }
      else if(other.tag == "tetrahedron"){
        startMiniPower();
      }
      else if(other.tag == "octahdron"){
        startMiniPower();
      }


      Vector3 pos = (transform.rotation * getRandomPos()) + transform.position;
      other.transform.parent.position = pos;
      other.transform.parent.LookAt(camera);


    }

  }

  Vector3 getRandomPos(){
    float randY = getRandomInRadOutRad(0.5f, 5f);
    float randX = getRandomInRadOutRad(0.5f, 5f);
    //float randZ = Random.Range(3f, 12f);
    float randZ = 12f;


    return new Vector3(randX, randY, randZ);
  }

  void startSuperSpeed(){
    superPower.text = "Mode: Super Speed";
    counter = 0f;
    jetSpeed = 0.35f;

    superSpeedPower = true;
  }

  void makeImmortal(){
    superPower.text = "Mode: Immortal";
    counter = 0f;
    safetyBubbleIsOn = true;
    protectionBubble.SetActive(true);
    protectionBubble.transform.localScale = new Vector3(2.6f, 2.6f, 2.6f);
  }

  void startMiniPower(){
    superPower.text = "Mode: Mini";
    counter = 0f;
    maxiPower = false;
    miniPower = true;

  }

  void startHugePower(){
    superPower.text = "Mode: Huge";
    counter = 0f;
    //jetBody.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
    miniPower = false;
    maxiPower = true;
  }

  //TOFIX find a way to find a random number in a range and out another range
  float getRandomInRadOutRad(float inSide, float outSide){
    float range = outSide - inSide;
    float randNum = outSide + (Random.Range(- range, range));
    if (Random.value >= 0.5){
      return -randNum;
    }

    return randNum;
  }

  void movejet(){


    Quaternion jetDir = getJetDir();

    jetBody.localRotation = Quaternion.Slerp(Quaternion.identity, jetDir, 0.3f);
    camera.localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Inverse(transform.rotation), 0.05f);


    //getjetVel();


    if(Input.GetKey(KeyCode.Space) || wheelGem.State == GemState.Connected){

      transform.position = (jetSpeed * ((transform.rotation * jetDir) * Vector3.forward)) + transform.position;
      transform.rotation =  Quaternion.Slerp(transform.rotation, transform.rotation * jetDir, 0.01f);

      //turn on for mouse controlled steering
      if (false){
        Vector3 mouseVector = new Vector3(Input.GetAxis("Mouse X"), 0f, .2f);
        //transform.position = (jetSpeed * mouseVector) + transform.position;
      }

    }
  }



  Quaternion getJetDir(){
    //jet mode
    //Quaternion q = Quaternion.LookRotation(Vector3.right, Vector3.up);

    //fly mode
    Quaternion q = Quaternion.LookRotation(Vector3.right, Vector3.back);


    return q * wheelRotation * Quaternion.Inverse(q);
  }

  //returns an angle with proper sign given two vectors and a vector normal to them
  float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n){
    return Mathf.Atan2(
    Vector3.Dot(n, Vector3.Cross(v1, v2)),
    Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
  }



  void rotateWheel(){
    Quaternion q = Quaternion.LookRotation(Vector3.right, Vector3.back);
    steeringWheel.transform.rotation = q * wheelRotation * Quaternion.Inverse(q);
  }

  void calibrateGems(){
    inverseStartRotationWheel = Quaternion.Inverse(wheelGem.Rotation);
  }

  Quaternion getWheelRotation(){
    return inverseStartRotationWheel * wheelGem.Rotation;

  }


  void OnApplicationQuit()
  {
    GemManager.Instance.Disconnect();
  }

  //For Android to unbind Gem Service when the app is not in focus
  void OnApplicationPause(bool paused)
  {
    if (Application.platform == RuntimePlatform.Android)
    {
      if (paused)
      GemManager.Instance.Disconnect();
      else
      GemManager.Instance.Connect();
    }
  }
}
