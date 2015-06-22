#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import abc
import collections
import pickle
import marshal
import time
import numpy as np
import matplotlib.pyplot as plt

import sys
sys.path.append('../comm')
from serial_udp_bridge import Sample


class Transducer(metaclass=abc.ABCMeta):

    def __new__(cls, name, filepath):
        def prop(x):
            return property(lambda self: self.get_field(x))
        for f in cls._fields:
            setattr(cls, f, prop(f))
        return super().__new__(cls)
        #return super(Transducer, cls).__new__(cls)

    def __init__(self, name, filepath=''):
        self._name = name
        self._filename = filepath
        self._sample_size = len(self.__class__._fields)
        self._data_str = self._sample_size * ['0']
        self._time_str = ['0']
        self._time = np.zeros(0)
        self._data = np.zeros(0)
        self._start_time = None
        self._end_time = None

    def put(self, timestamp, data):
        if len(data) != self._sample_size:
            raise ValueError
        self._time_str.append(timestamp)
        self._data_str.extend(data)

    def update(self):
        self._update_time()
        self._update_data()
        self._update_dt()

    def get_field(self, fieldname):
        if len(self._time) == 0:
            print('update() must be called after data is added')
            return None
        if fieldname == 'time':
            return self._time
        idx = self._fields.index(fieldname)
        return self._data[:, idx]

    @property
    def name(self):
        return self._name

    @property
    def filepath(self):
        return self._filename

    @property
    def start_time(self):
        return self._start_time

    @property
    def end_time(self):
        return self._end_time

    @property
    def shape(self):
        return (len(self._time), self._sample_size)

    @property
    def fields(self):
        return self._fields

    @property
    def data(self):
        return self._data

    @property
    def time(self):
        return self._time

    @property
    def dt(self):
        return self._dt

    def _update_time(self):
        self._time = np.array(self._time_str, np.float32)

    def _update_data(self):
        self._data = np.array(self._data_str, np.float32).reshape(
                (self._time.shape[0], self._sample_size))

    def _update_dt(self):
        # don't include the last element
        self._dt = (np.roll(self.time, -1) - self.time)[:-1]


class Sensor(Transducer):
    _fields = ('delta', 'deltad', 'cadence', 'brake')


class Actuator(Transducer):
    _fields = ('torque',)


def parse_log(path):
    sensor = Sensor('sensor', path)
    actuator = Actuator('actuator', path)
    sample = Sample()
    with open(path, 'rb') as f:
        while True:
            try:
                #p = pickle.load(f)
                p = marshal.load(f)
            except EOFError:
                break

            if isinstance(p, int): #time.struct_time):
                p = time.gmtime(p)
                if sensor.start_time is None:
                    sensor._start_time = p
                    actuator._start_time = p
                elif sensor.end_time is None:
                    sensor._end_time = p
                    actuator._end_time = p
                else:
                    print('time.struct_time found but transducer start and '
                          'end times already set.')
            else:
                timestamp, data = p
                if isinstance(data, list):
                    sensor.put(timestamp, data)
                elif isinstance(data, float):
                    actuator.put(timestamp, (data,))
                else:
                    print('Unpickled unexpected type: {}'.format(type(data)))
    sensor.update()
    actuator.update()
    return sensor, actuator


def plot_timeinfo(transducer, max_dt=None):
    name = transducer.name
    dt = transducer.dt * 1000
    t = transducer.time[1:] # remove the first element to match dt.shape
    if max_dt is not None:
        # set a threshold for maximum dt to consider
        subset = dt < max_dt
        dt = dt[subset]
        t = t[subset]
    std = np.std(dt)
    avg = np.mean(dt)
    print('{} time mean = {:0.6f} s, std = {:0.6f}'.format(name, avg, std))
    if np.isnan(avg) or np.isnan(std):
        print('No data to plot')
        return None
    fig, ax = plt.subplots(1, 2)

    # plot dt vs time
    ax[0].set_xlabel('time [s]')
    ax[0].set_ylabel('dt [ms]')
    ax[0].set_ylim([min(0, dt.min()), max(avg*1.5, dt.max())])
    ax[0].set_xlim([t[0], t[-1]])
    l1 = ax[0].plot(t, dt)
    l2 = ax[0].plot(t[[0, -1]], 2*[avg], 'r-',
                    t[[0, -1]], 2*[avg + 2*std], 'r--',
                    t[[0, -1]], 2*[avg - 2*std], 'r--')
    plt.figlegend((l1[0], l2[0]), (name, 'mean'), loc='upper left')
    fig.suptitle('{} - {}'.format(transducer.filepath, name))

    # plot histogram of dt
    y, bin_edges = np.histogram(dt, 50)
    bin_centers = 0.5*(bin_edges[1:] + bin_edges[:-1])
    bin_widths = 0.8*(bin_edges[1:] - bin_edges[:-1])
    rects = ax[1].bar(bin_centers, y, bin_widths, align='center')
    ax[1].set_xlabel('dt [ms]')
    ax[1].set_ylabel('count')

    #register callbacks
    hd = HistDisplay(t, dt, rects, bin_edges)
    ax[0].callbacks.connect('xlim_changed', hd.ax_update)
    return fig, ax, hd


#def plot_hist(transducers, max_dt=None):
#    if isinstance(transducers, collections.abc.Iterable):
#        times = (t.dt for t in transducers)
#        x = (dt[dt<max_dt] if max_dt is not None else dt for dt in times)
#    else:
#        dt = transducers.dt
#        x = dt[dt<max_dt] if max_dt is not None else dt


class HistDisplay(object):
    def __init__(self, t, dt, rects, bins):
        self.t = t
        self.dt = dt
        self.rects = rects
        self.bins = bins
        self.ax = rects[0].get_axes()

    def __call__(self, x):
        y, _ = np.histogram(x, self.bins)
        return y

    def ax_update(self, ax):
        lb, ub = ax.get_xlim()
        y = self.__call__(self.dt[(self.t >= lb) & (self.t <= ub)])
        for rect, h in zip(self.rects, y):
            rect.set_height(h)
        ax.figure.canvas.draw()
        self.ax.relim()
        self.ax.autoscale_view()

