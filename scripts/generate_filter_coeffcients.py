#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import os
import sys
import jinja2
from scipy.signal import butter, cheby1, cheby2


def lowpass_coeffs(filter_type, cutoff, fs, order):
    nyq = 0.5*fs
    normal_cutoff = cutoff/nyq
    b, a = butter(order, normal_cutoff, btype='low', analog=False)
    return b, a


def generate_source(filename_base, ext, template_dict):
    if filename_base is 'median':
        template = template_setup(ext, filename_base)
    else:
        template = template_setup(ext)
    scripts_dir = os.path.dirname(os.path.abspath(__file__))
    source_dir = os.path.join(scripts_dir, '../BsgBikeSim2014')
    filename = os.path.join(source_dir, filename_base + 'lowpass' + ext)
    with open(filename, 'w') as f:
        f.write(template.render(template_dict))


def template_setup(ext, filename_base=None):
    template_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                'templates')
    template_loader = jinja2.FileSystemLoader(template_dir)
    template_env = jinja2.Environment(loader=template_loader)
    if filename_base is None:
        filename_base = 'filter'
    return template_env.get_template('{}lowpass{}.in'.format(filename_base,
                                                             ext))


if __name__ == "__main__":
    usage = 'generate arduino source files for filters\n'
    usage += '{0} <cutoff_freq> <sample_freq>'.format(__file__)

    if len(sys.argv) < 3:
        print(usage)
        sys.exit(1)

    cutoff_freq = float(sys.argv[1])
    sample_freq = float(sys.argv[2])
    order = 4
    template_dict = {
        'cutoff_freq': cutoff_freq,
        'sample_freq': sample_freq,
        'order': order
    }
    #generate_source('median', '.h', template_dict)

    for filter_type, filter_name in zip((butter,),
                                        ('butter',)):
        b, a = lowpass_coeffs(filter_type, cutoff_freq, sample_freq, order)
        template_dict['filter_name'] = filter_name.title()
        template_dict['a'] = ','.join(map(str, a))
        template_dict['b'] = ','.join(map(str, b))
        generate_source(filter_name.lower(), '.cpp', template_dict)
        generate_source(filter_name.lower(), '.h', template_dict)
