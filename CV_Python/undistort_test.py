import numpy as np
import cv2
import math

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

            #newcameramtx, roi = cv2.getOptimalNewCameraMatrix(camera_matrix, dist_coeffs, (width, height), 1,(width, height))
            #mapx, mapy = cv2.initUndistortRectifyMap(newcameramtx, dist_coeffs, None, newcameramtx, (width, height), 5)
            #dst = cv2.remap(frame, mapx, mapy, cv2.INTER_LINEAR)

            #cv2.imshow('undist_frame', dst)

            #print(is_similar(frame, dst))

            imgray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
            ret, thresh = cv2.threshold(imgray, 127, 255, 0)
            image, contours, hierarchy = cv2.findContours(imgray, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)

            crop_image = frame[100:300, 100:300]

            try:
                # Find contour with maximum area
                contour = max(contours, key=lambda x: cv2.contourArea(x))

                # Create bounding rectangle around the contour
                x, y, w, h = cv2.boundingRect(contour)
                cv2.rectangle(crop_image, (x, y), (x + w, y + h), (0, 0, 255), 0)

                # Find convex hull
                hull = cv2.convexHull(contour)

                # Draw contour
                drawing = np.zeros(crop_image.shape, np.uint8)
                cv2.drawContours(drawing, [contour], -1, (0, 255, 0), 0)
                cv2.drawContours(drawing, [hull], -1, (0, 0, 255), 0)

                # Find convexity defects
                hull = cv2.convexHull(contour, returnPoints=False)
                defects = cv2.convexityDefects(contour, hull)

                # Use cosine rule to find angle of the far point from the start and end point i.e. the convex points (the finger
                # tips) for all defects
                count_defects = 0

                for i in range(defects.shape[0]):
                    s, e, f, d = defects[i, 0]
                    start = tuple(contour[s][0])
                    end = tuple(contour[e][0])
                    far = tuple(contour[f][0])

                    a = math.sqrt((end[0] - start[0]) ** 2 + (end[1] - start[1]) ** 2)
                    b = math.sqrt((far[0] - start[0]) ** 2 + (far[1] - start[1]) ** 2)
                    c = math.sqrt((end[0] - far[0]) ** 2 + (end[1] - far[1]) ** 2)
                    angle = (math.acos((b ** 2 + c ** 2 - a ** 2) / (2 * b * c)) * 180) / 3.14

                    # if angle > 90 draw a circle at the far point
                    if angle <= 90:
                        count_defects += 1
                        cv2.circle(crop_image, far, 1, [0, 0, 255], -1)

                    cv2.line(crop_image, start, end, [0, 255, 0], 2)

                # Print number of fingers
                if count_defects == 0:
                    cv2.putText(frame, "HELLO", (50, 50), cv2.FONT_HERSHEY_SIMPLEX, 2, 2)
                elif count_defects == 1:
                    cv2.putText(frame, "TWO", (50, 50), cv2.FONT_HERSHEY_SIMPLEX, 2, 2)
                elif count_defects == 2:
                    cv2.putText(frame, "THREE", (5, 50), cv2.FONT_HERSHEY_SIMPLEX, 2, 2)
                elif count_defects == 3:
                    cv2.putText(frame, "FOUR", (50, 50), cv2.FONT_HERSHEY_SIMPLEX, 2, 2)
                elif count_defects == 4:
                    cv2.putText(frame, "FIVE", (50, 50), cv2.FONT_HERSHEY_SIMPLEX, 2, 2)
                else:
                    pass
            except:
                pass

                # Show required images
            cv2.imshow("Gesture", frame)
            all_image = np.hstack((drawing, crop_image))
            cv2.imshow('Contours', all_image)

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