#include "SDungeonMinimapPanel.h"

#include "Fonts/FontMeasure.h"

SDungeonMinimapPanel::SDungeonMinimapPanel()
{
}

void SDungeonMinimapPanel::Construct(const FArguments& InArgs)
{
}

void SDungeonMinimapPanel::SetTransformMatrix(FTransform2D InMatrix)
{
	WorldToScreen = InMatrix;
}

void SDungeonMinimapPanel::SetMapScale(float InScale)
{
	constexpr float MinScale = 0.000001f;
	if (InScale < MinScale)
	{
		InScale = MinScale;
	}
	MapScale = InScale;
}

void SDungeonMinimapPanel::SetUserScale(float InScale)
{
	constexpr float MinScale = 0.000001f;
	if (InScale < MinScale)
	{
		InScale = MinScale;
	}
	UserScale = InScale;
}

void SDungeonMinimapPanel::ClearElements()
{
	ElementsToDraw.Reset();
}

void SDungeonMinimapPanel::PushElementToDraw(FDungeonMinimapPanelElement&& InElementsToDraw)
{
	ElementsToDraw.Add(InElementsToDraw);
}

void SDungeonMinimapPanel::PushElementsToDraw(TArray<FDungeonMinimapPanelElement>& InElementsToDraw)
{
	ElementsToDraw.Append(InElementsToDraw);
}

void SDungeonMinimapPanel::Tick(const FGeometry& AllottedGeometry, const double InCurrentTime, const float InDeltaTime)
{
	SCompoundWidget::Tick(AllottedGeometry, InCurrentTime, InDeltaTime);

	Invalidate(EInvalidateWidgetReason::Paint);
}

int32 SDungeonMinimapPanel::OnPaint(const FPaintArgs& Args, const FGeometry& AllottedGeometry,
                                const FSlateRect& MyCullingRect, FSlateWindowElementList& OutDrawElements, int32 LayerId,
                                const FWidgetStyle& InWidgetStyle, bool bParentEnabled) const
{
	if(ElementsToDraw.Num() > 0)
	{
		ElementsToDraw.Sort([](const FDungeonMinimapPanelElement& LHS, const FDungeonMinimapPanelElement& RHS)
		{
			if (LHS.SourceElement.DrawPriority == RHS.SourceElement.DrawPriority)
			{
				return LHS.SourceElement.SubSortIndex < RHS.SourceElement.SubSortIndex;
			}
			return LHS.SourceElement.DrawPriority < RHS.SourceElement.DrawPriority;
		});

		int32 DrawLayerId = LayerId + 1;
		for(int i = 0; i < ElementsToDraw.Num(); ++i)
		{
			DrawElement(AllottedGeometry, MyCullingRect, OutDrawElements, DrawLayerId, ElementsToDraw[i]);
			if(i+1 < ElementsToDraw.Num() && ElementsToDraw[i].SourceElement.DrawPriority != ElementsToDraw[i + 1].SourceElement.DrawPriority)
			{
				DrawLayerId+=2;
			}
		}
		//ElementsToDraw.Reset();
		
		LayerId = DrawLayerId + 1;
	}
	
	return SCompoundWidget::OnPaint(Args, AllottedGeometry, MyCullingRect, OutDrawElements, LayerId, InWidgetStyle,
	                                bParentEnabled);
}

TAutoConsoleVariable<float> OM_Var_MiniMap_Text_Scale(TEXT("om.minimap.text.scale"), 1, HELP_TEXT(""));

