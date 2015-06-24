#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import abc
import bisect
import collections
import marshal
import time

import numpy as np
import matplotlib.pyplot as plt
import matplotlib.cm as cm
import seaborn as sns

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
        self._convert_rad_deg()

    def get_field(self, fieldname):
        if len(self._time) == 0:
            print('update() must be called after data is added')
            return None
        if fieldname == 'time':
            return self._time
        idx = self._fields.index(fieldname)
        return self._data[:, idx]

    def get_timerange_indices(self, timerange):
        tmin, tmax = timerange
        t = self._time
        return (t >= tmin) & (t <= tmax)

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
        # ignore first sample
        self._time = np.array(self._time_str[1:], np.float32)

    def _update_data(self):
        self._data = np.array(self._data_str[self._sample_size:],
                              np.float32).reshape((self._time.shape[0],
                                                   elf._sample_size))

    def _update_dt(self):
        # don't include the last element
        self._dt = (np.roll(self.time, -1) - self.time)[:-1]

    def _convert_rad_deg(self):
        for i, f in enumerate(self._fields):
            if units(f).startswith('deg'):
                self._data[:, i] = 180/np.pi*self._data[:, i]


class Sensor(Transducer):
    _fields = ('delta', 'deltad', 'cadence', 'brake')


class Actuator(Transducer):
    _fields = ('torque', 'phi')


class Log(object):
    def __init__(self, path):
        sensor, actuator = parse_log(path)
        self._filepath = path
        seif._logname = os.path.basename(path)
        parts = self._logname.split('_')
        self._subject = parts[-2]
        self._feedback_enabled = parts[-1]
        self._sensor = sensor
        self._actuator = actuator
        self._start_time = sensor.start_time
        self._timerange = (actuator.time[0], actuator.time[-1])

    @property
    def filepath(self):
        return self._filepath

    @property
    def logname(self):
        return self._logname

    @property
    def subject(self):
        return self._subject

    @property
    def feedback(self):
        return self._feedback_enabled

    @property
    def sensor(self):
        return self._sensor

    @property
    def actuator(self):
        return self._actuator

    @property
    def start_time(self):
        return self._start_time

    @property
    def timerange(self):
        return self._timerange


class Subject(object):
    def __init__(self, code):
        self._code = code
        self._logs = []
        self._log_keys = []

    @property
    def code(self):
        return self._code

    @property
    def logs(self):
        return self._logs

    def add_log(self, log):
        k = log.start_time
        i = bisect.bisect_left(self._log_keys, k)
        self._log_keys.insert(i, k)
        self._logs.insert(i, log)


def parse_log(path):
    sensor = Sensor('sensor', path)
    actuator = Actuator('actuator', path)
    with open(path, 'rb') as f:
        while True:
            try:
                p = marshal.load(f)
            except EOFError:
                break

            if isinstance(p, int): # unixtime
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
                timestamp, source, data = p
                if source == 'sensor':
                    sensor.put(timestamp, data)
                elif source == 'actuator':
                    actuator.put(timestamp, data)
                else:
                    print('Unmarshalled unexpected type: {}'.format(type(data)))
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


def align_yaxis(ax1, v1, ax2, v2):
    """adjust ax2 ylimit so that v2 in ax2 is aligned to v1 in ax1"""
    _, y1 = ax1.transData.transform((0, v1))
    _, y2 = ax2.transData.transform((0, v2))
    inv = ax2.transData.inverted()
    _, dy = inv.transform((0, 0)) - inv.transform((0, y1-y2))
    ax2.set_ylim(dy + ax2.get_ylim())


def scale_yaxis(ax, lim, v):
    ymin, ymax = ax.get_ylim()
    if lim < ymin:
        factor = (lim - v)/(ymin - v)
    else:
        factor = (lim - v)/(ymax - v)
    ax.set_ylim(factor*ymin, factor*ymax)


def plot_subplots(sensor, actuator, fields, timerange=None):
    colors = line_colors(len(fields))
    tmin, tmax = calc_timerange(sensor, actuator, fields, timerange)

    fig, axes = plt.subplots(len(fields))
    if len(fields) == 1:
        axes = (axes,)
    for ax, color, field in zip(axes, colors, fields):
        if field in actuator.fields:
            t = actuator.time
            y = actuator.get_field(field)
        else:
            t = sensor.time
            y = sensor.get_field(field)
        indices = (t >= tmin) & (t <= tmax)
        ax.plot(t[indices], y[indices], color=color)
        ax.set_ylabel(units(field), color=color)
        ax.tick_params(axis='y', colors=color)
        ax.legend((field,))
        ax.set_xlim([tmin, tmax])
        ax.grid()
    axes[-1].set_xlabel('time [s]')
    return fig, axes


