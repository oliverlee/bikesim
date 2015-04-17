using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using UnityEngine;


public class Sensor {
#if STEER_TORQUE_INPUT
    public double steerTorque, wheelRate, sampleTime;
    public Sensor() : this(0.0, 0.0, 0.0) { }
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
#else
    public double steerAngle, steerRate, wheelRate, sampleTime;
    public Sensor() : this(0.0, 0.0, 0.0, 0.0) { }
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
#endif // STEER_TORQUE_INPUT
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
    private Matrix<double> MM;
    private Matrix<double> C1;
    private Matrix<double> K0;
    private Matrix<double> K2;

    private double v;
    private Matrix<double> Cv;
    private Matrix<double> Kv;
    private Vector<double> qd;

    private UdpSensor uSensor;

    public BicycleSimulator() {
        valid = true;
        sensor = new Sensor();
        state = new State();
        uSensor = new UdpSensor();
        uSensor.Start();
        feedbackTorque = 0.0;

        // matrix construction uses column major order
        MM = new DenseMatrix(2, 2, new double[] {
            M_phiphi, M_deltaphi,
            M_phidelta, M_deltadelta,
        });
        C1 = new DenseMatrix(2, 2, new double[] {
            C1_phiphi, C1_deltaphi,
            C1_phidelta, C1_deltadelta,
        });
        K0 = new DenseMatrix(2, 2, new double[] {
            K0_phiphi, K0_deltaphi,
            K0_phidelta, K0_deltadelta,
        });
        K2 = new DenseMatrix(2, 2, new double[] {
            K2_phiphi, K2_deltaphi,
            K2_phidelta, K2_deltadelta,
        });
    }

    public void Stop() {
        uSensor.Stop();
    }

#if STEER_TORQUE_INPUT
    public void UpdateSteerTorqueWheelRate(
        float steerTorque, float wheelRate, float samplePeriod) {
        sensor.Update(steerTorque, wheelRate, samplePeriod);
#else
    public void UpdateSteerAngleRateWheelRate(float steerAngle,
            float steerRate, float wheelRate, float samplePeriod) {
        sensor = uSensor.sensor;
        sensor.Update(steerAngle, steerRate, wheelRate, samplePeriod);
#endif // STEER_TORQUE_INPUT
        v = -sensor.wheelRate * rR;
        Kv = K(v);
        Cv = C(v);
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
        IntegrateState();
#if !STEER_TORQUE_INPUT
        EstimateFeedbackTorque();
#endif // !STEER_TORQUE_INPUT
        valid = true;
    }

    private Matrix<double> C(double v) {
        return v*C1;
    }

    private Matrix<double> K(double v) {
        return g*K0 + v*v*K2;
    }

    private void IntegrateState() {
        IntegratorFunction f = delegate(double t, Vector<double> y) {
            Vector<double> q = new DenseVector(
                    new double[] {y[0], y[1], y[2], y[3]});
#if STEER_TORQUE_INPUT
            qd = A*q + B*sensor.steerTorque;
#else
            qd = A*q;
#endif // STEER_TORQUE_INPUT

            return new DenseVector(new double[] {
                    qd[0],
#if STEER_TORQUE_INPUT
                    qd[1],
#else
                    0,
#endif // STEER_TORQUE_INPUT
                    qd[2],
#if STEER_TORQUE_INPUT
                    qd[3],
#else
                    0,
#endif // STEER_TORQUE_INPUT
                    v*y[3] + trail*y[1]*Math.Cos(steerAxisTilt)/wheelbase,
                    v*Math.Cos(y[4]),
                    v*Math.Sin(y[4]),
                    sensor.wheelRate});
        };

#if !STEER_TORQUE_INPUT
        // Use the most recent measured handlebar steer angle and rate
        state.steerRate = sensor.steerRate;
        state.steer = sensor.steerAngle;
#endif // !STEER_TORQUE_INPUT
        state.vector = Integrator.RungeKutta4(f, state.vector, 0,
                sensor.sampleTime);
    }

    private void EstimateFeedbackTorque() {
        feedbackTorque = -(MM[1, 0]*qd[0] +
                           Cv[1, 0]*qd[2] +
                           Cv[1, 1]*qd[3] +
                           Kv[1, 0]*state.lean +
                           Kv[1, 1]*state.steer);
    }

    public Matrix<double> A {
        get {
            Matrix<double> eye2 = SparseMatrix.CreateIdentity(2);
            Matrix<double> z2 = new SparseMatrix(2);
            Matrix<double> A1 = eye2.Append(z2);
            Matrix<double> A0 = -MM.Solve(Cv.Append(Kv));
            return A0.Stack(A1);
        }
    }

    public Vector<double> B {
        get {
            Vector<double> B0 = MM.Solve(new DenseVector(new double[] {0, 1}));
            return new DenseVector(new double[] {B0[0], B0[1], 0, 0});
        }
    }
}
