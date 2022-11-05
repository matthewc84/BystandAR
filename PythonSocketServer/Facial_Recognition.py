from deepface import DeepFace
from deepface.detectors import FaceDetector
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
        x, y, w, h = res
        left = int(x)
        top = int(y)
        right = int(x) + int(w)
        bottom = int(y) + int(h)
        imgHeight, imgWidth, _ = imageData.shape
        thick = int((imgHeight + imgWidth) // 900)
        cv2.rectangle(imageData,(left, top), (right, bottom), color, thick)
        #cv2.putText(imageData, label, (left, top - 12), 0, 1e-3 * imgHeight, color, thick//3)

    cv2.imwrite(imageOutputPath, imageData)

numFaces = 0
numFrames = 0

backends = ['opencv', 'ssd', 'dlib', 'mtcnn', 'retinaface', 'mediapipe']
models = ["VGG-Face", "Facenet", "OpenFace", "DeepFace", "Dlib", "ArcFace"]
parent_dir = os.getcwd() + "\\BystandAR Testing\\All Tests\\"

for foldername in os.listdir(parent_dir):
    numFrames = 0
    tempOutputDirectory = os.path.join(parent_dir, foldername)
    outputdirectory = os.path.join(tempOutputDirectory, 'FramesWithBystanders')
    folderpath = os.path.join(parent_dir, foldername)
    try:
        os.mkdir(outputdirectory)
    except:
        print("Directory exists, not creating new one")
    for filename in os.listdir(folderpath):
        f = os.path.join(folderpath, filename)
        # checking if it is a file
        if os.path.isfile(f):
            img = cv2.imread(f)
            color = (0,255,0)
            detector = FaceDetector.build_model(backends[3])
            vettedDetections = []
            detections = detector.detect_faces(img)
            for face in detections:
                score = face["confidence"]
                if score > 0.90:
                    x, y, w, h = face["box"]
                    detected_face = img[int(y):int(y+h), int(x):int(x+w)]
                    analysis = DeepFace.analyze(detected_face, enforce_detection=False)
                    if(analysis["emotion"][analysis["dominant_emotion"]] > 90 or analysis["race"][analysis["dominant_race"]] > 90):
                        vettedDetections.append(face["box"])

        if len(vettedDetections) > 0:
            drawBoundingBoxes(img, outputdirectory, str(numFrames) + '.png', vettedDetections, color)
        numFrames += 1

print("Total faces: " + str(numFaces) + " in " + str(numFrames+1) + " frames.")

#while True:
 #  pass