from deepface import DeepFace
from retinaface import RetinaFace
import sys
import os
import time
import cv2
import matplotlib.pyplot as plt
from mtcnn import MTCNN

def drawBoundingBoxes(imageData, outputdirectory, imageOutputName, inferenceResults, color):
    """Draw bounding boxes on an image.
    imageData: image data in numpy array format
    imageOutputPath: output image file path
    inferenceResults: inference results array off object (l,t,w,h)
    colorMap: Bounding box color candidates, list of RGB tuples.
    """
    imageOutputPath = os.path.join(outputdirectory, imageOutputName)
    for res in inferenceResults:
        x, y, w, h = res["box"]
        left = int(x)
        top = int(y)
        right = int(x) + int(w)
        bottom = int(y) + int(h)
        #label = res['label']
        imgHeight, imgWidth, _ = imageData.shape
        thick = int((imgHeight + imgWidth) // 900)
        cv2.rectangle(imageData,(left, top), (right, bottom), color, thick)
        #cv2.putText(imageData, label, (left, top - 12), 0, 1e-3 * imgHeight, color, thick//3)

    cv2.imwrite(imageOutputPath, imageData)

numFaces = 0
numFrames = 0
minconfidence = 50
detector = MTCNN()

backends = ['opencv', 'ssd', 'dlib', 'mtcnn', 'retinaface', 'mediapipe']
parent_dir = os.getcwd() + "\\BystandAR Testing\\Day 2\\FrameCaptures\\10132022100453"
outputdirectory = os.path.join(parent_dir, 'FramesWithFacialDetection')
try:
    os.mkdir(outputdirectory)
except:
    print("Directory exists, not creating new one")

for filename in os.listdir(parent_dir):
    f = os.path.join(parent_dir, filename)
    # checking if it is a file
    if os.path.isfile(f):
        imgcv = cv2.imread(f)
        color = (0,255,0)
        numFrames = numFrames + 1
        img = cv2.cvtColor(cv2.imread(f), cv2.COLOR_BGR2RGB)

        detections = detector.detect_faces(img)
        embeddings = []
        vettedDetections = []
        for detection in detections:
            if detection["confidence"] > 0.90:
                
                x, y, w, h = detection["box"]
                detected_face = img[int(y):int(y+h), int(x):int(x+w)]
                embedding = DeepFace.represent(detected_face, model_name = 'Facenet', enforce_detection = False)
                vettedDetections.append(detection)
                for embed in embedding:
                    try:
                        analysis = DeepFace.analyze(embed, actions = ["age", "gender", "emotion", "race"], enforce_detection = False)
                        numFaces = numFaces + 1
                    
                    except:
                        continue
        
        drawBoundingBoxes(imgcv, outputdirectory, str(numFrames) + '.png', vettedDetections, color)

print("Total faces: " + str(numFaces) + " in " + str(numFrames) + " frames.")

#while True:
 #  pass