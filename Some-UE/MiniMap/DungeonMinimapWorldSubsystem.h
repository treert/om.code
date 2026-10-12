#pragma once
#include "DungeonMinimapComponent.h"
#include "Math/GenericOctree.h"

#include "DungeonMinimapWorldSubsystem.generated.h"

class AMoeCharacter;
class UDungeonMinimapComponent;

struct FDungeonMinimapOctreeElement
{
	TWeakObjectPtr<UDungeonMinimapComponent> Receiver;
	uint32 OwnerUniqueId = INDEX_NONE;

public:
	explicit FDungeonMinimapOctreeElement(UDungeonMinimapComponent* SourceReceiver)
		: Receiver(SourceReceiver)
		, OwnerUniqueId(SourceReceiver->UniqueIndex)
	{
		Bounds = FBoxSphereBounds(Receiver->GetBox());
	}
	
	FDungeonMinimapOctreeElement(const FDungeonMinimapOctreeElement& Other)
		: Receiver(Other.Receiver)
		, OwnerUniqueId(Other.OwnerUniqueId)
	{
		if(Other.Receiver.IsValid())
		{
			Bounds = Other.Receiver->GetBox();
		}
		else
		{
			Bounds = Other.Bounds;
		}
	}

	FDungeonMinimapOctreeElement& operator=(const FDungeonMinimapOctreeElement& Other)
	{
		new(this) FDungeonMinimapOctreeElement(Other);
		return *this;
	}

	const FBoxSphereBounds& GetBounds() const
	{
		return Bounds;
	}

	FORCEINLINE bool IsEmpty() const
	{
		FBox BBox = Receiver->GetBox();
		return (BBox.IsValid == 0 || BBox.GetSize().IsNearlyZero());
	}

	FBoxSphereBounds Bounds;
};

struct FDungeonMinimapOctreeSemantics
{
	typedef TOctree2<FDungeonMinimapOctreeElement, FDungeonMinimapOctreeSemantics> FOctree;
	enum { MaxElementsPerLeaf = 16 };
	enum { MinInclusiveElementsPerNode = 7 };
	enum { MaxNodeDepth = 12 };

	typedef TInlineAllocator<MaxElementsPerLeaf> ElementAllocator;

	FORCEINLINE static const FBoxSphereBounds& GetBoundingBox(const FDungeonMinimapOctreeElement& Data)
	{
		return Data.GetBounds();
	}

	FORCEINLINE static bool AreElementsEqual(const FDungeonMinimapOctreeElement& A, const FDungeonMinimapOctreeElement& B)
	{
		return A.Receiver == B.Receiver;
	}

	FORCEINLINE static void ApplyOffset(FDungeonMinimapOctreeElement& Element, const FVector& InOffset)
	{
		ensureMsgf(false, TEXT("Not implemented yet"));
	}

	static void SetElementId(FOctree& OctreeOwner, const FDungeonMinimapOctreeElement& Element, FOctreeElementId2 Id);
};

class FDungeonMinimapOCTree : public TOctree2<FDungeonMinimapOctreeElement, FDungeonMinimapOctreeSemantics>
{
public:
	FDungeonMinimapOCTree(const FVector& Origin, float Radius);
	~FDungeonMinimapOCTree();
	void SetElementIdImpl(const uint32 OwnerUniqueId, FOctreeElementId2 Id);

	FORCEINLINE uint32 HashObject(const UDungeonMinimapComponent* InfluenceReceiver) const
	{
		if (InfluenceReceiver == nullptr)
		{
			return INDEX_NONE;
		}
		return InfluenceReceiver->UniqueIndex;
	}

	FORCEINLINE const FOctreeElementId2* GetReceiverOctreeId(const UDungeonMinimapComponent* InfluenceReceiver) const
	{
		return ObjectToOctreeIds.Find(HashObject(InfluenceReceiver));
	}

