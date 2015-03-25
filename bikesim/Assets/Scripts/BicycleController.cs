using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class VizState {
	public float x, y, pitch, lean, yaw, thetaR, thetaF, steer;
	public VizState(QState q) {
		x = System.Convert.ToSingle(q.x);
		y = System.Convert.ToSingle(q.y);
		pitch = System.Convert.ToSingle(q.pitch);
		lean = System.Convert.ToSingle(q.lean);
		yaw = System.Convert.ToSingle(q.yaw);
		thetaR = System.Convert.ToSingle(q.thetaR);
		thetaF = System.Convert.ToSingle(q.thetaF);
		steer = System.Convert.ToSingle(q.steer);
	}
	public VizState() {
		x = 0.0f;
		y = 0.0f;
		pitch = 0.0f;
		lean = 0.0f;
		yaw = 0.0f;
		thetaR = 0.0f;
		thetaF = 0.0f;
		steer = 0.0f;
	}
	public void SetState(QState q) {
		x = System.Convert.ToSingle(q.x);
		y = System.Convert.ToSingle(q.y);
		pitch = System.Convert.ToSingle(q.pitch);
		lean = System.Convert.ToSingle(q.lean);
		yaw = System.Convert.ToSingle(q.yaw);
		thetaR = System.Convert.ToSingle(q.thetaR);
		thetaF = System.Convert.ToSingle(q.thetaF);
		steer = System.Convert.ToSingle(q.steer);
	}
}

public class BicycleController : MonoBehaviour {
	public GameObject rearWheel;
	public GameObject rearFrame;
	public GameObject frontFrame;
	public GameObject frontWheel;
	public Text sensorInfo;
	public Text stateInfo;
	
	// visualization parameters
	private float rR; // m
	private float rF; // m
	private float cR; // m
	private float ls; // m
	private float cF; // m

	// dependent parameters
	private float headAngle; // rad
	//private float trail; // m
	//private float wheelbase; // m

	// sensor measurements
	private float wheelRate; // rad/s
	private float steerRate; // rad/s
	private float steer; // rad
	
	private VizState q;
	private BicycleSimulator sim;

	// Setup the Bicycle Configuration
	void Start () {
		// Set component sizes
		// Values from Peterson dissertation
		rR = 0.3f;
		rF = 0.35f;
		cR = 0.9534570696121847f;
		ls = 0.2676445084476887f;
		cF = 0.0320714267276193f;

		headAngle = CalculateNominalPitch();

		// sensor measurements
		wheelRate = 0.0f;
		steerRate = 0.0f;
		steer = 0.0f;

		q = new VizState();
		q.pitch = headAngle;
		SetBicycleTransform(q);
		sim = new BicycleSimulator();
	}

	void FixedUpdate () {
		wheelRate -= Input.GetAxis("Vertical");
		steerRate = Input.GetAxis("Horizontal");
		steer += steerRate * Time.deltaTime;

		sim.UpdateSteerAngleRateWheelRate(steer, steerRate, wheelRate, Time.deltaTime);
		double T_f = sim.GetFeedbackTorque();
	}

	void Update() {
        float pitch = q.pitch; // save previous pitch value
		q.SetState(sim.GetQState());
        q.pitch = pitch; // restore previous pitch value
        SetConstraintPitch(q); // calculate pitch to satify constraints

		SetBicycleTransform(q);
		sensorInfo.text = System.String.Format(
			"wheelrate: {0}\nsteerrate: {1}\nsteer: {2}",
			wheelRate, steerRate, steer);
		stateInfo.text = System.String.Format(
			"x: {0}\ny: {1}\nlean: {2}\nyaw: {3}\nsteer: {4}",
			q.x, q.y, q.lean, q.yaw, q.steer);
	}

