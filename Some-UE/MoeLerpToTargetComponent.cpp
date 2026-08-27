// Fill out your copyright notice in the Description page of Project Settings.


#include "PlaceableActor/Components/Move/MoeLerpToTargetComponent.h"

#include "Core/MoeTimeLibrary.h"
#include "PlaceableActor/BaseActor/MoePlaceableActor.h"
#include "PlaceableActor/Utils/PlaceableActorLibrary.h"
#include "Sound/MoeSoundManagerUtility.h"

DEFINE_LOG_CATEGORY_STATIC(LogMoeLerpToTargetComponent, Log, All);

// Sets default values for this component's properties
UMoeLerpToTargetComponent::UMoeLerpToTargetComponent()
{
	// Set this component to be initialized when the game starts, and to be ticked every frame.  You can turn these features
	// off to improve performance if you don't need them.
	PrimaryComponentTick.bCanEverTick = true;
	PrimaryComponentTick.bStartWithTickEnabled = false;
	PrimaryComponentTick.bAllowTickOnDedicatedServer = true;
	PrimaryComponentTick.TickGroup = TG_PrePhysics;
	// ...
	// Mobility = EComponentMobility::Movable;
	SetIsReplicatedByDefault(true);
}


// Called when the game starts
void UMoeLerpToTargetComponent::BeginPlay()
{
	LogInfo(TEXT("BeginPlay"));
	Super::BeginPlay();

	// ...
	if (bControlSelf)
	{
		ControlMoveRoot = this;
	}
	else
	{
		ControlMoveRoot = GetAttachParent();
		if (!ControlMoveRoot.IsValid())
		{
			ControlMoveRoot = this;// 如果没有父节点，就用自己
			LogWarn(TEXT("GetAttachParent is null, use self."));
		}
	}
	if (ControlMoveRoot.IsValid())
	{
		ControlMoveRoot->SetMobility(EComponentMobility::Movable);
		InitialTransform = ControlMoveRoot->GetRelativeTransform();
	}
	else
	{
		LogError(TEXT("ControlMoveRoot Is Not Valid"));
	}
	BP_PostBeginPlay();

	if (ControlMoveRoot.IsValid())
	{
		// 极端情况下，ds已经同步了数据
		OnRep_LerpToTargetSyncInfo();
	}
}

void UMoeLerpToTargetComponent::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
	LogInfo(TEXT("EndPlay"));
	Super::EndPlay(EndPlayReason);
}

void UMoeLerpToTargetComponent::LogLog(const FString& Msg, ELogVerbosity::Type InLogLevel) const
{
	auto NetMode = GetNetMode();
	auto Role = GetOwnerRole();
	const FString ActorName = GetNameSafe(GetOwner());
	const FString ComponentName = GetNameSafe(this);
	const FString LogMsg = FString::Printf(TEXT("[%d-%d] [%s]-[%s] %s"),NetMode, Role, *ActorName, *ComponentName, *Msg);
	if (InLogLevel < ELogVerbosity::Warning)
	{
		MOE_LOG(LogMoeLerpToTargetComponent, Error, TEXT("%ls"), *LogMsg);
	}
	else if (InLogLevel == ELogVerbosity::Warning)
	{
		MOE_LOG(LogMoeLerpToTargetComponent, Warning, TEXT("%ls"), *LogMsg);
	}
	else
	{
		MOE_LOG(LogMoeLerpToTargetComponent, Display, TEXT("%ls"), *LogMsg);
	}
}

void UMoeLerpToTargetComponent::LogInfo(const FString& Msg) const
{
	LogLog(Msg, ELogVerbosity::Display);
}

void UMoeLerpToTargetComponent::LogWarn(const FString& Msg) const
{
	LogLog(Msg, ELogVerbosity::Warning);
}

void UMoeLerpToTargetComponent::LogError(const FString& Msg) const
{
	LogLog(Msg, ELogVerbosity::Error);
}

// Called every frame
void UMoeLerpToTargetComponent::TickComponent(float DeltaTime, ELevelTick TickType,
                                              FActorComponentTickFunction* ThisTickFunction)
{
	Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

	// ...
	if (!bIsMoving)
	{
		SetComponentTickEnabled(false);// should not happend
		LogWarn(TEXT("Tick But bIsMoving is False"));
		return;
	}
	if (!ControlMoveRoot.IsValid())
	{
		SetComponentTickEnabled(false);// should not happend
		LogError(TEXT("Tick But ControlMoveRoot Is Not Valid"));
		return;
	}
	float Percent = 1.1f;// 确保大于等于 1
	if (MoveInfo_ForTick.DurationTime > 0)
	{
		float CurrentTime = UMoeTimeLibrary::GetServerWorldTimeSecondsFast();
		float PassTime = CurrentTime - MoveInfo_ForTick.StartTimestamp;
		Percent = PassTime / MoveInfo_ForTick.DurationTime;
	}
	FTransform NewTransform = GetTransformByPercent(Percent);
	ControlMoveRoot->SetRelativeTransform(NewTransform);
	if (Percent >= 1.f)
	{
		InnerExitMovingState();
	}
}

