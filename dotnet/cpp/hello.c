#include <stdint.h>
#include <stdio.h>
#include <string.h>

#ifdef _WIN32
#define MY_API __declspec(dllexport)
#else
#define MY_API __attribute__ ((visibility ("default")))
#endif

MY_API int32_t Add(int32_t a, int32_t b) {
    return a + b;
}

MY_API int32_t GetStrLen(const char*str, int print) {
    if (print) puts(str);
    int len = 0;
    while (*str)
    {
        len++;
        str++;
    }
    return len;
}

MY_API const char* ConvertIntToStr(int n)
{
    static char buff[32] = {'\0'};
    sprintf(buff, "n=%d",n);
    return buff;
}


struct Data
{	
	int32_t i;
    int8_t arr[3]; 
};

MY_API void GetName(struct Data *data)
{
	data->i = 123;
    data->arr[0] = 0;
    data->arr[1] = 1;
    data->arr[2] = 2;
}