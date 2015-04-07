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
    public const double rR = 0.3; // m

    // bicycle state
    private bool valid; // if state is valid
    private Sensor sensor;
    private State state;
    private double feedbackTorque;


    public BicycleSimulator() {
        valid = true;
        sensor = new Sensor();
        state = new State();
        feedbackTorque = 0.0;
    }

    public void UpdateSteerAngleRateWheelRate(
        float steerAngle, float steerRate, float wheelRate, float samplePeriod) {
        sensor.Update(steerAngle, steerRate, wheelRate, samplePeriod);
        state.steerRate = steerRate;
        state.steer = steerAngle;
        valid = false;
    }

    public double GetFeedbackTorque() {
        if (!valid) {
            Simulate();
        }
        return feedbackTorque;
    }

    public State GetState() {
        if (!valid) {
            Simulate();
        }
        return state;
    }

    private void Simulate() {
        double v = -sensor.wheelRate * rR;
        IntegrateState(v);
        EstimateFeedbackTorque(v);
        valid = true;
    }

    // roll equation and kinematics
    private double phidd(State s, double v) {
        return -(v*C1_phidelta*s.steerRate + (K0_phidelta +
                    v*v*K2_phidelta)*s.steer - v*C1_phiphi*s.leanRate -
                (K0_phiphi + v*v*K2_phiphi)*s.lean)/M_phiphi;
    }
//
//    private double psid(State s, double v) {
//        return (v*s.steer + trail*s.steerRate)*Math.Cos(steerAxisTilt)/wheelbase;
//    }
//
//    private static double xd(State s, double v) {
//        return v*Math.Cos(s.yaw);
//    }
//
//    private static double yd(State s, double v) {
//        return v*Math.Sin(s.yaw);
//    }

    private void IntegrateState(double v) {
        IntegratorFunction f = delegate(double t, Vector<double> y) {
            return new DenseVector(new double[] {
                    -(v*C1_phidelta*y[1] + (K0_phidelta + v*v*K2_phidelta)*y[3]
                        - v*C1_phiphi*y[0] - (K0_phiphi +
                            v*v*K2_phiphi)*y[2])/M_phiphi,
                    0, // steer rate is controlled by physical handlebar
                    y[0], // lean rate
                    0, // steer is controller by physical handlebar
                    v*y[3] + trail*y[1]*Math.Cos(steerAxisTilt)/wheelbase,
                    v*Math.Cos(y[4]),
                    v*Math.Sin(y[4]),
                    sensor.wheelRate});
        };

        state.vector = Integrator.RungeKutta4(f, state.vector, 0,
                sensor.sampleTime);
    }

    private void EstimateFeedbackTorque(double v) {
        feedbackTorque = -( M_deltaphi*phidd(state, v) +
                v*C1_deltaphi*state.lean + v*C1_deltadelta*state.steer +
                (K0_deltaphi + v*v*K2_deltaphi)*state.lean + (K0_deltaphi +
                    v*v*K2_deltadelta)*state.steer);
        UnityEngine.Debug.Log(feedbackTorque);
    }
}