void UMoeLerpToTargetComponent::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);
	FDoRepLifetimeParams SharedParams;
	SharedParams.bIsPushBased = true;
	DOREPLIFETIME_WITH_PARAMS_FAST(UMoeLerpToTargetComponent, LerpToTargetSyncInfo, SharedParams);
}

void UMoeLerpToTargetComponent::StartMoveTo(float CostTime, const FVector& TargetLocation)
{
	if(!HasAuthority())
	{
		LogWarn(TEXT("StartMoveTo Has No Authority"));
		return;
	}
	if (!ControlMoveRoot.IsValid())
	{
		LogWarn(TEXT("StartMoveTo Failed. ControlMoveObj is null"));
		return;
	}
	FTransform TargetTransform = ControlMoveRoot->GetRelativeTransform();
	TargetTransform.SetLocation(TargetLocation);
	StartLerpToTrans(CostTime, TargetTransform);
}

void UMoeLerpToTargetComponent::StartRotateTo(float CostTime, const FRotator& TargetRotation)
{
	if(!HasAuthority())
	{
		LogWarn(TEXT("StartRotateTo Has No Authority"));
		return;
	}
	if (!ControlMoveRoot.IsValid())
	{
		LogWarn(TEXT("StartRotateTo Failed. ControlMoveObj is null"));
		return;
	}
	FTransform TargetTransform = ControlMoveRoot->GetRelativeTransform();
	TargetTransform.SetRotation(TargetRotation.Quaternion());
	StartLerpToTrans(CostTime, TargetTransform);
}

void UMoeLerpToTargetComponent::StartScaleTo(float CostTime, const FVector& TargetScale)
{
	if(!HasAuthority())
	{
		LogWarn(TEXT("StartScaleTo Has No Authority"));
		return;
	}
	if (!ControlMoveRoot.IsValid())
	{
		LogWarn(TEXT("StartScaleTo Failed. ControlMoveObj is null"));
		return;
	}
	FTransform TargetTransform = ControlMoveRoot->GetRelativeTransform();
	TargetTransform.SetScale3D(TargetScale);
	StartLerpToTrans(CostTime, TargetTransform);
}

void UMoeLerpToTargetComponent::StartLerpToTrans(float CostTime, const FTransform& TargetTransform)
{
	if(!HasAuthority())
	{
		LogWarn(TEXT("StartLerpToTrans failed. Has No Authority"));
		return;
	}
	if (!TargetTransform.IsValid())
	{
		LogWarn(TEXT("StartLerpToTrans failed. TargetTransform Is Not Valid"));
		return;
	}
	if (!ControlMoveRoot.IsValid())
	{
		LogWarn(TEXT("StartLerpToTrans failed. ControlMoveObj is null"));
		return;
	}
	LerpToTargetSyncInfo.StartTimestamp = UMoeTimeLibrary::GetServerWorldTimeSeconds(this);
	LerpToTargetSyncInfo.DurationTime = CostTime;
	LerpToTargetSyncInfo.StartTransform = ControlMoveRoot->GetRelativeTransform();
	LerpToTargetSyncInfo.TargetTransform = TargetTransform;
	MARK_PROPERTY_DIRTY_FROM_NAME(UMoeLerpToTargetComponent, LerpToTargetSyncInfo, this);

	OnRep_LerpToTargetSyncInfo();
}

