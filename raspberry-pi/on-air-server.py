#!/usr/bin/env python3

import socket
import RPi.GPIO as GPIO
import time

HOST = '0.0.0.0'  # Standard loopback interface address (localhost)
PORT = 65432        # Port to listen on (non-privileged ports are > 1023)

webcamOn = False
microphoneOn = False

try:
  GPIO.setmode(GPIO.BCM)
  GPIO.setup(5, GPIO.OUT)
  GPIO.setup(25, GPIO.OUT)

  with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
    s.bind((HOST, PORT))
    s.listen()

    while True:
      conn, addr = s.accept()
      with conn:
        print('Connected by', addr)
        while True:
          data = conn.recv(1024)
          if not data:
            break
          print('Received', str(data))

          if str(data) == "b'webcam:on'":
            webcamOn = True
          elif str(data) == "b'webcam:off'":
            webcamOn = False
          elif str(data) == "b'microphone:on'":
            microphoneOn = True
          elif str(data) == "b'microphone:off'":
            microphoneOn = False

          if webcamOn:
              GPIO.output(5, GPIO.HIGH)
              GPIO.output(25, GPIO.LOW)
          elif microphoneOn:
              GPIO.output(5, GPIO.LOW)
              GPIO.output(25, GPIO.HIGH)
          else:
              GPIO.output(5, GPIO.LOW)
              GPIO.output(25, GPIO.LOW)

except KeyboardInterrupt:
  print("")
  print("Ctrl-C detected: closing down")

finally:
   print("cleaning up GPIO")
   GPIO.cleanup()
