#pragma once
#include "DungeonMinimapComponent.h"
#include "Blueprint/UserWidget.h"
#include "Components/Widget.h"

#include "DungeonMinimapPanel.generated.h"

class SDungeonMinimapPanel;

/*
 * 目前是 ChestPVE 专用，位置计算逻辑 UMAYMinimapPanel 不同。
 */
UCLASS()
class UDungeonMinimapPanel : public UWidget
{
	GENERATED_UCLASS_BODY()
public:
	
#if WITH_EDITOR
	//~ Begin UWidget Interface
	virtual const FText GetPaletteCategory() override { return FText::FromString(TEXT("ChestPVE")); }
	//~ End UWidget Interface
#endif

	UFUNCTION(BlueprintCallable)
	void SetQueryBox(const FBox& InQueryBox);
	
	// 设置坐标转换信息
	UFUNCTION(BLueprintCallable)
	void SetTransformInfo(float MapOriginX, float MapOriginY, float UIOriginX, float UIOriginY, float InMapScale);
	
	UFUNCTION(BlueprintCallable)
	void SetMapScale(float Value);
	
	UFUNCTION(BlueprintCallable)
	void SetUserScale(float Value);

	UFUNCTION(BlueprintCallable)
	void NativeTick();

	// 一般是测试用
	UFUNCTION(BlueprintCallable)
	void PushNewCollectorElement(FDungeonMinimapElement CollectElement);
	
	UFUNCTION(BlueprintCallable)
	FVector2D ComputeScreenLocation(FVector2D WorldLocation);
	
	UFUNCTION(BlueprintCallable)
	FVector2D ComputeWorldLocation(FVector2D ScreenLocation);
protected:
	//~ Begin UWidget Interface
	virtual TSharedRef<SWidget> RebuildWidget() override;
	virtual void SynchronizeProperties() override;
	//~ End UWidget Interface
	
	//~ Begin UVisual Interface
	virtual void ReleaseSlateResources(bool bReleaseChildren) override;
	//~ End UVisual Interface
	
	UPROPERTY(EditAnywhere)
	FVector2D MapOriginPos;
	UPROPERTY(EditAnywhere)
	FVector2D UIOriginPos;
	// 地图组件元素显示到UI上的基准缩放系数
	UPROPERTY(EditAnywhere, meta=(ClampMin = 0.0001f))
	float MapScale = 0.1f;

	// map to ui 的坐标转换矩阵
	FTransform2D WorldToScreen;
	FTransform2D ScreenToWorld;

	void InnerSetTransformInfo();

	// 过滤框，在这个框里的地图元素才显示。
	UPROPERTY(EditAnywhere)
	FBox QueryBox;
	
	// 外部逻辑缩放系数。用这个参数来实现一些元素不随外部缩放而改变大小。按现在的逻辑只会大于 1
	UPROPERTY(EditAnywhere, meta=(ClampMin = 0.00001f))
	float UserScale = 1.0f;

	UPROPERTY(EditAnywhere)
	bool bDrawQueryBox = false;

	UPROPERTY(EditAnywhere)
	bool bQueryAllElement = false;

	// UPROPERTY(EditAnywhere)
	// FName DefaultQueryMinimapName;

	UPROPERTY(EditAnywhere)
	TMap<FName, bool> ExtraQueryMinimapNames;

	UPROPERTY(VisibleAnywhere)
	TArray<FDungeonMinimapElement> CustomCollectElements;

#if WITH_EDITOR
	virtual void PostEditChangeProperty(FPropertyChangedEvent& PropertyChangedEvent) override;
#endif
	
#if WITH_ACCESSIBILITY
	virtual TSharedPtr<SWidget> GetAccessibleWidget() const override;
#endif

	TSharedPtr<SDungeonMinimapPanel> MinimapPanel;

	// 图片元素绘制使用的图片资源配置
	UPROPERTY(EditAnywhere, Category="MinimapStamp")
	TMap<FName, FSlateBrush> MinimapStampMap;
	
};
