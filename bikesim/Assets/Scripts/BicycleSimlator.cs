using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;


public class Sensor {
    public double steerAngle, steerRate, wheelRate, sampleTime;
    public Sensor() : this(0.0f, 0.0f, 0.0, 0.0f) { }
    public Sensor(double delta, double deltad, double thetad, double dt) {
        steerAngle = delta;
        steerRate = deltad;
        wheelRate = thetad;
        sampleTime = dt;
    }
    public void Update(double delta, double deltad, double thetad, double dt) {
        steerAngle = delta;
        steerRate = deltad;
        wheelRate = thetad;
        sampleTime = dt;
    }
}

public class State {
    public double leanRate, steerRate, lean, steer, yaw, x, y, wheelAngle;
    public State() : this (0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0) { }
    public State(double phid, double deltad, double phi, double delta,
                 double psi, double x, double y, double thetaR) {
        leanRate = phid;
        steerRate = deltad;
        lean = phi;
        steer = delta;
        yaw = psi;
        this.x = x;
        this.y = y;
        wheelAngle = thetaR;
    }
    public Vector<double> vector {
        get {
            return new DenseVector(new double[] {leanRate, steerRate, lean,
                    steer, yaw, x, y, wheelAngle});
        }
        set {
            leanRate = value[0];
            steerRate = value[1];
            lean = value[2];
            steer = value[3];
            yaw = value[4];
            x = value[5];
            y = value[6];
            wheelAngle = value[7];
        }
    }
}

// This class implements the bicycle simulator equations of motion as described
// in Schwab, Recuero 2013.
public class BicycleSimulator {

	// parameters from Meijaard et al. 2007
	private const double g = 9.81;
	private const double M_phiphi = 80.81722;
	private const double M_phidelta = 2.31941332208709;
	private const double M_deltaphi = M_phidelta;
	private const double M_deltadelta = 0.29784188199686;
	private const double C1_phiphi = 0;
	private const double C1_phidelta = 33.86641391492494;
	private const double C1_deltaphi = -0.85035641456978;
	private const double C1_deltadelta = 1.68540397397560;
	private const double K0_phiphi = -80.95;
	private const double K0_phidelta = -2.59951685249872;
	private const double K0_deltaphi = K0_phidelta;
	private const double K0_deltadelta = -0.80329488458618;
	private const double K2_phiphi = 0;
	private const double K2_phidelta = 76.59734589573222;
	private const double K2_deltaphi = 0;
	private const double K2_deltadelta = 2.65431523794604;

	public const double steerAxisTilt = Math.PI/10; // rad
	public const double trail = 0.08; // m
	public const double wheelbase = 1.02; // m
	public const double r_R = 0.3; // m

	// bicycle state
	private QState q;
	private UState u;
	private double v;
	private double vSq;
	bool outputValid;

	private double leanAccel;
	private double feedbackTorque;
	private double timeStep;

	public BicycleSimulator() {
		q = new QState();
		u = new UState();

		// Let the pitch of the rear frame to be equal to the 
		// steer axis tilt in the nominal configuration.
		// q.pitch = steerAxisTilt; // NOTE: pitch is only used for visualization
		outputValid = true;
	}

	public void UpdateSteerAngleRateWheelRate(
		float steerAngle, float steerRate, float wheelRate, float samplePeriod) {
		q.steer = steerAngle;
		u.steer = steerRate;
		u.thetaR = wheelRate;

		// Measurements have just been updated and the previously computed state
		// is no longer valid.
		outputValid = false;
		v = -wheelRate*r_R;
		vSq = v*v;
		timeStep = samplePeriod;
	}
	
	public double GetFeedbackTorque() {
		if (!outputValid) {
			SimulateTimeStep();
		}
		return feedbackTorque;
	}
	
	public QState GetQState() {
		if (!outputValid) {
			SimulateTimeStep();
		}
		return q;
	}

	private void UpdateLeanStates() {
		// Use previously computed lean angle/rate for acceleration calculation.
		// Integrate to update lean angle/rate.
		leanAccel = -(v*C1_phidelta*u.steer + 
		              (g*K0_phidelta + vSq*K2_phidelta)*q.steer + 
		              v*C1_phidelta*u.lean + 
		              (g*K0_phidelta + vSq*K2_phidelta)*q.lean)/M_phiphi;

		q.lean += u.lean * timeStep; // update lean angle by integrating previous lean rate
		u.lean += leanAccel * timeStep; // update lean rate by integrating lean accel
	}

	private void EstimateFeedbackTorque() {
		feedbackTorque = -(
			M_deltaphi*leanAccel + v*C1_deltaphi*u.lean + v*C1_deltadelta*u.steer + 
			(g*K0_deltaphi + vSq*K2_deltaphi)*q.lean + (g*K0_deltaphi + vSq*K2_deltadelta)*q.steer);
	}

	private void UpdateYawStates() {
		u.yaw = (v*q.steer + trail*u.steer)/wheelbase * Math.Cos(steerAxisTilt);

		// integration step
		q.yaw += u.yaw * timeStep;
	}

	private void UpdateXYStates() {
		u.x = v*Math.Cos(q.yaw);
		u.y = v*Math.Sin(q.yaw);
	
		// integration step
		q.x += u.x * timeStep;
		q.y += u.y * timeStep;
	}

	private void UpdateWheelStates() {
		q.thetaR += u.thetaR * timeStep;
		q.thetaF += u.thetaR * timeStep; // TODO: calculate front wheel rate and use in integration
	}

	private void SimulateTimeStep() {
		UpdateLeanStates();
		UpdateYawStates();
		UpdateXYStates();
		UpdateWheelStates();
		EstimateFeedbackTorque();
		outputValid = true;
	}
}