void UMoeLerpToTargetComponent::OnRep_LerpToTargetSyncInfo()
{
	if (bCheckHasBeginPlay && !HasBegunPlay())
	{
		LogInfo(TEXT("OnRep_LerpToTargetSyncInfo abort. Has Not BeginPlay. will call later in BeginPlay."));
		return;
	}
	if (!(LerpToTargetSyncInfo.IsDataValid()))
	{
		return;// 无效值
	}
	if (!LerpToTargetSyncInfo.TargetTransform.IsValid())
	{
		LogWarn(TEXT("OnRep_LerpToTargetSyncInfo bad. TargetTransform not valid."));
		return;
	}
	if (!ControlMoveRoot.IsValid())
	{
		LogWarn(FString::Printf(TEXT("OnRep_LerpToTargetSyncInfo bad. ControlMoveRoot not valid. HasBegunPlay=%d"), HasBegunPlay()));
		return;
	}
	LogInfo(FString::Printf(TEXT("OnRep_LerpToTargetSyncInfo, Start:%.3f Cost:%3.3f TargetTrans:%s StartTrans:%s"),
		LerpToTargetSyncInfo.StartTimestamp, LerpToTargetSyncInfo.DurationTime,
		*LerpToTargetSyncInfo.TargetTransform.ToString(), *LerpToTargetSyncInfo.StartTransform.ToString()));
	
	MoveInfo_ForTick = LerpToTargetSyncInfo;
	if (MoveInfo_ForTick.DurationTime < 0.00001f)
	{
		// 这种情况，直接设置到目标位置
		ControlMoveRoot->SetRelativeTransform(MoveInfo_ForTick.TargetTransform);
		InnerExitMovingState();
		return;
	}
	
	// 非DS端中途连进来或者断线重连，需要处理下位置跳变的情况。
	if (!HasAuthority())
	{
		float CurrentTime = UMoeTimeLibrary::GetServerWorldTimeSeconds(this);
		float LeftTime = MoveInfo_ForTick.GetTargetTimestamp() - CurrentTime;
		float DelayTime = CurrentTime - MoveInfo_ForTick.StartTimestamp;
		LogInfo(FString::Printf(TEXT("OnRep_LerpToTargetSyncInfo: Client CurrentTime:%.3f LeftTime:%.3f DelayTime:%.3f"), CurrentTime, LeftTime, DelayTime));
		if (LeftTime < 0.00001f)
		{
			// 已经过了运动阶段了。直接设置到目标点
			ControlMoveRoot->SetRelativeTransform(MoveInfo_ForTick.TargetTransform);
			InnerExitMovingState();
			return;
		}
		float DeltaTime = CurrentTime - MoveInfo_ForTick.StartTimestamp;
		if (FMath::Abs(DeltaTime) > DelayTimeLimit)
		{
			// 延时过大。直接纠正坐标。继续按正常tick走
			FTransform CurrectTransform = GetTransformByTime(CurrentTime);
			ControlMoveRoot->SetRelativeTransform(CurrectTransform);
		}
		else 
		{
			// 按客户端的节奏继续。
			MoveInfo_ForTick.StartTimestamp = CurrentTime;
			MoveInfo_ForTick.StartTransform = ControlMoveRoot->GetRelativeTransform();
			if (bForceTargetTimestampSync)
			{
				MoveInfo_ForTick.DurationTime = LeftTime;// 和服务器同时到达目标位置
			}
		}
	}
	
	InnerEnterMovingState();
}

void UMoeLerpToTargetComponent::InnerEnterMovingState()
{
	if (bIsMoving) return;
	LogInfo(TEXT("InnerEnterMovingState"));
	bIsMoving = true;
	SetComponentTickEnabled(true);
	PlaySfxAudio(SfxMoveStart);
	PlaySfxAudio(SfxMoving);
	BP_OnStartMove();
	OnStartMove.Broadcast();
}

void UMoeLerpToTargetComponent::InnerExitMovingState()
{
	if (!bIsMoving) return;
	LogInfo(TEXT("InnerExitMovingState"));
	bIsMoving = false;
	SetComponentTickEnabled(false);
	StopSfxAudio(SfxMoving);
	PlaySfxAudio(SfxMoveStop);
	BP_OnStopMove();
	OnStopMove.Broadcast();
}

void UMoeLerpToTargetComponent::PlaySfxAudio(int SfxID)
{
#if!UE_SERVER
	if (SfxID == -1)
	{
		return;
	}
	AActor* Actor = GetOwner();
    if (IsValid(Actor) && Actor->HasActorBegunPlay())
    {
    	AMoePlaceableActor* PlaceableActor = Cast<AMoePlaceableActor>(Actor);
    	if (PlaceableActor)
    	{
    		UPlaceableActorLibrary::PlaySfx(PlaceableActor, SfxID);
    	}
    	else
    	{
    		UMoeSoundManager* SoundManager = UMoeSoundManagerUtility::GetSoundManager(Actor);
    		if (SoundManager)
    		{
    			SoundManager->PlaySfx(SfxID, false, Actor);
    		}
    	}
    }
#endif
}

void UMoeLerpToTargetComponent::StopSfxAudio(int SfxID)
{
#if!UE_SERVER
	if (SfxID == -1)
	{
		return;
	}
	AActor* Actor = GetOwner();
	if (IsValid(Actor) && Actor->HasActorBegunPlay())
	{
		AMoePlaceableActor* PlaceableActor = Cast<AMoePlaceableActor>(Actor);
		if (PlaceableActor)
		{
			UPlaceableActorLibrary::StopSfx(PlaceableActor, SfxID);
		}
		else
		{
			UMoeSoundManager* SoundManager = UMoeSoundManagerUtility::GetSoundManager(Actor);
			if (SoundManager)
			{
				SoundManager->StopSfx(SfxID, false, Actor);
			}
		}
	}
#endif
}

