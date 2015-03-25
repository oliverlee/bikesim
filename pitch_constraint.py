#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Calculate the pitch needed to maintain contact between the front wheel and
ground.
"""

from sympy import simplify, symbols
from sympy.physics.mechanics import ReferenceFrame, Point
from sympy.physics.mechanics import msprint
from sympy.utilities.codegen import codegen

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
R_z = (B.y ^ N.z) ^ B.y
F_z = (H.y ^ N.z) ^ H.z

# define rear wheel center point
pRs = pP.locatenew('R*', -rR*R_z)

# "top" of steer axis, point of SA closest to R*
# orthogonal projection of rear wheel center on steer axis
pRh = pRs.locatenew('R^', cR*B.x)

# orthogonal projection of rear wheel center on steer axis
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
