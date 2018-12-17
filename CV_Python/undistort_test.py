import numpy as np
import cv2

def is_similar(image1, image2):
    return image1.shape == image2.shape and not(np.bitwise_xor(image1,image2).any())

# from Kevins calibration of the depth camera - intrinsics matrix
fx = 197.707225374208
fy = 201.581311460880
ppx = 226.208683510427
ppy = 225.819548268168
k1 = -0.271283991007049
k2 = 0.0806828103078386
k3 = -0.0109236654954672
p1 = 0
p2 = 0

camera_matrix = np.array([[fx, 0, ppx],
                          [0, fy, ppy],
                          [0, 0, 1]], dtype='float32')

dist_coeffs = np.array([k1, k2, p1, p2, k3], dtype='float32')

# Create a VideoCapture object and read from input file
# If the input is the camera, pass 0 instead of the video file name
cap = cv2.VideoCapture('hands_red.mp4')

# Check if camera opened successfully
if (cap.isOpened() == False):
    print("Error opening video stream or file")

# Read until video is completed
while (cap.isOpened()):
    # Capture frame-by-frame
    ret, frame = cap.read()
    if ret == True:

        # Display the resulting frame
        # cv2.imshow('Frame', frame)

        height, width = frame.shape[:2]
        DIM = (width, height)

        # print red value of center of image (u, v, colorchannel)
        # print(frame[int(height/2)][int(width/2)][2])

        count = 0

        if frame[int(height/2)][int(width/2)][2] != 0:
            # searches in pixel and changes pixel colour
            # frame[np.where((frame == [0, 0, 0]).all(axis=2))] = [0, 33, 166]

            # trying to search in array with vectorisation
            # print (np.where((frame == [0, 0, 0]).all(axis=2)))
            cv2.imshow('frame', frame)

            newcameramtx, roi = cv2.getOptimalNewCameraMatrix(camera_matrix, dist_coeffs, (width, height), 1,
                                                              (width, height))
            mapx, mapy = cv2.initUndistortRectifyMap(newcameramtx, dist_coeffs, None, newcameramtx, (width, height), 5)
            dst = cv2.remap(frame, mapx, mapy, cv2.INTER_LINEAR)

            cv2.imshow('undist_frame', dst)

            print(is_similar(frame, dst))

            cv2.waitKey(0)
            break

        # Press Q on keyboard to  exit
        if cv2.waitKey(25) & 0xFF == ord('q'):
            break

    # Break the loop
    else:
        break

# When everything done, release the video capture object
cap.release()

# Closes all the frames
cv2.destroyAllWindows()