def plot_singleplot(sensor, actuator, fields, timerange=None):
    colors = line_colors(len(fields))
    tmin, tmax = calc_timerange(sensor, actuator, fields, timerange)

    fig, ax = plt.subplots()
    n = len(fields) - 2
    axes = [ax] + [ax.twinx() for i in fields[1:]]
    fig.subplots_adjust(right=1/(1 + 0.15*n))
    lines = []
    for i, ax in enumerate(axes[2:], 1):
        ax.spines['right'].set_position(('axes', 1 + 0.15*i))
        ax.set_frame_on(True)
        ax.patch.set_visible(False)
    for ax, color, field in zip(axes, colors, fields):
        if field in actuator.fields:
            t = actuator.time
            y = actuator.get_field(field)
        else:
            t = sensor.time
            y = sensor.get_field(field)
        indices = (t >= tmin) & (t <= tmax)
        ax.plot(t[indices], y[indices], color=color, label=field)
        ax.set_ylabel(units(field), color=color)
        ax.tick_params(axis='y', colors=color)
    axes[0].set_xlabel('time [s]')
    axes[0].set_xlim([tmin, tmax])
    return fig, axes


def plot_lean_steer(sensor, actuator, timerange=None):
    fields = ('delta', 'phi')
    fig, ax = _plot_singleplot_imp(sensor, actuator, fields)
    ax.set_ylabel('angle [deg]')
    ax.legend()
    return fig, ax


def plot_lean_steer_yy(sensor, actuator, timerange=None):
    fields = ('delta', 'phi')
    fig, ax1 = plt.subplots()
    ax2 = ax1.twinx()
    _ = _plot_singleplot_imp(sensor, actuator, fields, axes=(ax1, ax2))
    ymin, ymax = ax2.get_ylim()
    align_yaxis(ax1, 0, ax2, 0)
    if ymax > 90:
        scale_yaxis(ax2, 90, 0)
    else:
        scale_yaxis(ax2, -90, 0)
    return fig, (ax1, ax2)


def _plot_singleplot_imp(sensor, actuator, fields, timerange=None, colors=None, axes=None):
    if colors is None:
        colors = line_colors(len(fields))
    if axes is None:
        fig, ax = plt.subplots()
        axes = len(fields)*[ax]
    else:
        fig = None
    if timerange is None:
        timerange = shared_timerange(sensor, actuator, fields)

    s_ind = sensor.get_timerange_indices(timerange)
    a_ind = actuator.get_timerange_indices(timerange)
    s_t = sensor.time[s_ind]
    a_t = actuator.time[a_ind]
    for ax, color, field in zip(axes, colors, fields):
        if field in sensor.fields:
            t = s_t
            y = sensor.get_field(field)[s_ind]
        else:
            t = a_t
            y = actuator.get_field(field)[a_ind]
        ax.plot(t, y, color=color, label=field)
    axes[0].set_xlim(timerange)
    if fig is not None:
        axes = axes[0]
    return fig, axes




def shared_timerange(sensor, actuator, fields):
    if set.intersection(set(actuator.fields), set(fields)):
        tmin = max(sensor.time[0], actuator.time[0])
        tmax = min(sensor.time[-1], actuator.time[-1])
    else:
        tmin = sensor.time[0]
        tmax = sensor.time[-1]
    return tmin, tmax


def calc_timerange(sensor, actuator, fields, timerange):
    if timerange is None:
        return shared_timerange(sensor, actuator, fields)
    else:
        return timerange


def units(field):
    if field == 'delta':
        return 'deg'
    elif field == 'deltad':
        return 'deg/s'
    elif field == 'cadence':
        return 'deg/s'
    elif field == 'brake':
        return ''
    elif field == 'torque':
        return 'N-m'
    elif field == 'phi':
        return 'deg'
    raise KeyError


def line_colors(size):
    if size < 7:
        return iter(sns.color_palette('muted'))
    else:
        return iter(sns.color_palette('muted', size))