	void SetBicycleTransform(VizState q) {
		// Update x and y positions of the rear wheel contact, yaw and lean of the rear frame
		// by modifying the transform of root bicycle game object.
		// As Euler angles use the zxy ordering in Unity, yaw will be applied before lean.
		// In Unity, a series of rotations R1*R2 will apply R2 after R1.
		transform.localPosition = new Vector3(q.x, 0.0f, -q.y); // y and z axes are switched
		transform.localRotation = Quaternion.Euler(90.0f, 0, 0) *
			Quaternion.Euler(-Mathf.Rad2Deg*q.lean, 0.0f, -Mathf.Rad2Deg*q.yaw);
		Debug.Log(transform.position);

		// All wheel and frame local transforms are with respect to the container game or lean frame
		//   Update rear wheel angle
		rearWheel.transform.localRotation = Quaternion.Euler(0.0f, Mathf.Rad2Deg*q.thetaR, 0.0f);
		rearWheel.transform.localPosition = new Vector3(0.0f, 0.0f, -rR);

		//   Update pitch of the rear frame
		rearFrame.transform.localRotation = Quaternion.Euler(0.0f, Mathf.Rad2Deg*q.pitch, 0.0f);
		//   Set rear frame origin at rear wheel position, then translate alone the frame axis
		rearFrame.transform.localPosition = rearWheel.transform.localPosition;
		rearFrame.transform.Translate(new Vector3(cR/2, 0.0f, 0.0f), rearFrame.transform);

		//   Update pitch and steer of the front frame
		frontFrame.transform.localRotation = 
			Quaternion.Euler(0.0f, Mathf.Rad2Deg*q.pitch, -Mathf.Rad2Deg*q.steer);
		//   Set front frame origin at rear frame position, then translate alone the frame axes
		frontFrame.transform.localPosition = rearFrame.transform.localPosition;
		frontFrame.transform.Translate(
			new Vector3(cR/2, 0.0f, ls/2), rearFrame.transform);

		//   Update front wheel angle
		frontWheel.transform.localRotation = frontFrame.transform.localRotation*
			Quaternion.Euler(0.0f, Mathf.Rad2Deg*q.thetaF, 0.0f);
		frontWheel.transform.localPosition = frontFrame.transform.localPosition;
		frontWheel.transform.Translate(
			new Vector3(cF, 0.0f, ls/2), frontFrame.transform);
	}

	private float CalculateNominalPitch() {
		return Mathf.Atan(ls / (cR + cF));
	}

	private void SetConstraintPitch(VizState q) {
		Func<double, double> f0 = pitch => f(q.lean, pitch, q.steer);
		Func<double, double> df0 = pitch => df(q.lean, pitch, q.steer);

		// We only calculate pitch for visualization so accuracy can be low.
        double p = MathNet.Numerics.RootFinding.NewtonRaphson.FindRootNearGuess(f0, df0,
                q.pitch, 0, Math.PI/2, 1e-4, 10);
        q.pitch = System.Convert.ToSingle(p);
	}

    // pitch angle configuration constraint
    private double f(double lean, double pitch, double steer) {
        return cF*Math.Sin(steer)*Math.Sin(lean) - cF*Math.Sin(pitch)*Math.Cos(steer)*Math.Cos(lean) -
            cR*Math.Sin(pitch)*Math.Cos(lean) + ls*Math.Cos(pitch)*Math.Cos(lean) -
            rF*Math.Sin(steer)*Math.Sin(pitch)*Math.Cos(pitch)*Math.Pow(Math.Cos(lean), 2) -
            rF*Math.Sin(lean)*Math.Cos(steer)*Math.Cos(pitch)*Math.Cos(lean) - rR*Math.Pow(Math.Cos(lean),
                    2);
    }

    // derivative of f wrt to pitch
    private double df(double lean, double pitch, double steer) {
        return (-cF*Math.Cos(steer)*Math.Cos(pitch) - cR*Math.Cos(pitch) - ls*Math.Sin(pitch) -
                2*rF*Math.Sin(steer)*Math.Pow(Math.Cos(pitch), 2)*Math.Cos(lean) +
                rF*Math.Sin(steer)*Math.Cos(lean) +
                rF*Math.Sin(pitch)*Math.Sin(lean)*Math.Cos(steer))*Math.Cos(lean);
    }
}
