TEMPLATE = app
CONFIG += console
CONFIG -= qt

SOURCES += main.cpp \
    facedetector.cpp \
    face.cpp \
    logger.cpp

########## OpenCV

INCLUDEPATH += /usr/local/include/opencv2/

LIBS += -L/usr/local/lib/ \
-lopencv_imgproc \
-lopencv_core \
-lopencv_highgui \
-lopencv_calib3d \
-lopencv_video \
-lopencv_videostab \
-lopencv_features2d \
-lopencv_objdetect \
-loscpack

CONFIG+=link_pkgconfig PKGCONFIG+=opencv

HEADERS += \
    facedetector.h \
    face.h \
    logger.h
