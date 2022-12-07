import os
import shutil

images = [f for f in os.listdir() if '.jpg' in f.lower()]

os.mkdir('Raw_Images')
os.mkdir('Obscured_Images')

for image in images:
    if image.endswith('a.jpg'):
        new_path = 'Obscured_Images/' + image
        shutil.move(image, new_path)
    else:
        new_path = 'Raw_Images/' + image
        shutil.move(image, new_path)
