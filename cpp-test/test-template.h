#include <iostream>

template<typename T>class A {
	T num;
public:
	A(){
		num=T(6.6);
	}
	void print(){
		std::cout<<"A'num:"<<num<<std::endl;
	}
};

