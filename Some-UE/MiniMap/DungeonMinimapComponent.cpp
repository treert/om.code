
#include "DungeonMinimapComponent.h"
#include "DungeonMinimapWorldSubsystem.h"

TArray<FDungeonMinimapElement> UDungeonMinimapComponent::CollectMinimapDrawElementImp_Implementation()
{
	return {};
}

TArray<FDungeonMinimapElement> UDungeonMinimapComponent::CollectMinimapDrawElement()
{
	if(bNeedRecollect || !bStaticElement)
	{
		bNeedRecollect = false;
		CachedElements = CollectMinimapDrawElementImp();
		for (int i = 0; i < CachedElements.Num(); i++)
		{
			FDungeonMinimapElement& CollectElement = CachedElements[i];
			CollectElement.SubSortIndex = UniqueIndex;
		}
	}

	return CachedElements;
}

FName UDungeonMinimapComponent::GetMinimapName_Implementation()
{
	return NAME_None;
}

void UDungeonMinimapComponent::OnComponentDestroyed(bool bDestroyingHierarchy)
{
	Super::OnComponentDestroyed(bDestroyingHierarchy);

	UnregisterSelf();
}

void UDungeonMinimapComponent::RegisterSelf()
{
	if(UWorld* World = GetWorld())
	{
		if (World->IsEditorWorld() || World->IsGameWorld())
		{
			if (UDungeonMinimapWorldSubsystem* MinimapSys = World->GetSubsystem<UDungeonMinimapWorldSubsystem>())
			{
				FName MinimapName = GetMinimapName();
				//UniqueIndex = MinimapSys->AddMinimapElementCollector(MinimapName, this);
				// We need set UniqueIndex immediately for octree
				MinimapSys->AddMinimapElementCollector(MinimapName, this);
			}
		}
	}
}

void UDungeonMinimapComponent::UnregisterSelf()
{
	if(UWorld* World = GetWorld())
	{
		if (World->IsEditorWorld() || World->IsGameWorld())
		{
			if (UDungeonMinimapWorldSubsystem* MinimapSys = World->GetSubsystem<UDungeonMinimapWorldSubsystem>())
			{
				FName MinimapName = GetMinimapName();
				MinimapSys->RemoveMinimapElementCollector(MinimapName, UniqueIndex);
			}
		}
	}
}

void UDungeonMinimapComponent::ReupdateSelfBound()
{
	if(UWorld* World = GetWorld())
	{
		if (World->IsEditorWorld() || World->IsGameWorld())
		{
			if (UDungeonMinimapWorldSubsystem* MinimapSys = World->GetSubsystem<UDungeonMinimapWorldSubsystem>())
			{
				FName MinimapName = GetMinimapName();
				MinimapSys->UpdateMinimapElementCollector(MinimapName, UniqueIndex);
			}
		}
	}
}

void UDungeonMinimapComponent::GetAABB_Implementation(FVector& Min, FVector& Max)
{
}

FBox UDungeonMinimapComponent::GetBox()
{
	CachedBox = Bounds.GetBox();
	return CachedBox;
}
