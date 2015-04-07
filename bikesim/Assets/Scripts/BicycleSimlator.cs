using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;


public class Sensor {
    public double steerTorque, wheelRate, sampleTime;
    public Sensor() : this(0.0f, 0.0, 0.0f) { }
    public Sensor(double torque, double thetad, double dt) {
        steerTorque = torque;
        wheelRate = thetad;
        sampleTime = dt;
    }
    public void Update(double tau, double thetad, double dt) {
        steerTorque = tau;
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
    private Matrix<double> InvMM;


    public BicycleSimulator() {
        valid = true;
        sensor = new Sensor();
        state = new State();
        feedbackTorque = 0.0;
        InvMM = new DenseMatrix(2, 2, new double[] { // column major
                    M_phiphi, M_deltaphi,
                    M_phidelta, M_deltadelta,
                }).Inverse();
    }

    public void UpdateSteerTorqueWheelRate(
        float steerTorque, float wheelRate, float samplePeriod) {
        sensor.Update(steerTorque, wheelRate, samplePeriod);
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
        valid = true;
    }

    private Matrix<double> C(double v) {
        return v*(new DenseMatrix(2, 2, new double[] { // column major
                    C1_phiphi, C1_deltaphi,
                    C1_phidelta, C1_deltadelta,
                }));
    }

    private Matrix<double> K(double v) {
        // matrix construction uses column major order
        Matrix<double> K0 = new DenseMatrix(2, 2, new double[] {
                    K0_phiphi, K0_deltaphi,
                    K0_phidelta, K0_deltadelta,
                });
        Matrix<double> K2 = new DenseMatrix(2, 2, new double[] {
                    K2_phiphi, K2_deltaphi,
                    K2_phidelta, K2_deltadelta,
                });
        return K0 + v*v*K2;
    }

    private void IntegrateState(double v) {
        Matrix<double> Cv = C(v);
        Matrix<double> Kv = K(v);
        Vector<double> u = new DenseVector(new double[] {
                0.0, sensor.steerTorque});

        IntegratorFunction f = delegate(double t, Vector<double> y) {
            Vector<double> q = new DenseVector(new double[] {y[2], y[3]});
            Vector<double> qd = new DenseVector(new double[] {y[0], y[1]});
            Vector<double> qdd = InvMM*(u - Cv*qd - Kv*q);

            return new DenseVector(new double[] {
                    qdd[0],
                    qdd[1],
                    y[0],
                    y[1],
                    v*y[3] + trail*y[1]*Math.Cos(steerAxisTilt)/wheelbase,
                    v*Math.Cos(y[4]),
                    v*Math.Sin(y[4]),
                    sensor.wheelRate});
        };

        state.vector = Integrator.RungeKutta4(f, state.vector, 0,
                sensor.sampleTime);
    }
}
