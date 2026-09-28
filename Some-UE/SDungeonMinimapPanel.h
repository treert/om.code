#pragma once
#include "DungeonMinimapComponent.h"
#include "Styling/SlateTypes.h"

struct FDungeonMinimapPanelElement
{
	FDungeonMinimapElement SourceElement;
	FInvalidatableBrushAttribute Image;
};

// 和 SMAYMinimapPanel 实现思路不一样。
// 1. 外部直接把转换矩阵设置过来，方便上层逻辑精确控制。
// 2. 不会在 OnPaint 里实时取 AllocSize 来计算矩阵，有上层逻辑保证UI逻辑空间大小
class SDungeonMinimapPanel : public SCompoundWidget
{
public:
	SDungeonMinimapPanel();
	
	SLATE_BEGIN_ARGS(SDungeonMinimapPanel){}
	SLATE_END_ARGS();

	void Construct(const FArguments& InArgs);

	void SetTransformMatrix(FTransform2D InMatrix);
	void SetMapScale(float InScale);
	void SetUserScale(float InScale);
	
	void ClearElements();
	void PushElementToDraw(FDungeonMinimapPanelElement&& InElementsToDraw);
	void PushElementsToDraw(TArray<FDungeonMinimapPanelElement>& InElementsToDraw);

	virtual void Tick(const FGeometry& AllottedGeometry, const double InCurrentTime, const float InDeltaTime) override;

	virtual int32 OnPaint(const FPaintArgs& Args, const FGeometry& AllottedGeometry,
		const FSlateRect& MyCullingRect, FSlateWindowElementList& OutDrawElements,
		int32 LayerId, const FWidgetStyle& InWidgetStyle, bool bParentEnabled) const override;

	void DrawElement(const FGeometry& AllottedGeometry, const FSlateRect& MyCullingRect, FSlateWindowElementList& OutDrawElements, int32 LayerId, FDungeonMinimapPanelElement& Element) const;
	
	mutable TArray<FDungeonMinimapPanelElement> ElementsToDraw;

	FTransform2D WorldToScreen;
	
	// 地图组件元素显示到UI上的基准缩放系数
	float MapScale = 0.1f;

	// 外部逻辑缩放系数。用这个参数来实现一些元素不随外部缩放而改变大小。
	float UserScale = 1.0f;
};
