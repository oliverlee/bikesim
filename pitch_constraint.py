#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Calculate the pitch needed to maintain contact between the front wheel and
ground.
"""
from sympy import simplify, symbols
from sympy import Symbol
from sympy.physics.mechanics import ReferenceFrame, Point
from sympy.physics.mechanics import msprint
from sympy.mpmath import findroot
from sympy.utilities import lambdify
from sympy.printing import ccode


## define coordinates
# phi: roll
# theta: pitch
# delta: steer
phi, theta, delta = symbols('φ θ δ')
# rR: rear radius
# rF: front radius
rR, rF = symbols('rR rF')
# cR: distance from rear wheel center to steer axis
# cF: distance from front wheel center to steer axis
# ls: steer axis separation
cR, cF, ls = symbols('cR cF ls')

benchmark_parameters = {
    rR: 0.3,
    rF: 0.35,
    cR: 0.9534570696121847,
    ls: 0.2676445084476887,
    cF: 0.0320714267276193,
}

## define reference frames
# N: inertial frame
# B: rear aseembly frame
# H: front assembly frame
N = ReferenceFrame('N')
B = N.orientnew('B', 'body', [0, phi, theta], 'zxy') # yaw is ignored
H = B.orientnew('H', 'axis', [delta, B.z])

## define points
# rear wheel/ground contact point
pP = Point('P')

# define unit vectors from rear/front wheel centers to ground
# along the wheel plane
R_z = ((B.y ^ N.z) ^ B.y).normalize()
F_z = ((H.y ^ N.z) ^ H.y).normalize()

# define rear wheel center point
pRs = pP.locatenew('R*', -rR*R_z)

# "top" of steer axis, point of SA closest to R*
# orthogonal projection of rear wheel center on steer axis
pRh = pRs.locatenew('R^', cR*B.x)

# orthogonal projection of front wheel center on steer axis
pFh = pRh.locatenew('S^', ls*B.z)

# front wheel center point
pFs = pFh.locatenew('S*', cF*H.x)

# front wheel/ground contact point
pQ = pFs.locatenew('Q', rF*F_z)

# N.z component of vector to pQ from pP
# this is our configuration constraint
f = simplify(pQ.pos_from(pP) & N.z)

print("f = {}\n".format(msprint(f)))

# calculate the derivative of f for use with newton-raphson
df = simplify(f.diff(theta))
print("df/dθ = {}\n".format(msprint(df)))

# constraint function for zero steer/lean configuration and 
# using the benchmark parameters
f0 = lambdify(theta, f.subs({phi: 0, delta: 0}).subs(benchmark_parameters))
df0 = lambdify(theta, df.subs({phi: 0, delta: 0}).subs(benchmark_parameters))

print("verifying constraint equations are correct")
print("for zero steer/lean, pitch should be pi/10")
findroot(f0, 0.3, solver="newton", tol=1e-8, verbose=True, df=df0)

c_sym = [Symbol('lean'), Symbol('pitch'), Symbol('steer')]
c_sym_dict = dict(zip([phi, theta, delta], c_sym))

fc = ccode(f.subs(c_sym_dict))
dfc = ccode(df.subs(c_sym_dict))

cs_math = {
    'cos': 'Math.Cos',
    'sin': 'Math.Sin',
    'pow': 'Math.Pow',
    'sqrt': 'Math.Sqrt'
}

fcs = fc
dfcs = dfc

for k, v in cs_math.items():
    fcs = fcs.replace(k, v)
    dfcs = dfcs.replace(k, v)
    
print(fcs)
print(dfcs)