from deepface import DeepFace
import sys
import os
import time

def loading():                                  #make a function called loading
    spaces = 0                                      #making a variable to store the amount of spaces between the start and the "."
    while True:                                     #infinite loop
        print("\b "*spaces+".", end="", flush=True) #we are deleting however many spaces and making them " " then printing "."
        spaces = spaces+1                           #adding a space after each print
        time.sleep(0.2)                             #waiting 0.2 secconds before proceeding
        if (spaces>5):                              #if there are more than 5 spaces after adding one so meaning 5 spaces (if that makes sense)
            print("\b \b"*spaces, end="")           #delete the line
            spaces = 0                              #set the spaces back to 0

numFaces = 0
numFrames = 0
minconfidence = 50

backends = ['opencv', 'ssd', 'dlib', 'mtcnn', 'retinaface', 'mediapipe']
parent_dir = os.getcwd() + "\\FrameCaptures\\10032022223658"

for filename in os.listdir(parent_dir):
    f = os.path.join(parent_dir, filename)
    # checking if it is a file
    if os.path.isfile(f):
        numFrames = numFrames + 1
        try:
            face = DeepFace.detectFace(img_path = f, target_size = (224, 224), detector_backend = backends[4])
            for i, instance in face.iterrows():
                confidence = round(100*instance["confidence"], 2)
                if(confidence > minConfidence):
                    numFaces = numFaces + 1
                    print("Current number of found faces: " + str(numfaces))
        except:
            #print("No Face in file: " + str(f))
            pass

print("Total faces: " + str(numFaces) + " in " + str(numFrames) + " frames.")