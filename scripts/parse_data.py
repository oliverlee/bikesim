#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import abc
import bisect
import collections
import marshal
import os
import re
import time

import numpy as np
import matplotlib.pyplot as plt
import matplotlib.cm as cm
import matplotlib.patches as mpatches
import seaborn as sns
import pandas as pd
from scipy import signal

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
                                                   self._sample_size))

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
    FEEDBACK_DISABLED = False
    FEEDBACK_ENABLED = True

    def __init__(self, path):
        sensor, actuator = parse_log(path)
        self._filepath = path
        self._logname = os.path.basename(path)
        parts = self._logname.split('_')
        self._subject_code = parts[-2]
        self._feedback_enabled = bool(int(parts[-1])) == Log.FEEDBACK_ENABLED
        self._sensor = sensor
        self._actuator = actuator
        self._start_time = sensor.start_time
        # assume a new log file is created for every run
        try:
            self._timerange = (self._actuator.time[0], self._actuator.time[-1])
        except IndexError:
            raise ValueError('No actuator data in file {}'.format(path))
        self._check_timerange()
        self._balance_time = np.diff(self._timerange)[0]

    @property
    def filepath(self):
        return self._filepath

    @property
    def logname(self):
        return self._logname

    @property
    def subject_code(self):
        return self._subject_code

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

    @property
    def balance_time(self):
        return self._balance_time

    def _check_timerange(self):
        t = self._actuator.time
        dt = t[1:] - t[:-1]
        i = dt[dt > 1.05*np.median(t)]
        if not i: # no missing sections
            return
        torque = self._actuator.torque[i + 1]
        if np.any(torque == 0):
            raise ValueError('{} had more than one run'.format(self._logname))


class Subject(object):
    PERIOD0 = 0
    PERIOD1 = 1

    def __init__(self, code):
        self._code = code
        self._logs = []
        self._log_keys = []
        self._balance_time = {
                (Log.FEEDBACK_DISABLED, Subject.PERIOD0): [],
                (Log.FEEDBACK_DISABLED, Subject.PERIOD1): [],
                (Log.FEEDBACK_ENABLED, Subject.PERIOD1): [],
                (Log.FEEDBACK_ENABLED, Subject.PERIOD0): [],
                }

    @property
    def code(self):
        return self._code

    @property
    def logs(self):
        return self._logs

    def add_log(self, log):
        assert log.subject_code == self._code
        k = log.start_time
        i = bisect.bisect_left(self._log_keys, k)
        self._log_keys.insert(i, k)
        self._logs.insert(i, log)
        if not len(self._logs):
            period = Subject.PERIOD0
        else:
            t0 = time.mktime(self._logs[0].start_time)
            t1 = time.mktime(log.start_time)
            period = int((t1 - t0) > 25*60) # 25 minutes
        self._balance_time[(log.feedback, period)].append(log.balance_time)

    def balance_time(self, feedback=None, period=None):
        if period is None:
            if feedback is None:
                dis = (self._balance_time[(Log.FEEDBACK_DISABLED,
                                           Subject.PERIOD0)] +
                       self._balance_time[(Log.FEEDBACK_DISABLED,
                                           Subject.PERIOD1)])
                en = (self._balance_time[(Log.FEEDBACK_ENABLED,
                                          Subject.PERIOD0)] +
                      self._balance_time[(Log.FEEDBACK_ENABLED,
                                          Subject.PERIOD1)])
                return (dis, en)
            else:
                t = (self._balance_time[(feedback, Subject.PERIOD0)] +
                     self._balance_time[(feedback, Subject.PERIOD1)])
                return t
        else:
            if feedback is None:
                dis = self._balance_time[(Log.FEEDBACK_DISABLED, period)]
                en = self._balance_time[(Log.FEEDBACK_ENABLED, period)]
                return (dis, en)
            else:
                return self._balance_time[feedback, period]
        assert False


def balance_df(subjects):
    timespans = []
    groups = []
    enabled = []
    log_times = []
    elapsed_times = []
    elapsed_times_per_feedback = []
    first_log_time = dict()
    periods = []
    for s in subjects:
        for log in s.logs:
            log_time = time.mktime(log.start_time)
            if s.code not in first_log_time:
                first_log_time[s.code] = log_time
            full_code = s.code + '.' + str(log.feedback)
            if full_code not in first_log_time:
                first_log_time[full_code] = log_time
            elapsed_time = log_time - first_log_time[s.code]
            elapsed_per_feedback = log_time - first_log_time[full_code]

            timespans.append(log.balance_time)
            groups.append(s.code)
            enabled.append(log.feedback)
            log_times.append(log_time)
            elapsed_times.append(elapsed_time)
            elapsed_times_per_feedback.append(elapsed_per_feedback)
            if elapsed_time > 25*60: # 25 minutes
                periods.append("second")
            else:
                periods.append("first")
    return pd.DataFrame(dict(log_timespan=timespans, subject=groups,
                             torque_enabled=enabled,
                             log_start_time=log_times,
                             elapsed_time=elapsed_times,
                             period=periods,
                             elapsed_time_per_feedback=elapsed_times_per_feedback))


def plot_dist_grouped_boxchart(subject_map):
#   fig, ax = plt.subplots()
#    size = len(subject_map.keys())
#    for i, s in enumerate(subject_map.values(), 1):
#        sns.boxplot(s.balance_time(), positions=[3*i - 2, 3*i - 1])
#    xmax = 3*size
#    ax.set_xlim(0, xmax)
#    ax.set_xticks(np.arange(1.5, xmax, 3))
#    ax.set_xticklabels(list(subject_map.keys()))
#    ax.set_xlabel('subject')
#    ax.set_ylabel('time [s]')
#    set_torque_enabled_legend(ax)
#    return fig, ax
    df = balance_df(subject_map.values())
    g = sns.factorplot("subject", "log_timespan", "torque_enabled", df,
                       kind="box", size=6, aspect=1.75, legend=False)
    g.despine()
    g.set_axis_labels("subject", "time [s]")
    ax = g.axes[0][0]
    set_torque_enabled_legend(ax)
    return g


