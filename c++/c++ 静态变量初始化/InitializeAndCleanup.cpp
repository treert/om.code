#include "InitializeAndCleanup.h"

enum { kMaxCount = 40 };

struct OrderedCallback
{
	int														order;
	std::string												name;
	RegisterRuntimeInitializeAndCleanup::CallbackFunction*	init;
	RegisterRuntimeInitializeAndCleanup::CallbackFunction*	cleanup;
};

static int				gNumRegisteredCallbacks = 0;
static OrderedCallback	gCallbacks[kMaxCount];


bool operator < (const OrderedCallback& lhs, const OrderedCallback& rhs)
{
	return lhs.order < rhs.order;
}

RegisterRuntimeInitializeAndCleanup::RegisterRuntimeInitializeAndCleanup(CallbackFunction* Initialize, CallbackFunction* Cleanup, std::string name, int order)
{
	gCallbacks[gNumRegisteredCallbacks].init = Initialize;
	gCallbacks[gNumRegisteredCallbacks].cleanup = Cleanup;
	gCallbacks[gNumRegisteredCallbacks].order = order;
	gCallbacks[gNumRegisteredCallbacks].name = name;

	gNumRegisteredCallbacks++;
	Assert(gNumRegisteredCallbacks <= kMaxCount);
}

void RegisterRuntimeInitializeAndCleanup::ExecuteInitializations()
{
	std::sort (gCallbacks, gCallbacks + gNumRegisteredCallbacks);
	
	for (int i = 0; i < gNumRegisteredCallbacks; i++)
	{
		if (gCallbacks[i].init)
			gCallbacks[i].init ();
	}
}

void RegisterRuntimeInitializeAndCleanup::ExecuteCleanup()
{
	for (int i = gNumRegisteredCallbacks-1; i >=0 ; i--)
	{
		if (gCallbacks[i].cleanup)
			gCallbacks[i].cleanup ();
	}
}