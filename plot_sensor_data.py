#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import os
import sys

import numpy as np
import matplotlib.pyplot as plt

def parse_input(path):
    data = []    
    with open(path) as f:       
        for line in f:
            words = line.strip().split(',')
            if len(words) != 4:
                continue
            data.append([float(val) for val in words])
    return np.array(data)
            

if __name__ == "__main__":
    usage = "{0} <filename>".format(__file__)
    
    if len(sys.argv) < 2 or not os.path.isfile(sys.argv[1]):
        print(usage)
        sys.exit(1)
        
    d = parse_input(sys.argv[1])
    index = range(d.shape[0])
    
    fig, ax0 = plt.subplots()
    ax0.set_xlabel('index')
    ax0.plot(index, d[:, 0], 'b', label='delta')
    ax0.set_ylabel('delta (rad)', color='b')

    ax1 = ax0.twinx()
    ax1.plot(index, d[:, 1], 'r', label='deltad')
    ax1.set_ylabel('deltad (rad/s)', color='r')
    plt.show()