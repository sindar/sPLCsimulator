# !/usr/bin/env python3
# -*- coding: utf-8 -*-

import board
import neopixel
import sys
import getopt
import time
import random

pixels_pin = board.D12

pixels_num = 40
ORDER = neopixel.GRB

default_color = (255, 255, 0)
no_color = (0, 0, 0)

def switch_pixels(pixels, on, color):
    if on:
        pixels.fill(color)
    else:
        pixels.fill((0, 0, 0))
    pixels.show()

def music(pixels, num):
    while True:
        for i in range(num):
            r = random.randrange(255)
            g = random.randrange(255)
            b = random.randrange(255)
            pixels[i] = (r, g, b)
        pixels.show()
        time.sleep(0.5)

def main(argv):
    #print ('Argument List:', str(sys.argv))
    pixels = None
    on = False
    music_on = False
    talk_on = False
    red = default_color[0]
    green = default_color[1]
    blue = default_color[2]
    help_message = 'backlight.py -s <on|off|music> -r <val> -g <val> -b <val>' 
    if len(argv) < 2:
        print (help_message)
    try:
        opts, args = getopt.getopt(argv, "hs:r:g:b:", ["switch=", "red=", "green=", "blue="])
    except getopt.GetoptError:
        print (help_message)
        sys.exit(2)
    for opt, arg in opts:
        if opt == '-h':
            print (help_message)
            sys.exit(0)
        elif opt in ("-s", "--switch"):
            pixels = neopixel.NeoPixel(pixels_pin, pixels_num, brightness=0.5, auto_write=False,
                                       pixel_order=ORDER)
            if (arg == 'on'):
                on = True
            elif (arg == 'music'):
                music_on = True
        elif opt in ("-r", "--red"):
            red = int(arg)
        elif opt in ("-g", "--green"):
            green = int(arg)
        elif opt in ("-b", "--blue"):
            blue = int(arg)

    if (pixels):
        switch_pixels(pixels, on, (red, green, blue))

    if (music_on):
        music(pixels, num)

if __name__ == "__main__":
   main(sys.argv[1:])
