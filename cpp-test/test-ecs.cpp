#include<iostream>
#include<cstdint>
#include<time.h>
#include<memory>
#include<thread>

#include "utils.h"

/*
总结：
1. 队伍单线程运行。SOA 只有在数组内存大于L2缓存时，才会明显提速。
2. 多线程并行时。SOA可以避免 Cache Line 机制导致的伪共享问题，显著提升性能。
    - g++ -O3 编译的情况下，这条的效果又没有了。

总打来说。SOA至少没什么坏处，或许有好处。
ECS 运用SOA或许能提升不少性能。更重要的是组织管理代码的方式发生改变。
*/

/*   测试结果： Ryzen 7950x

C:\MyGit\om.code\cpp-test>g++ -O2 test-ecs.cpp && a.exe
sizeof(FItem) = 28 Num = 4681 LoopNum = 16384

============= Test AOS ====================
FTestAOS test_rw_all cost: 58.101ms
FTestAOS test_parall cost: 223.885ms
FTestAOS test_rw_pos cost: 29.031ms
FTestAOS test_rw_attr cost: 19.064ms
FTestAOS test_rw_move cost: 28.461ms

============= Test SOA ====================
FTestSOA test_rw_all cost: 59.487ms
FTestSOA test_parall cost: 29.487ms
FTestSOA test_rw_pos cost: 29.171ms
FTestSOA test_rw_attr cost: 16.142ms
FTestSOA test_rw_move cost: 28.635ms

C:\MyGit\om.code\cpp-test>g++ -O3 test-ecs.cpp && a.exe 
sizeof(FItem) = 28 Num = 4681 LoopNum = 16384

============= Test AOS ====================
FTestAOS test_rw_all cost: 98.242ms
FTestAOS test_parall cost: 47.513ms
FTestAOS test_rw_pos cost: 41.905ms
FTestAOS test_rw_attr cost: 3.520ms
FTestAOS test_rw_move cost: 42.163ms

============= Test SOA ====================
FTestSOA test_rw_all cost: 42.164ms
FTestSOA test_parall cost: 46.065ms
FTestSOA test_rw_pos cost: 41.891ms
FTestSOA test_rw_attr cost: 3.833ms
FTestSOA test_rw_move cost: 42.104ms
*/


// 发现 Prefetch 对于 AOS 的效果也不大
#if (defined(_M_IX86) || defined(_M_X64))
#define USE_Prefetch 0
#endif

#if USE_Prefetch
#include <intrin.h>
#endif

#if USE_Prefetch
__forceinline static void Prefetch(const void* Ptr){
    _mm_prefetch(static_cast<const char*>(Ptr), _MM_HINT_T0);
}
#else
#define Prefetch(Ptr) {}
#endif

// #define PaddingSize 12

struct FPos
{
    float X;
    float Y;
    float Z;
    #ifdef PaddingSize
    char padding[PaddingSize];
    #endif
    void Update(){
        X += 1.0f;
        Y -= 1.0f;
        Z = X + Y; 
    }
};

struct FAttr
{
    int32_t Blood;
    int32_t Magic;
    #ifdef PaddingSize
    char padding[PaddingSize];
    #endif
    void Update(){
        Blood -= 10;
        Magic ^= 0x1243123;
    }
};

struct FMove
{
    float Speed;
    float Dis;
    #ifdef PaddingSize
    char padding[PaddingSize];
    #endif
    void Update(){
        Speed += 1.4f;
        Dis -= Speed * 0.03f;
    }
};

struct FItem
{
    FPos Pos;
    FAttr Attr;
    FMove Move;
};


constexpr int CacheLineSize = 64;


#define my_test_all() {\
        test_rw_all();\
        test_parall();\
        test_rw_pos();\
        test_rw_attr();\
        test_rw_move();\
        /*test_rw_move();*/  /* 这一行会影响test_rw_attr g++ -O2 情况下*/ \
        printf("\n");\
}



template<int Num, int LoopNum>
struct FTestAOS
{
    alignas(CacheLineSize) FItem Items[Num];

    void test(){
        printf("============= Test AOS ====================\n");
        my_test_all();
    }
    
    void test_rw_all(){
        FLogCostTime log_time("FTestAOS test_rw_all");
        for (int i = 0;  i < LoopNum; i++){
            for (int k = 0; k < Num; k++){
                FItem *Data = Items + k;
                Prefetch(Data);
                Data->Pos.Update();
                Data->Attr.Update();
                Data->Move.Update();
            }
        }
    }

