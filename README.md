

[![MIT License](https://img.shields.io/apm/l/vim-mode?color=%2336f029&style=plastic)](https://github.com/LastKnightXZ/deterministic-lockstep-template-using-unity-and-mirror/blob/aa03a138bb5785d55039d5fe04aba83989a76273/LICENSE)



# Deterministic Lockstep Template using unity and mirror


A project to help anyone make their own fully deterministic lockstep games using unity and mirror.

## Features

- Implements tick system which works with variable frame rates 

- Custom serialization methods to have precise control on what gets serialized and what not 

- Client side preidiction 

- Server side reconciliation

- Entity interpolation with configurable waiting time

- Way to perform lag compensated actions 

- Custom solution to sync mechAnim Animator's parameters, state, uninterupted and interupted state transitions, layerWeights, etc

- Uses own custom fixedUpdate for physics and thus making sure no of physics step in between ticks stays consistent
