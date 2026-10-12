
#include "DungeonMinimapPanel.h"
#include "DungeonMinimapWorldSubsystem.h"
#include "SDungeonMinimapPanel.h"
#include "DungeonMinimapComponent.h"

UDungeonMinimapPanel::UDungeonMinimapPanel(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{
	bIsVolatile = true;// 这个组件动态绘制小地图底图，不能缓存
}

void UDungeonMinimapPanel::SetQueryBox(const FBox& InQueryBox)
{
	QueryBox = InQueryBox;
}

void UDungeonMinimapPanel::SetTransformInfo(float MapOriginX, float MapOriginY, float UIOriginX, float UIOriginY,
	float InMapScale)
{
	MapOriginPos = FVector2D(MapOriginX, MapOriginY);
	UIOriginPos = FVector2D(UIOriginX, UIOriginY);
	MapScale = InMapScale;

	FTransform2D T = FTransform2D(1.0, -1 * MapOriginPos);
	FTransform2D V = FTransform2D(MapScale, UIOriginPos);

	WorldToScreen = T.Concatenate(V);
	ScreenToWorld = WorldToScreen.Inverse();

	if (MinimapPanel.IsValid())
	{
		MinimapPanel->SetMapScale(MapScale);
		MinimapPanel->SetTransformMatrix(WorldToScreen);
	}
}

void UDungeonMinimapPanel::SetMapScale(float Value)
{
	MapScale = Value;
	if(MinimapPanel.IsValid())
	{
		MinimapPanel->SetMapScale(Value);
	}
}

void UDungeonMinimapPanel::SetUserScale(float Value)
{
	UserScale = Value;
	if(MinimapPanel.IsValid())
	{
		MinimapPanel->SetUserScale(Value);
	}
}

TSharedRef<SWidget> UDungeonMinimapPanel::RebuildWidget()
{
	MinimapPanel = SNew(SDungeonMinimapPanel);
	MinimapPanel->SetUserScale(UserScale);
	InnerSetTransformInfo();
	return MinimapPanel.ToSharedRef();
}

void UDungeonMinimapPanel::SynchronizeProperties()
{
	Super::SynchronizeProperties();
}

void UDungeonMinimapPanel::ReleaseSlateResources(bool bReleaseChildren)
{
	Super::ReleaseSlateResources(bReleaseChildren);
	
	MinimapPanel.Reset();
}

void UDungeonMinimapPanel::InnerSetTransformInfo()
{
	SetTransformInfo(MapOriginPos.X, MapOriginPos.Y, UIOriginPos.X, UIOriginPos.Y, MapScale);
}

void UDungeonMinimapPanel::NativeTick()
{
	//Super::NativeTick(MyGeometry, InDeltaTime);

	if(!MinimapPanel.IsValid())
	{
		return;
	}
	
	UWorld* World = GetWorld();
	if(UDungeonMinimapWorldSubsystem* MinimapSys = World->GetSubsystem<UDungeonMinimapWorldSubsystem>())
	{
		MinimapPanel->ClearElements();
		
		// Draw query box
		if(bDrawQueryBox && QueryBox.IsValid)
		{
			FDungeonMinimapPanelElement RenderElement;
			RenderElement.SourceElement.DrawPriority = 100;
			// RenderElement.Image = TAttribute<const FSlateBrush*>(nullptr);
			// 其实这行没有意义，划线时不关心 WorldLoc
			RenderElement.SourceElement.WorldLoc = FVector2D(QueryBox.GetCenter());

			TArray<FVector2D> ExtraPoints;
			FVector2D Min = FVector2D(QueryBox.Min);
			FVector2D Max = FVector2D(QueryBox.Max);
			ExtraPoints.Add(Min);
			ExtraPoints.Add(FVector2D(Min.X, Max.Y));
			ExtraPoints.Add(Max);
			ExtraPoints.Add(FVector2D(Max.X, Min.Y));
			ExtraPoints.Add(Min);
			RenderElement.SourceElement.ExtraPoints = MoveTemp(ExtraPoints);
			RenderElement.SourceElement.PointColor = FLinearColor::Yellow;
			MinimapPanel->PushElementToDraw(MoveTemp(RenderElement));
			
			// FDungeonMinimapPanelElement RenderElement;
			// RenderElement.SourceElement.DrawPriority = 100;
			// RenderElement.Image = TAttribute<const FSlateBrush*>(nullptr);
			// RenderElement.SourceElement.WorldLoc = FVector2D(QueryCenter);
			// RenderElement.SourceElement.ForwardVec = FVector2D(0.0f, 0.0f);
			// RenderElement.SourceElement.Scale = FVector2D(1.0f, 1.0f);
			//
			// float Scale = 1.0f / MinimapPanel->GetScale();
			// FVector ScaledQueryExtend = QueryExtend;
			// ScaledQueryExtend.X *= Scale;
			// ScaledQueryExtend.Y *= Scale;
			//
			// TArray<FVector2D> ExtraPoints;
			// FVector2D Min = FVector2D(QueryCenter - ScaledQueryExtend);
			// FVector2D Max = FVector2D(QueryCenter + ScaledQueryExtend);
			// ExtraPoints.Add(Min);
			// ExtraPoints.Add(FVector2D(Min.X, Max.Y));
			// ExtraPoints.Add(Max);
			// ExtraPoints.Add(FVector2D(Max.X, Min.Y));
			// ExtraPoints.Add(Min);
			// RenderElement.SourceElement.ExtraPoints = MoveTemp(ExtraPoints);
			// RenderElement.SourceElement.PointColor = FLinearColor::Yellow;
			// MinimapPanel->PushElementToDraw(MoveTemp(RenderElement));
		}
		
		TArray<UDungeonMinimapComponent*> Cmps;

		if(bQueryAllElement)
		{
			for (auto& ExtraQueryMinimapName : ExtraQueryMinimapNames)
			{
				if(ExtraQueryMinimapName.Value)
				{
					MinimapSys->GetCollectCmpAll(ExtraQueryMinimapName.Key,  Cmps);
				}
			}
		}
		else
		{
			FBoxCenterAndExtent QueryBoxExt = FBoxCenterAndExtent(QueryBox);
			for (auto& ExtraQueryMinimapName : ExtraQueryMinimapNames)
			{
				if(ExtraQueryMinimapName.Value)
				{
					MinimapSys->GetCollectCmpOverlapBox(ExtraQueryMinimapName.Key, QueryBoxExt, Cmps);
				}
			}
			
			// float Scale = 1.0f / MinimapPanel->GetScale();
			// FVector ScaledQueryExtend = QueryExtend;
			// ScaledQueryExtend.X *= Scale;
			// ScaledQueryExtend.Y *= Scale;
			//
			// for (auto& ExtraQueryMinimapName : ExtraQueryMinimapNames)
			// {
			// 	if(ExtraQueryMinimapName.Value)
			// 	{
			// 		MinimapSys->GetCollectCmpOverlapBox(ExtraQueryMinimapName.Key, FBoxCenterAndExtent(QueryCenter, ScaledQueryExtend), Cmps);
			// 	}
			// }
		}
		
		for (UDungeonMinimapComponent* Cmp : Cmps)
		{
			if(!IsValid(Cmp) || !Cmp->bEnable)
			{
				continue;
			}
		
			TArray<FDungeonMinimapElement> CollectElements = Cmp->CollectMinimapDrawElement();
			for (FDungeonMinimapElement& CollectElement : CollectElements)
			{
				FDungeonMinimapPanelElement RenderElement;
				if(FSlateBrush* StampImgPtr = MinimapStampMap.Find(CollectElement.StampKey))
				{
					RenderElement.Image = TAttribute<const FSlateBrush*>(StampImgPtr);
				}
				RenderElement.SourceElement = CollectElement;
				MinimapPanel->PushElementToDraw(MoveTemp(RenderElement));
			}
		}

		for (const FDungeonMinimapElement& CollectElement : CustomCollectElements)
		{
			FDungeonMinimapPanelElement RenderElement;
			if(FSlateBrush* StampImgPtr = MinimapStampMap.Find(CollectElement.StampKey))
			{
				RenderElement.Image = TAttribute<const FSlateBrush*>(StampImgPtr);
			}
			RenderElement.SourceElement = CollectElement;
			MinimapPanel->PushElementToDraw(MoveTemp(RenderElement));
		}
		CustomCollectElements.Reset();
	}
}

void UDungeonMinimapPanel::PushNewCollectorElement(FDungeonMinimapElement CollectElement)
{
	CustomCollectElements.Add(MoveTemp(CollectElement));
}

FVector2D UDungeonMinimapPanel::ComputeScreenLocation(FVector2D WorldLocation)
{
	return WorldToScreen.TransformPoint(WorldLocation);
}

FVector2D UDungeonMinimapPanel::ComputeWorldLocation(FVector2D ScreenLocation)
{
	return ScreenToWorld.TransformPoint(ScreenLocation);
}

#if WITH_EDITOR
void UDungeonMinimapPanel::PostEditChangeProperty(FPropertyChangedEvent& PropertyChangedEvent)
{
	Super::PostEditChangeProperty(PropertyChangedEvent);

	// 获取被修改的属性名称
	FName PropertyName = (PropertyChangedEvent.Property != nullptr) ? PropertyChangedEvent.Property->GetFName() : NAME_None;
	if (PropertyName == GET_MEMBER_NAME_CHECKED(ThisClass, MapScale)
		|| PropertyName == GET_MEMBER_NAME_CHECKED(ThisClass, MapOriginPos)
		|| PropertyName == GET_MEMBER_NAME_CHECKED(ThisClass, UIOriginPos))
	{
		InnerSetTransformInfo();
	}
	else if (PropertyName == GET_MEMBER_NAME_CHECKED(ThisClass, UserScale))
	{
		SetUserScale(UserScale);
	}
}
#endif

#if WITH_ACCESSIBILITY
TSharedPtr<SWidget> UDungeonMinimapPanel::GetAccessibleWidget() const
{
	return MinimapPanel;
}
#endif
