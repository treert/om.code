
template<typename T, int Size=4> class RingBuffer{
public:
    bool canput(){
        return _buffer[_write_idx%Size] == nullptr;
    }
    bool tryput(T* obj){
        if(!canput()) return false;
        _buffer[_write_idx%Size] = obj;
        ++_write_idx;
        return true;
    }
    bool caneat(){
        return _buffer[_read_idx%Size] != nullptr;
    }
    T* tryeat(){
        if (!caneat()) return nullptr;
        int idx = _read_idx%Size;
        ++_read_idx;
        T* obj = _buffer[idx];
        _buffer[idx] = nullptr;
        return obj;
    }
private:
    int _read_idx = 0;
    int _write_idx = 0;
    T* _buffer[Size] = {nullptr};
};

#include <iostream>
#include <thread>
#include <random>

int main(){
    RingBuffer<int> rb;
    std::thread t1([&rb](){
        std::random_device rd{};
        std::mt19937 gen{rd()};
        std::uniform_int_distribution<> distrib(50, 100);
        for(int i=1; i < 100; i++){
            std::chrono::milliseconds ms(distrib(gen));
            std::this_thread::sleep_for(ms);
            while(! rb.tryput(reinterpret_cast<int*>(i)));
            std::cout << "put " << i << std::endl;
        }
    });

    std::thread t2([&rb](){
        std::random_device rd{};
        std::mt19937 gen{rd()};
        std::uniform_int_distribution<> distrib(50, 100);
        for(int i=1; i < 100; i++){
            int*obj = nullptr;
            while((obj = rb.tryeat())==nullptr);
            std::cout << "eat " << reinterpret_cast<size_t>(obj) << std::endl;
            std::chrono::milliseconds ms(distrib(gen));
            std::this_thread::sleep_for(ms);
        }
    });

    t1.join();
    t2.join();
}