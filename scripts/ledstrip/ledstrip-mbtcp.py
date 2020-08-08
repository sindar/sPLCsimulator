#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import os
import time
import subprocess
import sys
from pyModbusTCP.client import ModbusClient

script_dir = os.path.dirname(os.path.abspath(__file__))
mbclient = ModbusClient(host="localhost", port=502, auto_open=True)

while mbclient:
    p = None
    command = mbclient.read_holding_registers(10, 5)

    if command:
        if command[0] != 0:
            print("command = " + str(command))
            if command[0] == 2:
                p = subprocess.Popen(["python3", script_dir + "/ledstrip-neopixel.py", "-s", "off"])
            elif command[0] == 1:
                set_color = mbclient.read_holding_registers(11, 1)[0]
                if command[1] == 0:
                    p = subprocess.Popen(["python3", script_dir + "/ledstrip-neopixel.py", "-s", "on"])
                else:
                    color = mbclient.read_holding_registers(12, 3)
                    p = subprocess.Popen(["python3", script_dir + "/ledstrip-neopixel.py", "-s", "on",
                                          "-r", str(command[2] if command[2] <= 255 else 255),
                                          "-g", str(command[3] if command[3] <= 255 else 255),
                                          "-b", str(command[4] if command[4] <= 255 else 255)])
            mbclient.write_multiple_registers(10, [0 for i in range(5)])
    else:
        print("Modbus TCP read error, exiting!")
        sys.exit(1)
            
    if p:
        p.wait()
    else:
        time.sleep(0.1)
