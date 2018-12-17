import numpy as np
import cv2

# Create a VideoCapture object and read from input file
# If the input is the camera, pass 0 instead of the video file name
cap = cv2.VideoCapture('hands_red.mp4')
#cap = cv2.VideoCapture('depth_gray.mp4')

# Check if camera opened successfully
if (cap.isOpened() == False):
    print("Error opening video stream or file")

# Read until video is completed
while (cap.isOpened()):
    # Capture frame-by-frame
    ret, frame = cap.read()
    if ret == True:

        height, width = frame.shape[:2]

        imgray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        image, contours, hierarchy = cv2.findContours(imgray, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)

        if len(contours) != 0:
            # draw in blue the contours that were found
            #cv2.drawContours(frame, contours, -1, 255, 3)

            # find the biggest area
            c = max(contours, key=cv2.contourArea)

            x, y, w, h = cv2.boundingRect(c)

            hull = cv2.convexHull(c)

            cv2.drawContours(frame, hull, -1, 255, 3)

            # draw the book contour (in green)
            #cv2.rectangle(frame, (x, y), (x + w, y + h), (0, 255, 0), 2)

            # stop at one point where hand is in the middle
            if frame[int(height / 2)][int(width / 2)][2] != 0:
                cv2.waitKey(0)

        # Display the resulting frame
        cv2.imshow('Imgray', frame)

        # Press Q on keyboard to exit
        if cv2.waitKey(25) & 0xFF == ord('q'):
            break

    # Break the loop
    else:
        break

# When everything done, release the video capture object
cap.release()

# Closes all the frames
cv2.destroyAllWindows()