cmake_minimum_required(VERSION 3.6)
project(hello)

if(WIN32)
    add_definitions("/wd4996")
endif(WIN32)


set(SRC_PATH ${CMAKE_CURRENT_SOURCE_DIR})
set (SRC_FILES ${SRC_PATH}/hello.c)

add_library (libhello_share SHARED ${SRC_FILES})
set_target_properties ( libhello_share PROPERTIES OUTPUT_NAME hello )

install(TARGETS libhello_share)