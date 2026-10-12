#include "DungeonMinimapWorldSubsystem.h"

#include "DungeonMinimapComponent.h"
#include "Core/LogMacrosEx.h"
//#include "Chaos/Public/Chaos/AABB.h"

void FDungeonMinimapOctreeSemantics::SetElementId(FOctree& OctreeOwner, const FDungeonMinimapOctreeElement& Element,
	FOctreeElementId2 Id)
{
	((FDungeonMinimapOCTree&)OctreeOwner).SetElementIdImpl(Element.OwnerUniqueId, Id);
}

FDungeonMinimapOCTree::FDungeonMinimapOCTree(const FVector& Origin, float Radius) : TOctree2(Origin, Radius)
{
}

FDungeonMinimapOCTree::~FDungeonMinimapOCTree()
{
	ObjectToOctreeIds.Empty();
}

void FDungeonMinimapOCTree::SetElementIdImpl(const uint32 OwnerUniqueId, FOctreeElementId2 Id)
{
	ObjectToOctreeIds.Add(OwnerUniqueId, Id);
}

void FDungeonMinimapOCTree::UpdateElement(const FOctreeElementId2& Id)
{
	SCOPED_NAMED_EVENT(FDungeonMinimapOCTree_UpdateElement, FColor::Yellow);
	FDungeonMinimapOctreeElement ElementCopy = GetElementById(Id);
	RemoveElement(Id);
	if (ObjectToOctreeIds.Find(ElementCopy.OwnerUniqueId) != nullptr)
	{
		ObjectToOctreeIds.Remove(ElementCopy.OwnerUniqueId);
	}
	AddElement(ElementCopy);
}

void UDungeonMinimapManager::Initialize()
{
	StaticOCTree = MakeShareable(new FDungeonMinimapOCTree(OcTreeSpace.GetCenter(), OcTreeSpace.GetExtent().GetMax()));

	// add all receivers
	for (const auto KV : CollectComponents)
	{
		if(KV.Value->bStaticElement)
		{
			if(StaticOCTree != nullptr)
			{
				StaticOCTree->AddElement(FDungeonMinimapOctreeElement(KV.Value));
			}
		}
	}
}

void UDungeonMinimapManager::Deinitialize()
{
}

void UDungeonMinimapManager::Clear()
{
	if(StaticOCTree != nullptr)
	{
		StaticOCTree->Destroy();
	}

	DynamicCollectComponents.Reset();
	CollectComponents.Reset();
}


int32 UDungeonMinimapManager::AddMinimapElementCollector(UDungeonMinimapComponent* Cmp)
{
	if(Cmp == nullptr)
	{
		UE_LOG(LogTemp, Error, TEXT("UDungeonMinimapManager::AddMinimapElementCollector with nullptr"));
		return INDEX_NONE;
	}
	
	if(CollectComponents.Contains(Cmp->UniqueIndex))
	{
		UE_LOG(LogTemp, Warning, TEXT("UDungeonMinimapManager::AddMinimapElementCollector [%s] has been register with [%d]"), *Cmp->GetName(), Cmp->UniqueIndex);
		return Cmp->UniqueIndex;
	}
	CollectComponents.Add(++AllocatedIndex, Cmp);
	Cmp->UniqueIndex = AllocatedIndex;

	if(Cmp->bStaticElement)
	{
		if(StaticOCTree != nullptr)
		{
			StaticOCTree->AddElement(FDungeonMinimapOctreeElement(Cmp));
		}
	}
	else
	{
		DynamicCollectComponents.Add(AllocatedIndex, Cmp);
	}
	
	return AllocatedIndex;
}

bool UDungeonMinimapManager::UpdateMinimapElementCollector(int32 CmpIndex)
{
	if(UDungeonMinimapComponent** Find = CollectComponents.Find(CmpIndex))
	{
		if((*Find)->bStaticElement)
		{
			const FOctreeElementId2* ElementId = StaticOCTree->GetReceiverOctreeId((*Find));
			if (ElementId != nullptr)
			{
				StaticOCTree->UpdateElement(*ElementId);
				return true;
			}
			else
			{
				UE_LOG(LogTemp, Warning, TEXT("UDungeonMinimapManager::UpdateMinimapElementCollector cant find Octree element with %s"), *(*Find)->GetName());
			}
		}
	}
	else
	{
		UE_LOG(LogTemp, Warning, TEXT("UDungeonMinimapManager::UpdateMinimapElementCollector cant find cmp with %d"), CmpIndex);
	}

	return false;
}

