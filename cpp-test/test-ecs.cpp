#include<iostream>
#include<cstdint>
#include<time.h>

#define PaddingSize 32

struct FPos
{
    float X;
    float Y;
    float Z;
    char padding[PaddingSize];
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
    char padding[PaddingSize];
    void Update(){
        Blood -= 10;
        Magic ^= 0x1243123;
    }
};

struct FMove
{
    float Speed;
    float Dis;
    char padding[PaddingSize];
    void Update(){
        Speed += 1.4f;
        Dis -= Speed * 0.03f;
    }
};

const int ChunkSize = 64*1024;
const int ScaleChunckSize = 4;
const int LoopNum = 1024*32 / ScaleChunckSize;


struct FLogCostTime
{
    const char * msg;
    clock_t t1;
    FLogCostTime(const char* InMsg){
        t1 = clock();
        msg = InMsg;
    }
    ~FLogCostTime(){
        clock_t t2 = clock();
        double c1 = (1000.0*(t2-t1)/CLOCKS_PER_SEC);
        printf("%s cost: %.2lfms\n",msg, c1);
    }
};

struct FTestAOS
{
    struct FItem
    {
        FPos Pos;
        FAttr Attr;
        FMove Move;
    };

    FItem* Chunk = nullptr;
    int num = 0;

    FTestAOS(){
        int size = sizeof(FItem);
        num = ChunkSize/size * ScaleChunckSize;
        Chunk = new FItem[num];
        std::cout << "FTestAOS sizeof(FItem)=" << size << " num=" << num << std::endl;
    }

    ~FTestAOS(){
        delete Chunk;
        Chunk = nullptr;
        num = 0;
    }

    void test(){
        printf("============= Test AOS ====================\n");
        test_rw_pos();
        test_rw_all();
        test_rw_attr();
        test_rw_move();
        printf("\n");
    }
    
    void test_rw_all(){
        FLogCostTime log_time("FTestAOS test_rw_all");
        for (int i = 0;  i < LoopNum; i++){
            for (int k = 0; k < num; k++){
                FItem *Data = Chunk + k;
                Data->Attr.Update();
                Data->Move.Update();
                Data->Pos.Update();
            }
        }
    }

    void test_rw_pos(){
        FLogCostTime log_time("FTestAOS test_rw_pos");
        for (int i = 0;  i < LoopNum; i++){
            for (int k = 0; k < num; k++){
                FItem *Data = Chunk + k;
                // Data->Attr.Update();
                // Data->Move.Update();
                Data->Pos.Update();
            }
        }
    }

    void test_rw_attr(){
        FLogCostTime log_time("FTestAOS test_rw_attr");
        for (int i = 0;  i < LoopNum; i++){
            for (int k = 0; k < num; k++){
                FItem *Data = Chunk + k;
                Data->Attr.Update();
                // Data->Move.Update();
                // Data->Pos.Update();
            }
        }
    }

    void test_rw_move(){
        FLogCostTime log_time("FTestAOS test_rw_move");
        for (int i = 0;  i < LoopNum; i++){
            for (int k = 0; k < num; k++){
                FItem *Data = Chunk + k;
                // Data->Attr.Update();
                Data->Move.Update();
                // Data->Pos.Update();
            }
        }
    }
};

struct FTestSOA
{
    int8_t* Chunk = nullptr;
    int num = 0;

    FPos *PosPtr;
    FAttr *AttrPtr;
    FMove *MovePtr;

    FTestSOA(){
        Chunk = new int8_t[ChunkSize*ScaleChunckSize];
        int size = sizeof(FPos) + sizeof(FAttr) + sizeof(FMove);
        num = ChunkSize/size * ScaleChunckSize;
        std::cout << "FTestSOA sizeof(FItem)=" << size << " num=" << num << std::endl;
        PosPtr = reinterpret_cast<FPos *>(Chunk);
        AttrPtr = reinterpret_cast<FAttr *>(Chunk + (num * sizeof(FPos)));
        MovePtr = reinterpret_cast<FMove *>(Chunk + (num * (sizeof(FPos) + sizeof(FAttr))));
    }
    ~FTestSOA(){
        delete Chunk;
        Chunk = nullptr;
        num = 0;
    }

    void test(){
        printf("============= Test SOA ====================\n");
        test_rw_pos();
        test_rw_all();
        test_rw_attr();
        test_rw_move();
    }

    void test_rw_all(){
        FLogCostTime log_time("FTestAOS test_rw_all");
        for (int i = 0;  i < LoopNum; i++){
            for (int k = 0; k < num; k++){
                (PosPtr+k)->Update();
                (AttrPtr+k)->Update();
                (MovePtr+k)->Update();
            }
        }
    }

    void test_rw_pos(){
        FLogCostTime log_time("FTestAOS test_rw_pos");
        for (int i = 0;  i < LoopNum; i++){
            for (int k = 0; k < num; k++){
                (PosPtr+k)->Update();
                // (AttrPtr+k)->Update();
                // (MovePtr+k)->Update();
            }
        }
    }

    void test_rw_attr(){
        FLogCostTime log_time("FTestAOS test_rw_attr");
        for (int i = 0;  i < LoopNum; i++){
            for (int k = 0; k < num; k++){
                // (PosPtr+k)->Update();
                (AttrPtr+k)->Update();
                // (MovePtr+k)->Update();
            }
        }
    }

    void test_rw_move(){
        FLogCostTime log_time("FTestAOS test_rw_move");
        for (int i = 0;  i < LoopNum; i++){
            for (int k = 0; k < num; k++){
                // (PosPtr+k)->Update();
                // (AttrPtr+k)->Update();
                (MovePtr+k)->Update();
            }
        }
    }
};

int main()
{
    
    {
        FTestAOS test_aos;
        test_aos.test();
    }
    {
        FTestSOA test_soa;
        test_soa.test();
    }
    // {
    //     FTestAOS test_aos;
    //     test_aos.test();
    // }
}