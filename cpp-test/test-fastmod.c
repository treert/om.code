#include <stdint.h>
#include <math.h>
#include <stdio.h>
#include <time.h>
#include <stdlib.h>

struct FastModData
{
    uint32_t divisor;
    uint64_t fastmod_multplier;
};

// FastMod
// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/HashHelpers.cs
// https://github.com/dotnet/runtime/pull/406

inline static uint32_t helper_FastMod(uint32_t value, uint32_t divisor, uint64_t multiplier){
    uint32_t highbits = (uint32_t)(((((multiplier * value) >> 32) + 1) * divisor) >> 32);
    return highbits;
}

#define FastMod(v,d,m) (helper_FastMod(v,d,m))
// #define FastMod(v,d,m) ((uint32_t)(((((m * v) >> 32) + 1) * d) >> 32)) // -O0 时会快一点点
#define FastModDD(v,dd) FastMod(v,(dd)->divisor,(dd)->fastmod_multplier)
#define NormalMod(v,d) (v%d)
#define NormalModDD(v,dd) NormalMod(v,(dd)->divisor)
int main(){
    static const uint32_t Primes[] = {
        3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
        1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
        17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
        187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
        1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
    };

    const int size = sizeof(Primes)/sizeof(int32_t);
    struct FastModData data[sizeof(Primes)/sizeof(int32_t)];

    for( int i = 0; i < size; i++){
        data[i].divisor = Primes[i];
        data[i].fastmod_multplier = UINT64_MAX/Primes[i] + 1;
    }

    puts("test begin");
    const int NUM = 99999999;
    int count = 0;
    clock_t t1 = clock();

    srand(time(NULL)); // use current time as seed for random generator
    int random_variable = rand() % 6;

    for(int i = 1; i < NUM; i++){
        int l2 = FastModDD(i, data + 30 + random_variable - 1);
        int l1 = FastModDD(i, data + 30 + random_variable - 3);
        count += l1 +l2;
    }

    clock_t t2 = clock();

    for(int i = 1; i < NUM; i++){
        int l2 = NormalModDD(i, data + 30 + random_variable - 1);
        int l1 = NormalModDD(i, data + 30 + random_variable - 3);
        count += l1 +l2;
    }

    clock_t t3 = clock();

    for(int i = 1; i < NUM; i++){
        struct FastModData *dd = data + 30 - random_variable - 1;
        int l1 = FastModDD(i, dd);
        int l2 = NormalModDD(i, dd);
        if (l1 != l2){
            printf("fast1 mod error %u %u %u\n", i, dd->divisor,dd->fastmod_multplier);
            break;
        }
    }

    #define getcost(t1,t2) (1000.0*(t2-t1)/CLOCKS_PER_SEC)

    double c1 = getcost(t1,t2);
    double c2 = getcost(t2,t3);

    printf("cost: %.2f %.2f\n",c1,c2);

    return count != 0;
}

/**
上面的测试代码，FastMod 耗时最好时大约是 % 的 1/3
gcc -O0 test-fastmod.c && a.exe  # cost: 622.00 444.00
gcc -O1 test-fastmod.c && a.exe  # cost: 103.00 310.00
gcc -O2 test-fastmod.c && a.exe  # cost: 101.00 314.00
gcc -O3 test-fastmod.c && a.exe  # cost: 192.00 308.00

-O3 负优化了。
*/