void UDungeonMinimapManager::RemoveMinimapElementCollector(int32 CmpIndex)
{
	if(UDungeonMinimapComponent** Find = CollectComponents.Find(CmpIndex))
	{
		if((*Find)->bStaticElement)
		{
			StaticOCTree->RemoveReceiverOctreeId(*Find);
		}
		else
		{
			DynamicCollectComponents.Remove(CmpIndex);
		}
		
		CollectComponents.Remove(CmpIndex);
	}
}

void UDungeonMinimapManager::GetCollectCmpOverlapBox(const FBoxCenterAndExtent& QueryBox, TArray<UDungeonMinimapComponent*>& OutCmps)
{
	for (const auto& DynamicCollectComponent : DynamicCollectComponents)
	{
		if(DynamicCollectComponent.Value)
		{
			if(QueryBox.GetBox().Intersect(DynamicCollectComponent.Value->GetBox()))
			{
				OutCmps.Add(DynamicCollectComponent.Value);
			}
		}
	}

	TArray<int32> StaticCmpsNeedRemove;
	StaticOCTree->FindElementsWithBoundsTest(QueryBox, [&OutCmps](const FDungeonMinimapOctreeElement& Element)
	{
		if(Element.Receiver.IsValid())
		{
			OutCmps.Add(Element.Receiver.Get());
		}
	});
}

void UDungeonMinimapManager::GetCollectCmpAll(TArray<UDungeonMinimapComponent*>& OutCmps)
{
	for (const auto& DynamicCollectComponent : DynamicCollectComponents)
	{
		if(DynamicCollectComponent.Value)
		{
			OutCmps.Add(DynamicCollectComponent.Value);
		}
	}

	TArray<int32> StaticCmpsNeedRemove;
	StaticOCTree->FindAllElements([&OutCmps](const FDungeonMinimapOctreeElement& Element)
	{
		if(Element.Receiver.IsValid())
		{
			OutCmps.Add(Element.Receiver.Get());
		}
	});
}

void UDungeonMinimapManager::RebuildAllElenments()
{
	for (TTuple<signed int, UDungeonMinimapComponent*> DynamicCollectComponent : DynamicCollectComponents)
	{
		if(DynamicCollectComponent.Value)
		{
			DynamicCollectComponent.Value->bNeedRecollect = true;
		}
	}

	TArray<UDungeonMinimapComponent*> CmpsNeedUpdate;
	StaticOCTree->FindAllElements([&CmpsNeedUpdate](const FDungeonMinimapOctreeElement& Element)
	{
		if(Element.Receiver.IsValid())
		{
			Element.Receiver->bNeedRecollect = true;
			CmpsNeedUpdate.Add(Element.Receiver.Get());
		}
	});
	StaticOCTree->Destroy();
	
	for (UDungeonMinimapComponent* Cmp : CmpsNeedUpdate)
	{
		StaticOCTree->AddElement(FDungeonMinimapOctreeElement(Cmp));
	}
}

UDungeonMinimapWorldSubsystem* UDungeonMinimapWorldSubsystem::Instance = nullptr;

UDungeonMinimapWorldSubsystem* UDungeonMinimapWorldSubsystem::GetInstance()
{
	return Instance;
}

void UDungeonMinimapWorldSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);

	Instance = this;
}

void UDungeonMinimapWorldSubsystem::Deinitialize()
{
	Super::Deinitialize();

	Instance = nullptr;
}

bool UDungeonMinimapWorldSubsystem::DoesSupportWorldType(EWorldType::Type WorldType) const
{
	return WorldType == EWorldType::Game || WorldType == EWorldType::PIE;
}

bool UDungeonMinimapWorldSubsystem::ShouldCreateSubsystem(UObject* Outer) const
{
	bool Result = UWorldSubsystem::ShouldCreateSubsystem(Outer);
	
	return Result;
}

