#pragma once

#include "CoreMinimal.h"
#include "DungeonMinimapComponent.generated.h"

USTRUCT(BlueprintType, Blueprintable)
struct FDungeonMinimapElement
{
	GENERATED_BODY()

	// 和 DrawPriority 配合实现稳定排序，避免UI闪烁
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly)
	int32 SubSortIndex = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 DrawPriority = 1;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FName StampKey;
	
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D WorldLoc = FVector2D::ZeroVector;

	// 替换原来的 ForwardVec，用于控制 UI显示的旋转角度，180度单位制
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float RotateDegree = 0.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D Scale = FVector2D(1.0f, 1.0f);

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FLinearColor Color = FLinearColor::White;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FVector2D> ExtraPoints;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FLinearColor PointColor = FLinearColor::White;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString DrawText;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FSlateFontInfo Font;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FLinearColor TextColor = FLinearColor::White;

	// 图片部分是否受到地图缩放和玩家缩放影响
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bAffectByScale = true;

	// 图片是否受到旋转影响。
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bImageAffectByRotation = true;

	// 文本是否跟随地图缩放。【不要使用，效果很差,代码先保留】
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bTextScaleWithMap = false;

	// 文本是否跟随用户缩放
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bTextScaleWithUser = false;
};

UCLASS(Blueprintable, BlueprintType)
class UDungeonMinimapComponent : public UStaticMeshComponent
{
	GENERATED_BODY()
public:
	
	UFUNCTION(BlueprintCallable)
	TArray<FDungeonMinimapElement> CollectMinimapDrawElement();

	UFUNCTION(BlueprintNativeEvent)
	TArray<FDungeonMinimapElement> CollectMinimapDrawElementImp();

	UFUNCTION(BlueprintNativeEvent)
	FName GetMinimapName();

	virtual void OnComponentDestroyed(bool bDestroyingHierarchy) override;

	UPROPERTY(VisibleAnywhere)
	TArray<FDungeonMinimapElement> CachedElements;

	UFUNCTION(BlueprintCallable)
	void RegisterSelf();

	UFUNCTION(BlueprintCallable)
	void UnregisterSelf();

	UFUNCTION(BlueprintCallable)
	void ReupdateSelfBound();

	UFUNCTION(BlueprintNativeEvent)
	void GetAABB(FVector& Min, FVector& Max);

	UFUNCTION(BlueprintCallable)
	FBox GetBox();

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bEnable = true;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bStaticElement = true;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bNeedRecollect = true;

	UPROPERTY(VisibleAnywhere)
	int32 UniqueIndex = INDEX_NONE;

	UPROPERTY()
	FBox CachedBox;
};
