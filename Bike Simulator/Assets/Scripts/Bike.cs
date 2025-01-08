using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bike : MonoBehaviour
{
    [SerializeField]
    WheelCollider frontWheel;
    [SerializeField]
    WheelCollider rearWheel;
    [SerializeField]
    [Range(-1,1)]float maxSteer;
    [SerializeField]
    float maxLean;
    [SerializeField]
    float centerofMassY;
    [SerializeField]
   [Range(0f,1f)] public float value;
    public Rigidbody rb;

    public float mass;
    [SerializeField] 
    [Range(-1f,1f) ] public float leanValue;

    Vector3 COG;
    [Range(-40, 40)]public float layingammount;
    [Range(0.000001f, 1 )] [SerializeField] float leanSmoothing;
    public float targetlayingAngle;
    [SerializeField] float currentSteeringAngle;

    [Header("engineTorque")]
    [SerializeField]
    AnimationCurve enginetorque;
    [SerializeField]
   
    // [SerializeField]
    // public float gearRation[] ={2f, 1.7f, 1.3f, 0.9f, 0.5f};

    private const float MAX_TORQUE = 29.85f;
    private const float MAX_POWER = 24.41f;
    
    // RPM range from the graph
    private const float MIN_RPM = 2000f;
    private const float MAX_RPM = 5500f;
    
    // Current engine RPM

     [System.Serializable]
    public class GearRatio
    {
        public string name;
        public float ratio;
        public float minSpeed;
        public float maxSpeed;
        
        public GearRatio(string _name, float _ratio, float _minSpeed, float _maxSpeed)
        {
            name = _name;
            ratio = _ratio;
            minSpeed = _minSpeed;
            maxSpeed = _maxSpeed;
        }
    }
        private const float IDLE_RPM = 1000f;
    
    // Current engine state
    private float currentRPM = IDLE_RPM;
    private int currentGear = 1;
     private float clutchEngagement = 1f;
  private List<GearRatio> gearRatios = new List<GearRatio>
{
    new GearRatio("1st", 3.06f, 0f, 35f),     // Updated from 2.917f
    new GearRatio("2nd", 2.01f, 20f, 55f),    // Updated from 1.875f
    new GearRatio("3rd", 1.52f, 35f, 75f),    // Updated from 1.368f
    new GearRatio("4th", 1.21f, 50f, 95f),    // Updated from 1.045f
    new GearRatio("5th", 1.00f, 70f, 120f)    // Updated from 0.875f
};

    // Final drive ratio
    private const float FINAL_DRIVE_RATIO = 2.8f;

        [Header("Steering Components")]
    [SerializeField] private Transform handlebarTransform;
    [SerializeField] private Transform frontForkTransform;
    [SerializeField] private WheelCollider frontWheelCollider;
 
    
    [Header("Steering Settings")]
    [SerializeField] private float maxSteerAngle = 35f;
    [SerializeField] private float steeringSpeed = 5f;
    [SerializeField] private float returnSpeed = 6f;
    [SerializeField] private AnimationCurve steeringCurve;
    
    [Header("Visual Rotation Limits")]
    [SerializeField] private float maxHandlebarRotation = 45f;
    [SerializeField] private float maxForkRotation = 35f;

    // private float currentSteerAngle = 0f;
    // private float targetSteerAngle = 0f;

    [Header("Lean and Steer Settings")]
    [SerializeField] private LeanSteerSettings settings = new LeanSteerSettings();
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugGizmos = true;
    
    // Runtime variables
    private float currentLeanAngle = 0f;
    private float targetLeanAngle = 0f;
    private float currentSteerAngle = 0f;
    private float counterSteerAngle = 0f;
    [System.Serializable]
public class LeanSteerSettings
{
    public AnimationCurve leanVsSpeedCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(10f, 15f),
        new Keyframe(20f, 25f),
        new Keyframe(30f, 35f)
    );

    public float maxLeanAngle = 45f;
    public float leanResponseTime = 0.3f;
    public float counterSteerFactor = 0.2f;
    public float speedInfluenceFactor = 0.5f;
}

    // Angular Velocity and Power calculation variables
    private float wheelAngularVelocity;
    private float engineAngularVelocity;
    private float currentPower;
    private float currentTorque;
        [Header("Power and Torque Settings")]
    [SerializeField] private float maxEnginePower = 15063f; // Maximum power in watts (about 67 HP)
    [SerializeField] private float maxEngineTorque = 27f;   // Maximum torque in Nm
    [SerializeField] private float drivetrainEfficiency = 0.9f;




    







    // [SerializeField]
    // Curves curves;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mass = rb.mass + frontWheel.mass + rearWheel.mass;
        // enginetorque = new AnimationCurve();
        // // Key points from the graph (RPM normalized to 0-1, Torque in ft-lbs)
        // enginetorque.AddKey(2000f/MAX_RPM, 9f);     // Initial torque
        // enginetorque.AddKey(2500f/MAX_RPM, 28f);    // Sharp torque rise
        // enginetorque.AddKey(3000f/MAX_RPM, 28.5f);  // Plateau start
        // enginetorque.AddKey(4000f/MAX_RPM, 29.85f); // Peak torque
        // enginetorque.AddKey(4500f/MAX_RPM, 29f);    // Slight decline
        // enginetorque.AddKey(5000f/MAX_RPM, 25f);    // Steeper decline
        // enginetorque.AddKey(5500f/MAX_RPM, 20f);    // Final torque

        
        // // Adjust the curve tangents for smoother interpolation
        // for (int i = 0; i < enginetorque.keys.Length; i++)
        // {
        //     enginetorque.SmoothTangents(i, 0.5f);
        // }
    InitializeTorqueCurve();
            if (steeringCurve.length == 0)
        {
            steeringCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 0.5f),
                new Keyframe(1f, 1f)
            );
        }

      
    }

        private void InitializeTorqueCurve()
    {
        // Create a more detailed torque curve with points every 250 RPM
        List<Keyframe> keyframes = new List<Keyframe>
        {
            // Initial range (2000-2500 RPM)
            new Keyframe(2000f, 7.0f),
            new Keyframe(2250f, 18.0f),
            new Keyframe(2500f, 28.0f),
            
            // Mid range climbing (2500-3500 RPM)
            new Keyframe(2750f, 28.3f),
            new Keyframe(3000f, 28.5f),
            new Keyframe(3250f, 28.8f),
            new Keyframe(3500f, 29.2f),
            
            // Peak torque range (3500-4000 RPM)
            new Keyframe(3750f, 29.5f),
            new Keyframe(4000f, 29.85f),
            
            // High RPM decline (4000-5500 RPM)
            new Keyframe(4250f, 29.4f),
            new Keyframe(4500f, 29.0f),
            new Keyframe(4750f, 27.5f),
            new Keyframe(5000f, 25.0f),
            new Keyframe(5250f, 22.5f),
            new Keyframe(5500f, 20.0f)
        };

        enginetorque = new AnimationCurve(keyframes.ToArray());
        
        // Smooth out the curve
        for (int i = 0; i < keyframes.Count; i++)
        {
            enginetorque.SmoothTangents(i, 0.5f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
     void FixedUpdate() {
        float speed = GetVehicleSpeed();
        float maxSpeedCurrentGear = gearRatios[currentGear - 1].maxSpeed;
        float torque = GetCurrentPower();
        bool front = frontWheel;
        bool rear = rearWheel;
        Debug.Log(torque);

        if (front == frontWheel.isGrounded && rear == rearWheel.isGrounded){
        // Apply the acceleration to the bike
        if (speed < maxSpeedCurrentGear)
        {
            setAcceleration();
            UpdateRPM();
            
        }

        // HandleSteering();
        // LayOnTurn();
            HandleGearShifting();
            brake();
            UpdateVisuals();
            UpdateLeanAndSteer();   
        }
    
}  
    private void HandleGearShifting()
    {
        float speed = GetVehicleSpeed();
        
        // Automatic gear shifting based on speed ranges
        for (int i = 0; i < gearRatios.Count; i++)
        {
            if (speed >= gearRatios[i].minSpeed && speed <= gearRatios[i].maxSpeed)
            {
                if (currentGear != i + 1)
                {
                    StartGearShift(i + 1);
                }
                break;
            }
            
         }
            // Manual gear up
    if (Input.GetKeyDown(KeyCode.E))
    {
        if (speed >= gearRatios[currentGear].minSpeed)
        {
            StartGearShift(currentGear + 1);
            value = 0f;
        }
    }
    
    // Manual gear down
    if (Input.GetKeyDown(KeyCode.Q) && speed >= gearRatios[currentGear -1].minSpeed)
    {
        if(speed <= gearRatios[0].maxSpeed){
            StartGearShift(currentGear);
        }
        else{
            StartGearShift(currentGear - 1);
            value = 0f;
        }
    }
    }
    
    private void StartGearShift(int newGear)
    {
        // Simulate clutch disengagement
        clutchEngagement = 0f;
        currentGear = newGear;
        
        // Gradually re-engage clutch
        StartCoroutine(ClutchEngagement());
    }
    
    private System.Collections.IEnumerator ClutchEngagement()
    {
        float engagementTime = 0.5f; // Half second gear change
        float elapsed = 0f;
        
        while (elapsed < engagementTime)
        {
            elapsed += Time.deltaTime;
            clutchEngagement = Mathf.Lerp(0f, 1f, elapsed / engagementTime);
            yield return null;
        }
        
        clutchEngagement = 1f;
    } 

    // public float engineRpm(){
    //     float engine_rpm = 1000f;
    //     float rpmIncreaseRate = 300f;
    //     float rpmDecreaseRate = 200f;
    //     float time = Time.fixedDeltaTime;
    //     float throttle = getThrottleInput();
    //     if(throttle > 0f){
    //         engine_rpm += rpmIncreaseRate * throttle * time;
    //         if (engine_rpm > MAX_RPM){
    //             engine_rpm = MAX_RPM;
    //         }
    //     else{
    //         engine_rpm -= rpmDecreaseRate * time;
    //     }

    //     if(engine_rpm< IDLE_RPM){
    //         engine_rpm = IDLE_RPM;
    //     }
    //     }
    //     return engine_rpm;
    // }

        private void UpdateRPM()
    {
        
        // Calculate average wheel RPM
        float wheelRPM =0f;
        wheelRPM = rearWheel.rpm;

        // float engineRPM = engineRpm();
        
        // Convert wheel RPM to engine RPM through current gear ratio
        float gearRatio = gearRatios[currentGear - 1].ratio;
        float totalRatio = gearRatio * FINAL_DRIVE_RATIO;
        
        // Calculate target RPM based on wheel speed and gear ratio
        float targetRPM = Mathf.Abs(wheelRPM * totalRatio);
        
        // Factor in throttle and simulate engine behavior
        float throttle = getThrottleInput();
        if (throttle > 0.1f)
        {
            targetRPM = Mathf.Max(targetRPM, IDLE_RPM);
            currentRPM = Mathf.Lerp(currentRPM, targetRPM, Time.fixedDeltaTime);
        }
        else
        {
            currentRPM = Mathf.Lerp(currentRPM, IDLE_RPM, Time.fixedDeltaTime);
        }
        
        // Clamp RPM to valid range
        currentRPM = Mathf.Clamp(currentRPM, IDLE_RPM, MAX_RPM);
    }
    

    public void setAcceleration(){
        // currentRPM = Mathf.Clamp(
        //         rearWheel.rpm * 3.6f, // Convert wheel RPM to engine RPM
        //         MIN_RPM,
        //         MAX_RPM
        //     );
        // float normalizedRPM = currentRPM / MAX_RPM;
        // float currentTorque = enginetorque.Evaluate(normalizedRPM) * getThrottleInput();
        //  float force = mass * value ;
        //  float torque = force * rearWheel.radius;

        // Calculate wheel angular velocity (rad/s)
        wheelAngularVelocity = rearWheel.rpm * (2f * Mathf.PI / 60f);


         float normalizedRPM = currentRPM / MAX_RPM;
        float baseTorque = enginetorque.Evaluate(normalizedRPM);
        float speed = GetVehicleSpeed();
        
        // Apply gear ratio and clutch engagement
        float gearRatio = gearRatios[currentGear - 1].ratio;
        float totalRatio = gearRatio * FINAL_DRIVE_RATIO;
        engineAngularVelocity = wheelAngularVelocity * totalRatio;

        currentPower = baseTorque * engineAngularVelocity;
        float maxTorqueAtRPM = Mathf.Min(
            baseTorque,
            (maxEnginePower / Mathf.Max(engineAngularVelocity, 0.1f))
        );
        float availableTorque = maxTorqueAtRPM * totalRatio * drivetrainEfficiency;
        
        // Apply throttle and clutch
        float finalTorque = availableTorque * getThrottleInput() * clutchEngagement;
        // float finalTorque = baseTorque * totalRatio * clutchEngagement * getThrottleInput();
        
        rearWheel.motorTorque = finalTorque;
        float force = finalTorque / rearWheel.radius;
        Vector3 forceDir = transform.forward * force * getThrottleInput();
        rb.AddRelativeForce(forceDir, ForceMode.Force);
        currentTorque = finalTorque;
        
        // Debug output
        Debug.Log($"Engine RPM: {currentRPM:F0}");
        Debug.Log($"Engine Power: {currentPower / 1000f:F2} kW");
        Debug.Log($"Wheel Torque: {finalTorque:F2} Nm"); 
        // rb.AddRelativeForce(Vector3.left * rearWheel.motorTorque); 
        Debug.Log($"baseTorque:{baseTorque}");
        Debug.Log($"FinalTorque:{finalTorque}");
    }
public float getThrottleInput(){
    if(Input.GetKey(KeyCode.L)){
        value += 0.01f;
        if(value > 1f){
            value = 1f;
        }
    }
    if(Input.GetKey(KeyCode.K)){
        value -= 0.01f;
        if(value < 0f){
            value = 0f;
        }
    }
    return value;
}

// public void setSteering(){
//         frontWheel.steerAngle= maxSteer;
//     }

//     public void UpdateHandle()
// 	{		
		
// 		handle.localRotation = Quaternion.Euler(handle.localRotation.eulerAngles.x, maxSteer, handle.localRotation.eulerAngles.z);
// 	}
    //  private void HandleSteering()
    // {
    //     // Get input (-1 to 1)
    //     float steerInput = maxSteer;
        
    //     // Calculate target angle based on input
    //     targetSteerAngle = steerInput * maxSteerAngle;
        
    //     // Smoothly interpolate current angle
    //     float speed = steerInput != 0 ? steeringSpeed : returnSpeed;
    //     currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, Time.deltaTime * speed);
        
    //     // Apply to wheel collider
    //     frontWheelCollider.steerAngle = currentSteerAngle;
    // }

    private void UpdateVisuals()
    {
        // Calculate normalized steering value
float normalizedSteer = currentSteerAngle / maxSteerAngle;

// Apply curved steering response
float curvedSteer = steeringCurve.Evaluate(Mathf.Abs(normalizedSteer)) * Mathf.Sign(normalizedSteer);

// Apply rotations
if (handlebarTransform != null)
{
    // Create a new Quaternion for handlebar rotation
    Quaternion handleRotation = Quaternion.Euler(0f, curvedSteer * maxHandlebarRotation, 0f);
    handlebarTransform.localRotation = handleRotation;
}

if (frontForkTransform != null)
{
    // Create a new Quaternion for fork rotation
    Quaternion forkRotation = Quaternion.Euler(0f, curvedSteer * maxForkRotation, 0f);
    frontForkTransform.localRotation = forkRotation;
}
    }


    // Helper function to visualize steering limits in editor
    private void OnDrawGizmosSelected()
    {
        if (frontWheelCollider != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 wheelPos = frontWheelCollider.transform.position;
            
            // Draw lines showing max steering angles
            Quaternion leftRot = Quaternion.Euler(0, -maxSteerAngle, 0);
            Quaternion rightRot = Quaternion.Euler(0, maxSteerAngle, 0);
            
            Gizmos.DrawRay(wheelPos, leftRot * frontWheelCollider.transform.forward * 2f);
            Gizmos.DrawRay(wheelPos, rightRot * frontWheelCollider.transform.forward * 2f);
        }
    }

    public void brake(){
        frontWheel.brakeTorque=0f;
        rearWheel.brakeTorque=0f;
        if(Input.GetKey(KeyCode.F)){
            frontWheel.brakeTorque= 9f;
        }
         if(Input.GetKey(KeyCode.R)){
            rearWheel.brakeTorque= 9f;
        }
    }
    

    // private void LayOnTurn()
	// {
	// 	Vector3 currentRot = transform.rotation.eulerAngles;

	// 	if (rb.linearVelocity.magnitude < 1)
	// 	{
	// 		layingammount = Mathf.LerpAngle(layingammount, 0f, 0.05f);		
	// 		transform.rotation = Quaternion.Euler(currentRot.x, currentRot.y, layingammount);
	// 		return;
	// 	}

	// 	if (currentSteeringAngle < 0.5f && currentSteeringAngle > -0.5  ) //We're stright
	// 	{
	// 		layingammount =  Mathf.LerpAngle(layingammount, 0f, leanSmoothing);			
	// 	}
	// 	else //We're turning
	// 	{
	// 		layingammount = Mathf.LerpAngle(layingammount, targetlayingAngle, leanSmoothing );		
	// 		rb.centerOfMass = new Vector3(rb.centerOfMass.x, COG.y, rb.centerOfMass.z);
	// 	}

	// 	transform.rotation = Quaternion.Euler(currentRot.x, currentRot.y, layingammount);
	// }

        private void UpdateLeanAndSteer()
    {
        float speed = rb.linearVelocity.magnitude * 3.6f;// Convert to km/h
        float steerInput = leanValue;
        Debug.Log(speed);

        
        // Calculate target lean based on speed and steering input
        float maxLeanAtSpeed = settings.leanVsSpeedCurve.Evaluate(speed);
        targetLeanAngle = steerInput * Mathf.Min(maxLeanAtSpeed, settings.maxLeanAngle);
        
        // Apply counter-steering effect at higher speeds
        float speedFactor = Mathf.Clamp01(speed * settings.speedInfluenceFactor / 100f);
        counterSteerAngle = -steerInput * settings.counterSteerFactor * speedFactor;
        
        // Smoothly interpolate current lean angle
        currentLeanAngle = Mathf.Lerp(
            currentLeanAngle, 
            targetLeanAngle, 
            Time.fixedDeltaTime / settings.leanResponseTime
        );
        
        // Calculate final steering angle including counter-steer
        float baseSteerAngle = steerInput * 30f; // Base steering angle
        currentSteerAngle = baseSteerAngle + counterSteerAngle;
        
        // Apply lean and steering
        ApplyLean();
        ApplySteering();
        
        // Apply gyroscopic stabilization
        ApplyGyroscopicEffect(speed);
    }
    
    private void ApplyLean()
    {
        // Get current rotation
        Vector3 currentRotation = transform.rotation.eulerAngles;
        
        // Apply lean angle while maintaining other rotation axes
        transform.rotation = Quaternion.Euler(
            currentRotation.x,
            currentRotation.y,
            -currentLeanAngle
        );
    }
    
    private void ApplySteering()
    {
        float speed = rb.linearVelocity.magnitude * 3.6f;
        if (frontWheel != null)
        {
            if(speed > 0f && speed <= 10f){
            frontWheel.steerAngle = currentSteerAngle * 1f;
        }
            if (speed > 10f && speed <= 20f){
                frontWheel.steerAngle = currentSteerAngle * 0.76f;

            }
            if (speed >20f && speed <= 30){
                frontWheel.steerAngle = currentSteerAngle * 0.5f;
            }
            if(speed > 30f && speed <= 50f){
                frontWheel.steerAngle = currentSteerAngle * 0.36f;

            }
            if (speed > 50f && speed <= 80f){
                frontWheel.steerAngle = currentSteerAngle * 0.24f;
            }
    }
    }
    private void ApplyGyroscopicEffect(float speed)
    {
        // Simple gyroscopic stabilization effect
        float stabilizationForce = speed * 0.01f;
        Vector3 stabilizingTorque = Vector3.down * -currentLeanAngle * stabilizationForce;
        rb.AddRelativeTorque(stabilizingTorque);
    }

     private float GetVehicleSpeed()
    {
        
        // Calculate average wheel speed in km/h
        float speed = 0f;
        speed += frontWheel.rpm * frontWheel.radius * 2f * Mathf.PI * 60f / 1000f;
        
        return speed;
    }
         private float GetFrontSpeed()
    {
        
        // Calculate average wheel speed in km/h
        float speed = 0f;
        speed += frontWheel.rpm;
        
        return speed;
    }
            private float GetRearSpeed()
    {
        
        // Calculate average wheel speed in km/h
        float speed = 0f;
        speed += rearWheel.rpm;
        
        return speed;
    }
    
    // Helper method to get current engine power
    public float GetCurrentPower()
    {
        float torque = enginetorque.Evaluate(currentRPM / MAX_RPM);
        return (torque * currentRPM) / 5252f; // HP calculation
    }
    
    // Debug information
    void OnGUI()
    {
        // GUI.Label(new Rect(10, 10, 200, 20), $"RPM: {currentRPM:F0}");
        // GUI.Label(new Rect(10, 30, 200, 20), $"Gear: {currentGear}");
        // GUI.Label(new Rect(10, 50, 200, 20), $"Speed: {GetVehicleSpeed():F1} km/h");
        // GUI.Label(new Rect(10, 70, 200, 20), $"Power: {GetCurrentPower():F1} hp");
        GUI.contentColor = Color.black;
                GUILayout.BeginArea(new Rect(10, 10, 300, 200));
                GUILayout.Label($"RearWheelRpm: {GetFrontSpeed()}");
                GUILayout.Label($"FrontWheelRpm: {GetFrontSpeed()}");
        GUILayout.Label($"Speed: {GetVehicleSpeed():F1} km/h");
        GUILayout.Label($"RPM: {currentRPM:F0}");
        GUILayout.Label($"Gear: {currentGear}");
        GUILayout.Label($"Engine Power: {currentPower / 1000f:F2} kW");
        GUILayout.Label($"Engine Torque: {currentTorque:F2} Nm");
        GUILayout.Label($"Wheel Angular Velocity: {wheelAngularVelocity:F2} rad/s");
        GUILayout.EndArea();
    }
      
}