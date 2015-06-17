#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import abc
import itertools
import collections
import numpy as np
import matplotlib.pyplot as plt


class Transducer(metaclass=abc.ABCMeta):

    def __new__(cls, name):
        def prop(x):
            return property(lambda self: self.get_field(x))
        for f in cls._fields:
            setattr(cls, f, prop(f))
        return super().__new__(cls)
        #return super(Transducer, cls).__new__(cls, name)

    def __init__(self, name):
        self._name = name
        self._sample_size = len(self.__class__._fields)
        self._data_str = self._sample_size * ['0']
        self._time_str = ['0']
        self._time = np.zeros(0)
        self._data = np.zeros(0)

    def put(self, time, data):
        if len(data) != self._sample_size:
            raise ValueError
        self._time_str.append(time)
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
    sensor = Sensor('sensor')
    actuator = Actuator('actuator')
    with open(path) as f:
        for line in f:
            if line.startswith('Simulator log'):
                continue
            time, data = line.strip().split(':')
            data = [d.strip() for d in data.split(',')]
            if len(data) == 4:
                sensor.put(time, data)
            elif len(data) == 1:
                actuator.put(time, data)
    sensor.update()
    actuator.update()
    return sensor, actuator


def plot_timeinfo(transducer, max_dt=None):
    name = transducer.name
    dt = transducer.dt
    t = transducer.time[1:] # remove the first element to match dt.shape
    if max_dt is not None:
        # set a threshold for maximum dt to consider
        subset = dt < max_dt
        dt = dt[subset]
        t = t[subset]
    std = np.std(dt)
    med = np.median(dt)
    print('{} time median = {:0.6f} s, std = {:0.6f}'.format(name, med, std))
    fig, ax = plt.subplots(1, 2)

    # plot dt vs time
    ax[0].set_xlabel('time [s]')
    ax[0].set_ylabel('dt [s]')
    ax[0].set_ylim([min(0, dt.min()), max(med*1.5, dt.max())])
    ax[0].set_xlim([t[0], t[-1]])
    l1 = ax[0].plot(t, dt)
    l2 = ax[0].plot(t[[0, -1]], 2*[med], 'r-',
                    t[[0, -1]], 2*[med + 2*std], 'r--',
                    t[[0, -1]], 2*[med - 2*std], 'r--')
    plt.figlegend((l1[0], l2[0]), (name, 'median'), loc='upper center')

    # plot histogram of dt
    y, bin_edges = np.histogram(dt)
    bin_centers = 0.5*(bin_edges[1:] + bin_edges[:-1])
    bin_widths = 0.8*(bin_edges[1:] - bin_edges[:-1])
    rects = ax[1].bar(bin_centers, y, bin_widths, align='center')
    ax[1].set_xlabel('dt [s]')
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