	FORCEINLINE bool HasReceiverOctreeId(const UDungeonMinimapComponent* InfluenceReceiver) const
	{
		return ObjectToOctreeIds.Find(HashObject(InfluenceReceiver)) != nullptr;
	}

	FORCEINLINE void RemoveReceiverOctreeId(const UDungeonMinimapComponent* InfluenceReceiver)
	{
		ObjectToOctreeIds.Remove(InfluenceReceiver->UniqueIndex);
	}

	// update bounds by motion driver changed
	void UpdateElement(const FOctreeElementId2& Id);

private:
	TMap<uint32, FOctreeElementId2> ObjectToOctreeIds;
};

UCLASS()
class UDungeonMinimapManager : public UObject
{
	GENERATED_BODY()
public:
	void Initialize();
	void Deinitialize();
	void Clear();

	int32 AddMinimapElementCollector(UDungeonMinimapComponent* Cmp);
	bool UpdateMinimapElementCollector(int32 CmpIndex);
	void RemoveMinimapElementCollector(int32 CmpIndex);
	
	void GetCollectCmpOverlapBox(const FBoxCenterAndExtent& QueryBox, TArray<UDungeonMinimapComponent*>& OutCmps);
	void GetCollectCmpAll(TArray<UDungeonMinimapComponent*>& OutCmps);

	void RebuildAllElenments();
	
protected:
	UPROPERTY(VisibleAnywhere, BlueprintReadWrite)
	TMap<int32, UDungeonMinimapComponent*> CollectComponents;

	UPROPERTY(VisibleAnywhere, BlueprintReadWrite)
	TMap<int32, UDungeonMinimapComponent*> DynamicCollectComponents;

	TSharedPtr<FDungeonMinimapOCTree, ESPMode::ThreadSafe> StaticOCTree;

	FBox OcTreeSpace = FBox(FVector(-100000.f, -100000.f, -100000.f), FVector(100000.f, 100000.f, 100000.f));
	
	UPROPERTY(VisibleAnywhere)
	int AllocatedIndex = 0;
};

UCLASS(BlueprintType, config=Game)
class UDungeonMinimapWorldSubsystem : public UWorldSubsystem
{
	GENERATED_BODY()
public:
	UFUNCTION()
	static UDungeonMinimapWorldSubsystem* GetInstance();
	
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual void Deinitialize() override;
	
	virtual bool DoesSupportWorldType(EWorldType::Type WorldType) const override;

	virtual bool ShouldCreateSubsystem(UObject* Outer) const override;

	int32 AddMinimapElementCollector(FName MinimapName, UDungeonMinimapComponent* Cmp);
	bool UpdateMinimapElementCollector(FName MinimapName, int32 CmpIndex);
	void RemoveMinimapElementCollector(FName MinimapName, int32 CmpIndex);

	void GetCollectCmpOverlapBox(FName Minimap, const FBoxCenterAndExtent& QueryBox, TArray<UDungeonMinimapComponent*>& OutCmps);
	void GetCollectCmpAll(FName Minimap, TArray<UDungeonMinimapComponent*>& OutCmps);
	
	UDungeonMinimapManager* FindOreCreateManager(FName MinimapName);

	UFUNCTION(BlueprintCallable)
	void SetMonitorEnable(bool bEnable);

	UFUNCTION(BlueprintCallable)
	void SetMonitorFocusActor(AActor* Actor);

	FORCEINLINE bool GetMonitorEnable() const { return MonitorEnable; }
	FORCEINLINE const AActor* GetMonitorFocusActor() const { return MonitorFocusActor.Get(); }
private:
	UPROPERTY()
	TMap<FName, UDungeonMinimapManager*> MinimapManager;

	UPROPERTY()
	bool MonitorEnable = false;;

	UPROPERTY()
	TWeakObjectPtr<AActor> MonitorFocusActor;

	static UDungeonMinimapWorldSubsystem* Instance;
};