int32 UDungeonMinimapWorldSubsystem::AddMinimapElementCollector(FName MinimapName, UDungeonMinimapComponent* Cmp)
{
	if(UDungeonMinimapManager* Manager = FindOreCreateManager(MinimapName))
	{
		return Manager->AddMinimapElementCollector(Cmp);
	}

	UE_LOG(LogTemp, Warning, TEXT("[UDungeonMinimapWorldSubsystem]::AddMinimapElementCollector Fail to get Minimap with [%s]"), *MinimapName.ToString());	
	return INDEX_NONE;
}

bool UDungeonMinimapWorldSubsystem::UpdateMinimapElementCollector(FName MinimapName, int32 CmpIndex)
{
	if(UDungeonMinimapManager* Manager = FindOreCreateManager(MinimapName))
	{
		return Manager->UpdateMinimapElementCollector(CmpIndex);
	}
	else
	{
		UE_LOG(LogTemp, Warning, TEXT("[UDungeonMinimapWorldSubsystem]::UpdateMinimapElementCollector Fail to get Minimap with [%s]"), *MinimapName.ToString());
	}

	return false;
}

void UDungeonMinimapWorldSubsystem::RemoveMinimapElementCollector(FName MinimapName, int32 CmpIndex)
{
	if(UDungeonMinimapManager* Manager = FindOreCreateManager(MinimapName))
	{
		Manager->RemoveMinimapElementCollector(CmpIndex);
	}
	else
	{
		UE_LOG(LogTemp, Warning, TEXT("[UDungeonMinimapWorldSubsystem]::RemoveMinimapElementCollector Fail to get Minimap with [%s]"), *MinimapName.ToString());
	}
}

void UDungeonMinimapWorldSubsystem::GetCollectCmpOverlapBox(FName MinimapName, const FBoxCenterAndExtent& QueryBox,
	TArray<UDungeonMinimapComponent*>& OutCmps)
{
	if(UDungeonMinimapManager** ManagerPtr = MinimapManager.Find(MinimapName))
	{
		(*ManagerPtr)->GetCollectCmpOverlapBox(QueryBox, OutCmps);
	}
	// else
	// {
	// 	UE_LOG(LogTemp, Warning, TEXT("[UDungeonMinimapWorldSubsystem]::GetCollectCmpOverlapBox Fail to get Minimap with [%s]"), *MinimapName.ToString());
	// }
}

void UDungeonMinimapWorldSubsystem::GetCollectCmpAll(FName MinimapName, TArray<UDungeonMinimapComponent*>& OutCmps)
{
	if(UDungeonMinimapManager** ManagerPtr = MinimapManager.Find(MinimapName))
	{
		(*ManagerPtr)->GetCollectCmpAll(OutCmps);
	}
	// else
	// {
	// 	UE_LOG(LogTemp, Warning, TEXT("[UDungeonMinimapWorldSubsystem]::GetCollectCmpAll Fail to get Minimap with [%s]"), *MinimapName.ToString());
	// }
}

UDungeonMinimapManager* UDungeonMinimapWorldSubsystem::FindOreCreateManager(FName MinimapName)
{
	UDungeonMinimapManager* Manager = nullptr;;
	UDungeonMinimapManager** ManagerPtr = MinimapManager.Find(MinimapName);
	if(ManagerPtr == nullptr)
	{
		MinimapManager.Add(MinimapName, NewObject<UDungeonMinimapManager>(GetWorld(), MinimapName));
		ManagerPtr = MinimapManager.Find(MinimapName);
		if(ManagerPtr != nullptr)
		{
			// {
			// 	TArray<float> Data1;
			// 	Data1.Init(NAN, 52);
			// 	TArray<uint8> Data2;
			// 	Data2.Init(0x55, 224);
			// 	TArray<uint8> Data3;
			// 	Data3.Init(0xc0, 208);
			// }
			Manager = *ManagerPtr;
			Manager->Initialize();
		}
	}
	else
	{
		Manager = *ManagerPtr;	
	}

	return Manager;
}

void UDungeonMinimapWorldSubsystem::SetMonitorEnable(bool bEnable)
{
	MonitorEnable = bEnable;
}

void UDungeonMinimapWorldSubsystem::SetMonitorFocusActor(AActor* Actor)
{
	MonitorFocusActor = Actor;
}
