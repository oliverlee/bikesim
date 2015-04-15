using System;
using MathNet.Numerics.LinearAlgebra;

public delegate Vector<double> IntegratorFunction(double t, Vector<double> y);

public class Integrator {
    public static Vector<double> RungeKutta4(IntegratorFunction f,
            Vector<double> y0, double t0, double h) {
        Vector<double> k1 = f(t0, y0);
        Vector<double> k2 = f(t0 + h/2, y0 + h/2*k1);
        Vector<double> k3 = f(t0 + h/2, y0 + h/2*k2);
        Vector<double> k4 = f(t0 + h, y0 + h*k3);
        return y0 + h/6*(k1 + 2*k2 + 2*k3 + k4);
    }
}