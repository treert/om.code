#include "test-template.h"

template<> class A<char*> {
	const char* str;
public:
	A() {
		str = "A' special definition 11111";
	}
	void print() {
		std::cout<<str<<std::endl;
	}
};

void ff1(){
    A<int> a1;      //显示模板实参的隐式实例化
	a1.print();
	A<char*> a2;    //使用特化的类模板
	a2.print();
}