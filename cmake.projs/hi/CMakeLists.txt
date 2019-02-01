cmake_minimum_required(VERSION 3.0)

project(hi)

set(PROJECT_VERSION_MAJOR "1")
set(PROJECT_VERSION_MINOR "0")
set(PROJECT_VERSION_PATCH "0")
set(PROJECT_VERSION_EXTRA "")
set(PROJECT_VERSION "${PROJECT_VERSION_MAJOR}.${PROJECT_VERSION_MINOR}")
set(PROJECT_VERSION_FULL "${PROJECT_VERSION}.${PROJECT_VERSION_PATCH}${PROJECT_VERSION_EXTRA}")

include(../cmake.utils/funcs.cmake)

my_get_c_files(v_source src)
my_add_executable(hi ${v_source})

add_subdirectory(../protobuf protobuf.out)

target_include_directories(hi PRIVATE src)

target_link_libraries(hi protobuf)