    void test_parall(){
        FLogCostTime log_time("FTestAOS test_parall");
        std::thread t1([&](){
            for (int i = 0;  i < LoopNum; i++){
                for (int k = 0; k < Num; k++){
                    Items[k].Pos.Update();
                }
            }
        });
        std::thread t2([&](){
            for (int i = 0;  i < LoopNum; i++){
                for (int k = 0; k < Num; k++){
                    Items[k].Attr.Update();
                }
            }
        });
        std::thread t3([&](){
            for (int i = 0;  i < LoopNum; i++){
                for (int k = 0; k < Num; k++){
                    Items[k].Move.Update();
                }
            }
        });
        t1.join();
        t2.join();
        t3.join();
    }

    void test_rw_pos(){
        FLogCostTime log_time("FTestAOS test_rw_pos");
        for (int i = 0;  i < LoopNum; i++){
            for (int k = 0; k < Num; k++){
                FItem *Data = Items + k;
                Prefetch(Data);
                // Data->Attr.Update();
                // Data->Move.Update();
                Data->Pos.Update();
            }
        }
    }

    void test_rw_attr(){
        FLogCostTime log_time("FTestAOS test_rw_attr");
        for (int i = 0;  i < LoopNum; i++){
            for (int k = 0; k < Num; k++){
                FItem *Data = Items + k;
                Prefetch(Data);
                Data->Attr.Update();
                // Data->Move.Update();
                // Data->Pos.Update();
            }
        }
    }

    void test_rw_move(){
        FLogCostTime log_time("FTestAOS test_rw_move");
        for (int i = 0;  i < LoopNum; i++){
            for (int k = 0; k < Num; k++){
                FItem *Data = Items + k;
                Prefetch(Data);
                // Data->Attr.Update();
                Data->Move.Update();
                // Data->Pos.Update();
            }
        }
    }
};

template<int Num, int LoopNum>
struct FTestSOA
{
    alignas(CacheLineSize) FPos PosData[Num];
    alignas(CacheLineSize) FAttr AttrData[Num];
    alignas(CacheLineSize) FMove MoveData[Num];

    void test(){
        printf("============= Test SOA ====================\n");
        my_test_all();
    }

    void test_rw_all(){
        FLogCostTime log_time("FTestSOA test_rw_all");
        for (int i = 0;  i < LoopNum; i++){
            for (int k = 0; k < Num; k++){
                PosData[k].Update();
                AttrData[k].Update();
                MoveData[k].Update();
            }
        }
    }

    void test_parall(){
        FLogCostTime log_time("FTestSOA test_parall");
        std::thread t1([&](){
            for (int i = 0;  i < LoopNum; i++){
                for (int k = 0; k < Num; k++){
                    PosData[k].Update();
                }
            }
        });
        std::thread t2([&](){
            for (int i = 0;  i < LoopNum; i++){
                for (int k = 0; k < Num; k++){
                    AttrData[k].Update();
                }
            }
        });
        std::thread t3([&](){
            for (int i = 0;  i < LoopNum; i++){
                for (int k = 0; k < Num; k++){
                    MoveData[k].Update();
                }
            }
        });
        t1.join();
        t2.join();
        t3.join();
    }

    void test_rw_pos(){
        FLogCostTime log_time("FTestSOA test_rw_pos");
        for (int i = 0;  i < LoopNum; i++){
            for (int k = 0; k < Num; k++){
                PosData[k].Update();
                // AttrData[k].Update();
                // MoveData[k].Update();
            }
        }
    }

    void test_rw_attr(){
        FLogCostTime log_time("FTestSOA test_rw_attr");
        for (int i = 0;  i < LoopNum; i++){
            for (int k = 0; k < Num; k++){
                // PosData[k].Update();
                AttrData[k].Update();
                // MoveData[k].Update();
            }
        }
    }

    void test_rw_move(){
        FLogCostTime log_time("FTestSOA test_rw_move");
        for (int i = 0;  i < LoopNum; i++){
            for (int k = 0; k < Num; k++){
                // PosData[k].Update();
                // AttrData[k].Update();
                MoveData[k].Update();
            }
        }
    }
};

int main()
{
    constexpr int ChunkSize = 64*1024;
    {
        constexpr int ScaleChunckSize = 2;
        constexpr int Num = ChunkSize * ScaleChunckSize / sizeof(FItem);
        constexpr int LoopNum = 1024*32 / ScaleChunckSize;

        printf("sizeof(FItem) = %d Num = %d LoopNum = %d\n\n", sizeof(FItem), Num, LoopNum);

        // {
        //     FTestAOS<Num, LoopNum> test_aos;
        //     test_aos.test();
        // // }
        // // {
        //     FTestSOA<Num, LoopNum> test_soa;
        //     test_soa.test();
        // }

        {
            auto test_aos = std::make_shared<FTestAOS<Num, LoopNum>>();
            test_aos->test();
        }
        {
            auto test_soa = std::make_shared<FTestSOA<Num, LoopNum>>();
            test_soa->test();
        }
    }
}