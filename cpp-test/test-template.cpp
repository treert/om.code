#include<iostream>

struct A{
    int a;
    int b;
    A(){
        a = 2;
        puts("A()");
    }
};

template <typename Type, typename... Types>
struct TypeRegister{
    template<typename Queried_type>
    static constexpr int id(){
        if constexpr (std::is_same_v<Type, Queried_type>) return 0;
        else{
            static_assert((sizeof...(Types) > 0), "You shan't query a type you didn't register first");
            return 1 + TypeRegister<Types...>::template id<Queried_type>();
        }
    }
};

typedef int MyDefInt;
using MyUsingInt = int;



using reg_map = TypeRegister<
    int,
    float,
    char,
    int const&,
    const int&,
    int&,
    const int&&,
    int&&,
    bool
    >;

#define PrintId(T) {std::cout << reg_map::id<T>() << std::endl;}
#define Println(Str) {std::cout << #Str << std::endl;}

int main(){
    
    A a;
    printf("%d %d\n",a.a,a.b);
    A b{};
    printf("%d %d\n",b.a,b.b);

    PrintId(int);
    PrintId(float);
    PrintId(char);
    PrintId(int const&);
    PrintId(const int&);
    PrintId(int&);
    PrintId(const int&&);
    PrintId(int&&);
    PrintId(bool);

    Println(  test typedef and using );

    PrintId(MyDefInt);
    PrintId(MyUsingInt);

    // PrintId(int16_t);// compile error

    // std::cout << reg_map::id<&&>() << std::endl;
    return 0;
}