void SDungeonMinimapPanel::DrawElement(const FGeometry& AllottedGeometry, const FSlateRect& MyCullingRect,
	FSlateWindowElementList& OutDrawElements, int32 LayerId, FDungeonMinimapPanelElement& Element) const
{
	//@TODO::CullingRect ?
	
	FVector2D ElementLoc = WorldToScreen.TransformPoint(Element.SourceElement.WorldLoc);

	FSlateRenderTransform RootRenderTrans = AllottedGeometry.GetAccumulatedRenderTransform();
	// 外层UI的旋转
	float RootAngle = RootRenderTrans.GetMatrix().GetRotationAngle();

	const FSlateBrush* ImageBrush = Element.Image.GetImage().Get();
	if ((ImageBrush != nullptr) && (ImageBrush->DrawAs != ESlateBrushDrawType::NoDrawType))
	{
		float LastScale = MapScale;
		if (!Element.SourceElement.bAffectByScale)
		{
			LastScale = 1.0f/UserScale;// 不受 地图 和用户缩放影响
		}
		FMatrix2x2 ScaleTrans = FMatrix2x2(FScale2D(Element.SourceElement.Scale * LastScale));
		float RotateAngle;
		
		if (Element.SourceElement.bImageAffectByRotation)
		{
			RotateAngle = FMath::DegreesToRadians(Element.SourceElement.RotateDegree);
		}
		else
		{
			RotateAngle = -RootAngle;
		}
		FMatrix2x2 RotateTrans =  FMatrix2x2(FQuat2D(RotateAngle));
		FMatrix2x2 RenderTrans = ScaleTrans.Concatenate(RotateTrans);
		FPaintGeometry PlayerImageGeometry = AllottedGeometry.ToPaintGeometry(
			//ImageBrush->ImageSize * Element.Scale,
			ImageBrush->ImageSize,
			FSlateLayoutTransform(ElementLoc - ImageBrush->ImageSize * 0.5),
			FSlateRenderTransform(RenderTrans),
			FVector2D(0.5f, 0.5f));
		FSlateDrawElement::MakeBox(OutDrawElements, LayerId, PlayerImageGeometry, ImageBrush, ESlateDrawEffect::None, Element.SourceElement.Color);
	}

	if(Element.SourceElement.DrawText.Len() != 0)
	{
		FVector2D TextLoc = ElementLoc;

		// if(ImageBrush != nullptr)
		// {
		// 	TextLoc -= ImageBrush->ImageSize * 0.5;
		// }
			
		const TSharedRef< FSlateFontMeasure > FontMeasureService = FSlateApplication::Get().GetRenderer()->GetFontMeasureService();
		FVector2D TextSize = FontMeasureService->Measure(Element.SourceElement.DrawText, Element.SourceElement.Font);
		// 文本不受旋转影响

		FMatrix2x2 RotateTrans =  FMatrix2x2(FQuat2D(-RootAngle));
		
		float LastTextRenderScale = 1;
		float LastTextLayoutScale = 1;
		
		LastTextRenderScale /= UserScale;// 反转用户缩放。不然文本会变糊

		if (Element.SourceElement.bTextScaleWithUser)
		{
			LastTextLayoutScale *= UserScale;
		}

		LastTextLayoutScale *= OM_Var_MiniMap_Text_Scale.GetValueOnAnyThread();

		TextLoc -= TextSize * 0.5f * LastTextLayoutScale;

		FMatrix2x2 TextScaleTrans = FMatrix2x2(LastTextRenderScale);
		FMatrix2x2 TextRenderTrans = RotateTrans.Concatenate(TextScaleTrans);
		FPaintGeometry TextPaintGeometry = AllottedGeometry.ToPaintGeometry(
			TextSize,
			FSlateLayoutTransform(LastTextLayoutScale,TextLoc),
			FSlateRenderTransform(TextRenderTrans),
			FVector2D(0.5f, 0.5f));
		
		FSlateDrawElement::MakeText(OutDrawElements,
			LayerId+1,
			// AllottedGeometry.ToPaintGeometry(TextLoc -= TextSize * 0.5f, TextSize),
			TextPaintGeometry,
			Element.SourceElement.DrawText,
			Element.SourceElement.Font,
			ESlateDrawEffect::None,
			Element.SourceElement.TextColor);
	}

	if(Element.SourceElement.ExtraPoints.Num())
	{
		TArray<FVector2D> Points;
		Points.SetNum(Element.SourceElement.ExtraPoints.Num());
		for(int i = 0; i < Element.SourceElement.ExtraPoints.Num(); ++i)
		{
			Points[i] = WorldToScreen.TransformPoint(FVector2D(Element.SourceElement.ExtraPoints[i]));
		}
		
		FSlateDrawElement::MakeLines(OutDrawElements,
			LayerId,
			 AllottedGeometry.ToPaintGeometry(),
			 Points,
			 ESlateDrawEffect::None,
			 Element.SourceElement.PointColor,
			 true,
			 3);
	}
}