def plot_dist_overlapping_histogram(subject_map):
    fig, ax = plt.subplots()
    sns.despine(left=True)
    disabled = []
    enabled = []
    for s in subject_map.values():
        dis, en = s.balance_time()
        disabled += dis
        enabled += en
    sns.distplot(disabled, kde=False, ax=ax)
    sns.distplot(enabled, kde=False, ax=ax)

    ax.set_xlim([0, ax.get_xlim()[1]])
    ax.set_xlabel('time [s]')

    plt.setp(ax, yticks=[])
    plt.tight_layout()
    set_torque_enabled_legend(ax)
    return fig, ax


def rms(a):
    return np.sqrt(np.mean(a**2))


def plot_overlapping_psd(subject_map):
    fig, ax = plt.subplots()
    sns.despine(left=True)
    color = sns.color_palette()
    psd = [np.array([]), np.array([])]
    fs = 20 # Hz
#    text_offset = [2, 10]
    text_offset = [2.1, 10]
    text_size = 12
    bbox_props = dict(boxstyle="square,pad=0.1", fc="w", ec="w", alpha=0.9)
    for i, s in enumerate(subject_map.values(), 1):
#        lean0 = []
#        lean1 = []
#        for log in s.logs: # maybe just take the longest run?
#            if not log.feedback:
#                lean0 = np.append(lean0, log.actuator.phi)
#            else:
#                lean1 = np.append(lean1, log.actuator.phi)
#        for en, lean in zip((0, 1), (lean0, lean1)):
        best_balance = [0, 0]
        best_lean = [[], []]
        for log in s.logs:
            en = int(log.feedback)
            if log.balance_time > best_balance[en]:
                best_balance[en] = log.balance_time
                best_lean[en] = log.actuator.phi
        for en, lean in zip((0, 1), best_lean):
            f, psds = signal.welch(lean, fs, nperseg=256,
                                   return_onesided=True)
            ax.loglog(f, psds, color=color[en])
            psd[en] = np.append(psd[en], psds)
            ax.text(f[len(f)/(text_offset[en]*i)],
                    psds[len(f)/(text_offset[en]*i)],
                    '{} - rms: {:0.4}'.format(log.subject_code, rms(lean)),
                    ha='right', va ='center', color=color[en],
                    size=text_size, bbox=bbox_props)

    psd0, psd1 = psd
    group_size = len(subject_map)
    psd0 = psd0.reshape((group_size, len(f)))
    psd0_max = psd0.max(0)
    psd1 = psd1.reshape((group_size, len(f)))
    psd1_max = psd1.max(0)

#    ymin = 1e-2
    ymin = 1e-5
    plt.fill_between(f, psd0_max, psd1_max, color=color[0], alpha=0.2)
    plt.fill_between(f, psd1_max, ymin, color=color[1], alpha=0.2)

#    ax.set_xlim([1e-2,  1e1])
    ax.set_xlim([1e-1,  1e1])
    ax.set_xlabel('frequency [Hz]')
    ax.set_ylim([ymin, ax.get_ylim()[1]])
    ax.set_yticks([])

    set_torque_enabled_legend(ax)
    return fig, ax


def plot_subject_balance_time_change_boxplot(subject_map):
    df = pd.DataFrame(dict(v=[0, 1, 2, 3]))
    g = sns.FacetGrid(df, col='v', col_wrap=2, sharex=True,
                      sharey=False, legend_out=True)
    sns.despine()

    c = sns.color_palette()
    color = 2*[c[0], c[1]]
    for i, s in enumerate(subject_map.values()):
        ax = g.axes[i]
        ax.set_title('{}'.format(s.code))
        sns.boxplot(s.balance_time(period=0) + s.balance_time(period=1),
                    positions=[1, 2, 4, 5], color=color, ax=ax)
    xmax = 3*2
    ax.set_xlim([0, xmax])
    ax.set_xticks(np.arange(1.5, xmax, 3))
    ax.set_xticklabels(['first', 'second'])
    p0 = mpatches.Patch(color=color[0], label='torque disabled')
    p1 = mpatches.Patch(color=color[1], label='torque enabled')
    g.add_legend({p0.get_label(): p0, p1.get_label(): p1})
    g.set_axis_labels('period', 'time')
    return g


def set_torque_enabled_legend(ax):
    color = sns.color_palette()
    p0 = mpatches.Patch(color=color[0], label='torque disabled')
    p1 = mpatches.Patch(color=color[1], label='torque enabled')
    ax.legend(handles=(p0, p1))


def parse_log_dir(dirname):
    pattern = re.compile('^log_\d{6}_\d{6}_UTC_\d{3}_[01]$')
    subjects = dict()
    log_count = 0
    for f in os.listdir(dirname):
        if pattern.match(os.path.basename(f)):
            f = os.path.join(dirname, f)
            #print('Parsing file {}'.format(f))
            log = Log(f)
            if log.subject_code not in subjects:
                s = Subject(log.subject_code)
                subjects[log.subject_code] = s
            subjects[log.subject_code].add_log(log)
            log_count += 1
    if not subjects:
        print('No valid log files found!')
    else:
        subjects = collections.OrderedDict(sorted(subjects.items()))
        msg = '{} log file(s) parsed for subject(s): {}'
        print(msg.format(log_count, ', '.join(subjects.keys())))
    return subjects


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
    dt = transducer.dt * 1000 # milliseconds
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


