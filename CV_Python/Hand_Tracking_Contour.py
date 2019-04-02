import numpy as np
import cv2

# Create a VideoCapture object and read from input file
# If the input is the camera, pass 0 instead of the video file name
cap = cv2.VideoCapture('hands_red.mp4')
#cap = cv2.VideoCapture('depth_gray.mp4')

# stop at 200th frame and save to output
timer = 200

# Check if camera opened successfully
if (cap.isOpened() == False):
    print("Error opening video stream or file")

# Read until video is completed
while (cap.isOpened()):
    # Capture frame-by-frame
    ret, frame = cap.read()
    if ret == True:

        timer = timer - 1

        height, width = frame.shape[:2]

        imgray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        image, contours, hierarchy = cv2.findContours(imgray, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)

        if len(contours) != 0:
            # draw in blue the contours that were found
            #cv2.drawContours(frame, contours, -1, 255, 3)

            # a contour is simply a NumPy array of (x, y)-coordinates.

            # find the biggest contour --> Hand
            cnt = max(contours, key=cv2.contourArea)

            # finds hightest point
            topmost = tuple(cnt[cnt[:, :, 1].argmin()][0])

            # build rectangle around highest point
            A_x = topmost[0] - width/14
            A_y = topmost[1] + height/4

            B_x = topmost[0] + width/14
            B_y = topmost[1]

            roi = frame[B_y:A_y, A_x:B_x]
            imgray = cv2.cvtColor(roi, cv2.COLOR_BGR2GRAY)
            image, contours, hierarchy = cv2.findContours(imgray, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)
            cv2.drawContours(roi, contours, -1, 255, 3)
            cnt = max(contours, key=cv2.contourArea)

            #cv2.imshow("test", roi)

            (x, y), radius = cv2.minEnclosingCircle(cnt)

            CIRCLE_THICKNESS = 2
            GREEN = np.array([0, 255, 0])

            cv2.circle(roi, (int(x), int(y)), int(radius), GREEN, CIRCLE_THICKNESS)
            cv2.circle(roi, (int(x), int(y)), int(2), GREEN, CIRCLE_THICKNESS)
            cv2.rectangle(frame, (A_x, B_y), (B_x, A_y), (255, 0, 0), 2)

            M = cv2.moments(cnt)
            cx = int(M['m10'] / M['m00'])
            cy = int(M['m01'] / M['m00'])

            cv2.circle(roi, (cx, cy), int(2), [0, 255, 255], CIRCLE_THICKNESS)

            # build bounding rectangle
            x, y, w, h = cv2.boundingRect(cnt)

            # find convex hull and defects
            hull = cv2.convexHull(cnt, returnPoints = False)

            # [ start point, end point, farthest point, approximate distance to farthest point ]
            defects = cv2.convexityDefects(cnt, hull)

            hull = cv2.convexHull(cnt)
            #cv2.drawContours(frame, hull, -1, [255, 255, 0], 3)
            #cv2.drawContours(frame, defects, -1, 255, 3)

            M = cv2.moments(cnt)
            cX = int(M["m10"] / M["m00"])
            cY = int(M["m01"] / M["m00"])

            #cv2.circle(frame, (cX, cY), 5, (255, 255, 255), -1)

            # draw the rectangle bounding box of contour (in green)
            #cv2.rectangle(frame, (x, y), (x + w, y + h), (0, 255, 0), 2)



        # Display the resulting frame
        cv2.imshow('Imgray', frame)

        # Press Q on keyboard to exit
        if timer == 150:
            cv2.imwrite('image0.png', frame)

        if timer == 30:
            cv2.imwrite('image1.png', frame)

        if timer == 0:
            cv2.imwrite('image2.png', frame)
            break

        # Press p for pause
        elif cv2.waitKey(25) & 0xFF == ord('p'):
            cv2.waitKey(0)



    # Break the loop
    else:
        break

# When everything done, release the video capture object
cap.release()

# Closes all the frames
cv2.destroyAllWindows()