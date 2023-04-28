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

static const uint32_t s_primes[31] = {
    1,3,5,11,19,41,79,157,317,631,1259,2521,5039,10079,20161,40343,80611,161221,322459,644881,1289749,2579513,5158999,10317991,20635981,41271991,82543913,165087817,330175613,660351239,1320702451
};
static const uint64_t s_primes_fastmod_multiplier[31] = {
    0,6148914691236517206ull,3689348814741910324ull,1676976733973595602ull,970881267037344822ull,449920587163647601ull,233503089540627236ull,117495185182863387ull,58191621683626346ull,29234142747558719ull,14651901567680343ull,7317232873347700ull,3660794616731406ull,1830215703314769ull,914971681648210ull,457247702791304ull,228836561681527ull,114418990539133ull,57206479191803ull,28604880704672ull,14302584513506ull,7151250671623ull,3575644049110ull,1787823237461ull,893911662049ull,446955516969ull,223477945294ull,111738978739ull,55869492923ull,27934745912ull,13967373242ull,
};

// 尝试定义一个结构体。这样只用读一次内存的样子。在这个测试样例中，没什么区别的样子
#ifndef USE_STRUCT_PRIME
#define USE_STRUCT_PRIME 1
#endif
static const struct FastModStruct
{
    uint32_t divisor;
    uint64_t multiplier;
} s_fastmod_primes[31] = {
    {1,0},{3,6148914691236517206ull},{5,3689348814741910324ull},{11,1676976733973595602ull},{19,970881267037344822ull},{41,449920587163647601ull},{79,233503089540627236ull},{157,117495185182863387ull},{317,58191621683626346ull},{631,29234142747558719ull},{1259,14651901567680343ull},{2521,7317232873347700ull},{5039,3660794616731406ull},{10079,1830215703314769ull},{20161,914971681648210ull},{40343,457247702791304ull},{80611,228836561681527ull},{161221,114418990539133ull},{322459,57206479191803ull},{644881,28604880704672ull},{1289749,14302584513506ull},{2579513,7151250671623ull},{5158999,3575644049110ull},{10317991,1787823237461ull},{20635981,893911662049ull},{41271991,446955516969ull},{82543913,223477945294ull},{165087817,111738978739ull},{330175613,55869492923ull},{660351239,27934745912ull},{1320702451,13967373242ull}
};


inline static uint32_t helper_Mod_Pow2_FastMod(uint32_t value, uint8_t size){
    #if USE_STRUCT_PRIME
    struct FastModStruct data = s_fastmod_primes[size];
    return helper_FastMod(value, data.divisor, data.multiplier);
    #else
    uint32_t d = s_primes[size];
    uint64_t m = s_primes_fastmod_multiplier[size];
    return helper_FastMod(value, d, m);
    #endif
}

inline static uint32_t helper_Mod_Pow2(uint32_t value, uint8_t size){
    #if USE_STRUCT_PRIME
    uint32_t d = s_fastmod_primes[size].divisor;
    return value % d;
    #else
    uint32_t d = s_primes[size];
    return value % d;
    #endif
}

#define FastMod(v,d,m) (helper_FastMod(v,d,m))
// #define FastMod(v,d,m) ((uint32_t)(((((m * v) >> 32) + 1) * d) >> 32)) // -O0 时会快一点点
#define FastModDD(v,dd) FastMod(v,(dd)->divisor,(dd)->fastmod_multplier)
#define NormalMod(v,d) (v%d)
#define NormalModDD(v,dd) NormalMod(v,(dd)->divisor)


int test_fastmod(int NUM){
    puts("test_fastmod begin");
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
            printf("test_fastmod error %u %u %llu\n", i, dd->divisor,dd->fastmod_multplier);
            break;
        }
    }

    #define getcost(t1,t2) (1000.0*(t2-t1)/CLOCKS_PER_SEC)

    double c1 = getcost(t1,t2);
    double c2 = getcost(t2,t3);

    printf("cost: %.2f %.2f\n",c1,c2);
    return count;
}

int test_fastmod_Pow2(int NUM){
    puts("test_fastmod_Pow2 begin");
    int count = 0;
    clock_t t1 = clock();

    srand(time(NULL)); // use current time as seed for random generator
    uint8_t random_variable = rand() % 30;

    for(int i = 1; i < NUM; i++){
        int l2 = helper_Mod_Pow2_FastMod(i, random_variable);
        int l1 = helper_Mod_Pow2_FastMod(i, random_variable + 1);
        count += l1 +l2;
    }

    clock_t t2 = clock();

    for(int i = 1; i < NUM; i++){
        int l2 = helper_Mod_Pow2(i, random_variable);
        int l1 = helper_Mod_Pow2(i, random_variable + 1);
        count += l1 +l2;
    }

    clock_t t3 = clock();

    for(int i = 1; i < NUM; i++){
        int l1 = helper_Mod_Pow2_FastMod(i, random_variable);
        int l2 = helper_Mod_Pow2(i, random_variable);
        if (l1 != l2){
            printf("test_fastmod_Pow2 error %u %u %llu\n", i, s_primes[random_variable],s_primes_fastmod_multiplier[random_variable]);
            break;
        }
    }

    #define getcost(t1,t2) (1000.0*(t2-t1)/CLOCKS_PER_SEC)

    double c1 = getcost(t1,t2);
    double c2 = getcost(t2,t3);

    printf("cost: %.2f %.2f\n",c1,c2);
    return count;
}

int main(){
    printf("USE_STRUCT_PRIME=%d\n",USE_STRUCT_PRIME);
    test_fastmod(99999999);
    test_fastmod_Pow2(99999999);
    return 0;
}

/**
上面的测试代码，FastMod 耗时最好时大约是 % 的 1/3
gcc -O0 test-fastmod.c && a.exe  # cost: 622.00 444.00
gcc -O1 test-fastmod.c && a.exe  # cost: 103.00 310.00
gcc -O2 test-fastmod.c && a.exe  # cost: 101.00 314.00
gcc -O3 test-fastmod.c && a.exe  # cost: 192.00 308.00

-O3 负优化了。-O1 或者 -O2 都可以。
*/
