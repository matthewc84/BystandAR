
import socket
import datetime
import os
import sys
from PIL import BmpImagePlugin
from PIL import Image as Image
import io
import struct
import base64
import cv2
import numpy as np
import math


# import thread module
from _thread import *
import threading
 

print_lock = threading.RLock()

def Convert(filename, width, height):
        f_y = open(filename, "rb")
        f_uv= open(filename, "rb")
        converted_image = Image.new("RGB", (width, height) )
        pixels = converted_image.load()
 		#lets get our y cursor ready
        for j in range(0, height):
            for i in range(0, width):

                uv_index = (width * math.floor(j/2)) + (math.floor(i/2))*2
                f_uv.seek(uv_index)
                y = ord(f_y.read(1))
                u = ord(f_uv.read(1))
                v = ord(f_uv.read(1))

                b = 1.164 * (y-16) + 2.018 * (u - 128)
                g = 1.164 * (y-16) - 0.813 * (v - 128) - 0.391 * (u - 128)
                r = 1.164 * (y-16) + 1.596*(v - 128)

                pixels[i,j] = int(r), int(g), int(b)
        converted_image.save(filename)

# thread function
def threaded(c):
    frameCounter = 0
    currentDatetime = datetime.datetime.now()
    parent_dir = os.getcwd() + "\\FrameCaptures"
    path = os.path.join(parent_dir, currentDatetime.strftime("%m%d%Y%H%M%S"))
    os.mkdir(path)
    print("Directory '% s' created" % path)
    while True:
        print_lock.acquire()
        filenameJPG = str(frameCounter) + ".jpg"
        filePathJPG = os.path.join(path, filenameJPG)
        frameCounter += 1
        data = c.recv(4)
        
        length = int.from_bytes(data, "little")
        print(length)

        chunks = []
        bytes_recd = 0
        while bytes_recd < length:
            chunk = c.recv(min(length - bytes_recd, 8192))
            if chunk == b'':
                raise RuntimeError("socket connection broken")
            chunks.append(chunk)
            bytes_recd = bytes_recd + len(chunk)
        pictureInput = b''.join(chunks)


        try:
            #img = Image.frombytes('L', (1920,1080), pictureInput)
            img = Image.open(io.BytesIO(pictureInput))
            img = img.rotate(180)
            #img = img.convert('RGB')
            img = img.transpose(Image.FLIP_LEFT_RIGHT)
            img.save(filePathJPG)

        except:
            print("Frame Dropped")
            pass

        print_lock.release()
 
    # connection closed
    #c.close()

def Main():
    HOST = ""  # Standard loopback interface address (localhost)
    PORT = 65432  # Port to listen on (non-privileged ports are > 1023)
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.bind((HOST, PORT))
    print("socket binded to port", PORT)
 
    # put the socket into listening mode
    s.listen(5)
    print("socket is listening")
 
    # a forever loop until client wants to exit
    while True:
 
        # establish connection with client
        c, addr = s.accept()
 
        # lock acquired by client
        
        print('Connected to :', addr[0], ':', addr[1])
 
        # Start a new thread and return its identifier
        start_new_thread(threaded, (c,))
    s.close()
 
 
if __name__ == '__main__':
    Main()



