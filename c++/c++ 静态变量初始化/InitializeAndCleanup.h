#pragma once
#include<string>

/**
应用地方：
1. 注册全局回调，如：网络协议处理

Example1

RegisterRuntimeInitialize(test1,[]{
	// ...
},nullptr,1);

int main(int argc, char* argv[])
{
	RegisterRuntimeInitializeAndCleanup.ExecuteInitializations();

	// ...

	RegisterRuntimeInitializeAndCleanup.ExecuteCleanup();
}

PS: 比较灵活，可以注册任意想在main之前调用的函数。手写十分不方便。
	感觉更好的方式是用工具生成注册代码。
*/

class RegisterRuntimeInitializeAndCleanup
{
public:
	typedef void CallbackFunction ();
	RegisterRuntimeInitializeAndCleanup(CallbackFunction* Initialize, CallbackFunction* Cleanup, std::string name = "", int order = 0);

	static void ExecuteInitializations();
	static void ExecuteCleanup();
};

#define RegisterRuntimeInitialize(NAME,FUNC_INIT,FUNC_CLEAR,ORDER) \
static RegisterRuntimeInitializeAndCleanup NAME(FUNC_INIT,FUNC_CLEAR,#NAME,ORDER)