#include <chrono>
#include <cstdio>
#include <iostream>

struct FLogCostTime
{
    using TimePoint = std::chrono::high_resolution_clock::time_point;
    const char * msg;
    TimePoint start;
    FLogCostTime(const char* InMsg){
        using namespace std;
        using namespace chrono;
        start = high_resolution_clock::now();
        msg = InMsg;
    }
    ~FLogCostTime(){
        using namespace std;
        using namespace chrono;
        auto end = high_resolution_clock::now();
        const std::chrono::duration<double, std::milli> diff = end - start;
        // auto diff = duration<double>(end - start);
        // auto cost = double(duration.count()) * microseconds::period::num / microseconds::period::den ;
        // cout << diff.count() << endl;
        printf("%s cost: %.3lfms\n",msg, diff.count());
    }
};