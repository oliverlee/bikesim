#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Simulate linearized benchmark bicycle given a test file from the Unity3d
bicycle simulator. Apply the torque input specified in the test file and
compare bicycle states.
"""
import os
import sys
import numpy as np
from numpy import linalg as la
from scipy import signal as sig
import control as ctrl
import matplotlib.pyplot as plt


REAR_RADIUS = 0.3 # benchmark bicycle rear wheel radius
M = np.array([
    [80.81722, 2.31941332208709],
    [2.31941332208709, 0.29784188199686]
])
C1 = np.array([
    [0, 33.86641391492494],
    [-0.85035641456978, 1.68540397397560]
])
K0 = np.array([
    [-80.95, -2.59951685249872],
    [-2.59951685249872, -0.80329488458618]
])
K2 = np.array([
    [0, 76.59734589573222],
    [0, 2.65431523794604]
])


class SimData(object):
    radius = REAR_RADIUS    
    
    def __init__(self, omega, field_map, data):
        self.g = 9.81;
        self.v = -omega*self.radius
        self.map = field_map
        self.data = data


def parse_input(path):
    cols = -1
    start_time = -1
    field_map = {} # maps field names to column index
    omega = -1 # constant wheel rate (forward speed) of the bicycle
    
    idx_st = -1
    idx_wr = -1       
    
    values = []
    with open(path) as f:       
        for line in f:
            words = line.split()
            if cols > 0 and cols != len(words):
                print("Number of columns is inconsistent")
                print(line)
                sys.exit(1)
    
            try:
                v = [float(w) for w in words]
                
                # check if the torque disturbance occurs or if we have already
                # recorded the time of the disturbance
                if start_time >= 0 or v[idx_st]:
                    if start_time < 0:
                        start_time = v[0]
                        omega = v[idx_wr]
                        
                    # verify that wheelrate remains constant
                    if omega != v[idx_wr]:
                        print("Wheel rate is inconsistent")
                        sys.exit(1)
                    
                    # use the start time of the disturbance
                    values += [v[0] - start_time] + v[1:]
                    
            except ValueError:
                # line with non-numeric words, assumed to be the header
                cols = len(words)    
                field_map = dict(zip(words, range(cols)))
                idx_st = field_map["steertorque"] 
                idx_wr = field_map["wheelrate"]
        
    rows = len(values)/cols
    # wheel rate excluded from data
    data = np.array(values).reshape((rows, cols))
    return SimData(omega, field_map, data)
        
                
def create_system(data):
    C = data.v*C1
    K = data.g*K0 + data.v*data.v*K2
    
    A = np.vstack((la.solve(-M, np.hstack((C, K))),
                   np.hstack((np.eye(2), np.zeros((2, 2))))))
    B = np.vstack((la.solve(M, np.array([[0], [1]])),
                   np.array([[0], [0]])))
    # sys = sig.lti(A, B, np.array([1, 0, 0, 0]), 0)
    sys = ctrl.matlab.StateSpace(A, B, np.array([1, 0, 0, 0]), 0)
    return sys


def rungekutta4(f, y0, t0, h, u=0):
    k1 = f(t0, y0, u)
    k2 = f(t0 + h/2, y0 + h/2*k1, u)
    k3 = f(t0 + h/2, y0 + h/2*k2, u)
    k4 = f(t0 + h, y0 + h*k3, u)
    return y0 + h/6*(k1 + 2*k2 + 2*k3 + k4)


if __name__ == "__main__":
    usage = "{0} <filename>".format(__file__)
    
    if len(sys.argv) < 2 or not os.path.isfile(sys.argv[1]):
        print(usage)
        sys.exit(1)
    
    d = parse_input(sys.argv[1])
    sys = create_system(d)

    states = ['leanrate', 'steerrate', 'lean', 'steer']
    state_idx = [d.map[x] for x in states]

    t = d.data[:, d.map['time']]
    u = d.data[:, d.map['steertorque']]
    x = d.data[:, state_idx]

    # t_out, y_out, x_out = sig.lsim(sys, u, t)
    integrate_type = 'rk4'
    if integrate_type == 'control':
        y_out, t_out, x_out = ctrl.matlab.lsim(sys, u, t)
    elif integrate_type == 'rk4':
        f = lambda t, y, u: sys.A*y + sys.B*u
        x_out = np.matrix(np.zeros(x.shape))
        h = np.diff(t)
        h = np.insert(h, 0, h[0])
        for i, (ti, hi, ui) in enumerate(zip(t, h, u)):
            if i == 0:
                y = np.zeros((1, 4))
            else:
                y = x_out[i-1, :]
            x_out[i, :] = rungekutta4(f, y.T, ti, hi, ui).T

    if np.allclose(x_out, x):
        print('state is similar for both simulations')
    else:
        print('state does not match between simulations')

    N = x.shape[1]
    fig, axar = plt.subplots(N, sharex=True)
    axar[-1].set_xlabel('time (s)')

    for i in range(N):
        axar[i].plot(t, x[:, i], label='unity')
        axar[i].plot(t, x_out[:, i], label='lsim')
        axar[i].set_ylabel(states[i])

    # hidden plot to display legend
    axe = fig.add_axes([.9, 0.4, 0.2, 0.2])
    lp1 = axe.plot([0], label='unity')
    lp2 = axe.plot([0], label='python')
    l = plt.legend(loc='center')
    axe.get_xaxis().set_visible(False)
    axe.get_yaxis().set_visible(False)
    axe.set_frame_on(False)

    plt.show()
    
    print("A:")
    print(sys.A)
    print("B:")
    print(sys.B)