#include <iostream>

template<typename T1,typename T2 = int> struct Tuple;

template<typename T1,typename T2>
struct Tuple{
    T1 a1;
    T2 a2;
    static void PrintInfo(){
        std::cout << typeid(T2).name() << std::endl;
    }
};

template<class T, class U = int> class A;
template<class T = float, class U> class A;

template<class T, class U> class A {
   public:
      T x;
      U y;
    static void PrintInfo(){
        std::cout << typeid(T).name() << std::endl;
        std::cout << typeid(U).name() << std::endl;
    }
};

A<> a;

int main(){
    Tuple<bool>::PrintInfo();
    Tuple<bool,float>::PrintInfo();
    A<>::PrintInfo();
    return 0;
}