bool UMoeLerpToTargetComponent::HasAuthority() const
{
	AActor* Actor = GetOwner();
	return IsValid(Actor) && Actor->HasAuthority();
}

bool UMoeLerpToTargetComponent::IsClient() const
{
	return IsNetMode(NM_DedicatedServer) == false;
}

bool UMoeLerpToTargetComponent::IsServer() const
{
	return IsNetMode(NM_Client) == false;
}

FTransform UMoeLerpToTargetComponent::GetTransformByTime(float InTime)
{
	if (MoveInfo_ForTick.DurationTime <= 0)
	{
		return MoveInfo_ForTick.TargetTransform;// just for sure
	}
	float PassTime = InTime - MoveInfo_ForTick.StartTimestamp;
	float Percent = PassTime / MoveInfo_ForTick.DurationTime;
	return GetTransformByPercent(Percent);
}

FTransform UMoeLerpToTargetComponent::GetTransformByPercent(float Percent)
{
	Percent = FMath::Clamp(Percent, 0.0f, 1.0f);
	if (IsValid(MoveAjustCurve))
	{
		Percent = MoveAjustCurve->GetFloatValue(Percent);
	}
	FVector LerpedLocation = FMath::Lerp(MoveInfo_ForTick.StartTransform.GetLocation(), MoveInfo_ForTick.TargetTransform.GetLocation(), Percent);
	FQuat LerpedRotation = FQuat::Slerp(MoveInfo_ForTick.StartTransform.GetRotation(), MoveInfo_ForTick.TargetTransform.GetRotation(), Percent);
	FVector LerpedScale = FMath::Lerp(MoveInfo_ForTick.StartTransform.GetScale3D(), MoveInfo_ForTick.TargetTransform.GetScale3D(), Percent);

	FTransform Result(LerpedRotation, LerpedLocation, LerpedScale);
	return Result;
}

FVector UMoeLerpToTargetComponent::WorldPositionToLocal(const FVector& WorldPosition) const
{
	if (!ControlMoveRoot.IsValid())
	{
		LogWarn(TEXT("WorldPositionToLocal failed. ControlMoveRoot is not valid."));
		return WorldPosition;
	}
	USceneComponent* Parent = ControlMoveRoot->GetAttachParent();
	if (Parent)
	{
		return Parent->GetComponentTransform().InverseTransformPosition(WorldPosition);
	}
	// 没有父节点时，ControlMoveRoot 的 RelativeTransform 就是相对于世界的，直接返回世界坐标
	return WorldPosition;
}

FTransform UMoeLerpToTargetComponent::WorldTransformToLocal(const FTransform& WorldTransform) const
{
	if (!ControlMoveRoot.IsValid())
	{
		LogWarn(TEXT("WorldTransformToLocal failed. ControlMoveRoot is not valid."));
		return WorldTransform;
	}
	USceneComponent* Parent = ControlMoveRoot->GetAttachParent();
	if (Parent)
	{
		return WorldTransform.GetRelativeTransform(Parent->GetComponentTransform());
	}
	// 没有父节点时，ControlMoveRoot 的 RelativeTransform 就是相对于世界的，直接返回世界变换
	return WorldTransform;
}

FRotator UMoeLerpToTargetComponent::WorldRotationToLocal(const FRotator& WorldRotation) const
{
	if (!ControlMoveRoot.IsValid())
	{
		LogWarn(TEXT("WorldRotationToLocal failed. ControlMoveRoot is not valid."));
		return WorldRotation;
	}
	USceneComponent* Parent = ControlMoveRoot->GetAttachParent();
	if (Parent)
	{
		return Parent->GetComponentTransform().InverseTransformRotation(WorldRotation.Quaternion()).Rotator();
	}
	// 没有父节点时，ControlMoveRoot 的 RelativeTransform 就是相对于世界的，直接返回世界旋转
	return WorldRotation;
}

void UMoeLerpToTargetComponent::StartMoveToWorld(float CostTime, const FVector& WorldLocation)
{
	FVector LocalLocation = WorldPositionToLocal(WorldLocation);
	StartMoveTo(CostTime, LocalLocation);
}

void UMoeLerpToTargetComponent::StartRotateToWorld(float CostTime, const FRotator& WorldRotation)
{
	FRotator LocalRotation = WorldRotationToLocal(WorldRotation);
	StartRotateTo(CostTime, LocalRotation);
}

void UMoeLerpToTargetComponent::StartLerpToTransWorld(float CostTime, const FTransform& WorldTransform)
{
	FTransform LocalTransform = WorldTransformToLocal(WorldTransform);
	StartLerpToTrans(CostTime, LocalTransform);
}

