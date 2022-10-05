
import socket
import datetime
import os
import sys
from PIL import BmpImagePlugin
from PIL import Image as Image
import io
import struct
import base64



HOST = "192.168.0.40"  # Standard loopback interface address (localhost)
PORT = 65433  # Port to listen on (non-privileged ports are > 1023)

currentDatetime = datetime.datetime.now()
parent_dir = os.getcwd() + "\\DepthCaptures"
path = os.path.join(parent_dir, currentDatetime.strftime("%m%d%Y%H%M%S"))
os.mkdir(path)
print("Directory '% s' created" % path)

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
    s.bind((HOST, PORT))
    s.listen(1)
    conn, addr = s.accept()
    frameCounter = 0
    while True:
        filenameJPG = str(frameCounter) + ".jpg"
        filePathJPG = os.path.join(path, filenameJPG)
        frameCounter += 1
        data = conn.recv(4)
        
        length = int.from_bytes(data, "little")
        print(length)

        chunks = []
        bytes_recd = 0
        while bytes_recd < length:
            chunk = conn.recv(min(length - bytes_recd, 2048))
            if chunk == b'':
                raise RuntimeError("socket connection broken")
            chunks.append(chunk)
            bytes_recd = bytes_recd + len(chunk)
        pictureInput = b''.join(chunks)


        try:
            img = Image.frombytes('L', (320,288), pictureInput)
            img.save(filePathJPG)
        except:
            print("Frame Dropped")
            pass
        # close the server socket
        #s.close()



