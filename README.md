# bikesim

This project runs a bicycle simulator running with a monitor and a modified
stationary bicycle interface.

The software consists of 3 parts:
 - A Unity3d game/project that runs on a computer that runs the game engine and
   simulates the bicycle dynamics.
 - An arduino project that reads various sensors and actuates the haptic motor.
 - A python layer that interfaces between the game and embedded device.

## Generation of protobuf sources
As Unity is currently limited to use of Protocol Buffers 2, the protobuf dll and
source generation tool can be downloaded from [here](
https://storage.googleapis.com/google-code-archive-downloads/v2/code.google.com/protobuf-net/protobuf-net%20r668.zip).

Unfortunately, this protogen tool is not platform agnostic and must be run on
windows:

    > protogen.exe -i:pose.proto -o:pose.cs

The output class is put in the `pose` namespace. This must be manually changed
to the `pb` namespace. The output file must also be placed in the Scripts
directory.

Generation of new protobuf sources are only required when the protobuf message
definition is updated.


## Acknowledgements
This repository is forked from a project started by a Bachelor/Master student
group at TU Delft.

A number of people have contributed this project including:
Marco Grottoli, Jodi Kooijman, Thom van Beek, Guillermo Currás Lorenzo, and
Tiago Pinto
