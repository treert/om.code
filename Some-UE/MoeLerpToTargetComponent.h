// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "MoeLerpToTargetComponent.generated.h"

USTRUCT()
struct MOEGAMECORE_API FMoeLerpToTargetSyncInfo
{
	GENERATED_BODY()

	UPROPERTY()
	float StartTimestamp = -1.f;
	UPROPERTY()
	float DurationTime = 0.0f;

	UPROPERTY()
	FTransform StartTransform = FTransform::Identity;
	// 注意：目标位置是相对坐标，不支持世界坐标，使用的地方可以自己转换。
	UPROPERTY()
	FTransform TargetTransform = FTransform::Identity;

	bool IsDataValid() const
	{
		return StartTimestamp >= 0;
	}

	float GetTargetTimestamp() const
	{
		return StartTimestamp + DurationTime;
	}
};

/*
 * 实现非常简单，在目标时间把父节点插值到目标位置，确保DS位置同步。
 * 注意：直接修改父节点相对位置，如果两个组件同时工作，并不会有叠加效果。
 * PS： 处于不增加复杂性的考虑 没有继承 UMoeBaseSceneComponent
 */
UCLASS(ClassGroup=(Custom), meta=(BlueprintSpawnableComponent), BlueprintType, Blueprintable)
class MOEGAMECORE_API UMoeLerpToTargetComponent : public USceneComponent
{
	GENERATED_BODY()

	DECLARE_DYNAMIC_MULTICAST_DELEGATE(FOnStatusChanged);
public:
	// Sets default values for this component's properties
	UMoeLerpToTargetComponent();

protected:
	// Called when the game starts
	virtual void BeginPlay() override;
	virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;

public:
	// 可能在蓝图或 lua 里定制 begin play 后的逻辑
	UFUNCTION(BlueprintImplementableEvent)
	void BP_PostBeginPlay();
	
	UFUNCTION(BlueprintCallable, BlueprintPure)
	bool IsMoving() { return bIsMoving; }
	
	UFUNCTION(BlueprintCallable, BlueprintPure)
	bool IsControlMoveRootValid() const { return ControlMoveRoot.IsValid(); }

	// MoveRoot 的初始相对位置
	UFUNCTION(BlueprintCallable, BlueprintPure)
	FTransform GetInitialTransform() const { return InitialTransform; }

	UFUNCTION(BlueprintCallable, BlueprintPure)
	USceneComponent* GetControlMoveRoot()
	{
		if (ControlMoveRoot.IsValid())
		{
			return ControlMoveRoot.Get();
		}
		return nullptr;
	}

	// 将世界坐标位置转换为 ControlMoveRoot 父节点的局部坐标，方便外部调用 StartMoveTo
	UFUNCTION(BlueprintCallable, BlueprintPure)
	FVector WorldPositionToLocal(const FVector& WorldPosition) const;

	// 将世界变换转换为 ControlMoveRoot 父节点的局部变换，方便外部调用 StartLerpToTrans
	UFUNCTION(BlueprintCallable, BlueprintPure)
	FTransform WorldTransformToLocal(const FTransform& WorldTransform) const;

	// 将世界旋转转换为 ControlMoveRoot 父节点的局部旋转，方便外部调用 StartRotateTo
	UFUNCTION(BlueprintCallable, BlueprintPure)
	FRotator WorldRotationToLocal(const FRotator& WorldRotation) const;

	void LogLog(const FString& Msg, ELogVerbosity::Type InLogLevel = ELogVerbosity::Display) const;
	UFUNCTION(BlueprintCallable)
	void LogInfo(const FString& Msg) const;
	UFUNCTION(BlueprintCallable)
	void LogWarn(const FString& Msg) const;
	UFUNCTION(BlueprintCallable)
	void LogError(const FString& Msg) const;
public:
	// Called every frame
	virtual void TickComponent(float DeltaTime, ELevelTick TickType,
	                           FActorComponentTickFunction* ThisTickFunction) override;

public:
	// 默认 false 控制父节点，可以选择控制自己
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Config")
	bool bControlSelf = false;
	//起始音效
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Config")
	int SfxMoveStart = -1;
	//运动音效
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Config")
	int SfxMoving = -1;
	//停止音效
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Config")
	int SfxMoveStop = -1;

	// 调整移动的曲线，可以实现类似加速减速的效果。输入是 Percent，范围[0,1]
	UPROPERTY(EditDefaultsOnly, BlueprintReadWrite, Category="Config")
	UCurveFloat* MoveAjustCurve = nullptr;

	// 客户端与服务器的延时上限，超出这个会使用跳变的方式纠正坐标。用于应对 断线重连或者中途加入的情况
	UPROPERTY(EditDefaultsOnly, BlueprintReadWrite, Category="Config")
	float DelayTimeLimit = 0.6f;
	// 客户端是否严格按服务器时间戳到达目标点。用于调优运动效果。
	UPROPERTY(EditDefaultsOnly, BlueprintReadWrite, Category="Config")
	bool bForceTargetTimestampSync = false;
	// OnRep_LerpToTargetSyncInfo 是否检查 HasBeginPlay
	UPROPERTY(EditDefaultsOnly, BlueprintReadWrite, Category="ConfigDebug")
	bool bCheckHasBeginPlay = true;

	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;

	UFUNCTION(BlueprintCallable)
	void StartLerpToTrans(float CostTime, const FTransform& TargetTransform);
	
	UFUNCTION(BlueprintCallable, CallInEditor)
	void StartMoveTo(float CostTime, const FVector& TargetLocation);
	UFUNCTION(BlueprintCallable)
	void StartRotateTo(float CostTime, const FRotator& TargetRotation);
	UFUNCTION(BlueprintCallable)
	void StartScaleTo(float CostTime, const FVector& TargetScale);

	// 世界坐标版本：内部将世界坐标转换为局部坐标后委托给 StartLerpToTrans
	UFUNCTION(BlueprintCallable)
	void StartLerpToTransWorld(float CostTime, const FTransform& WorldTransform);
	// 世界坐标版本：内部将世界坐标转换为局部坐标后委托给 StartMoveTo
	UFUNCTION(BlueprintCallable)
	void StartMoveToWorld(float CostTime, const FVector& WorldLocation);
	// 世界坐标版本：内部将世界旋转转换为局部旋转后委托给 StartRotateTo
	UFUNCTION(BlueprintCallable)
	void StartRotateToWorld(float CostTime, const FRotator& WorldRotation);

	UFUNCTION(BlueprintCallable, CallInEditor)
	void TestStartLerpToTrans(float CostTime, FTransform TargetTransform)
	{
		StartLerpToTrans(CostTime, TargetTransform);
	}
	
	UFUNCTION(BlueprintCallable, CallInEditor)
	void TestStartMoveTo(float CostTime, const FVector& TargetLocation)
	{
		StartMoveTo(CostTime, TargetLocation);
	}
	UFUNCTION(BlueprintCallable, CallInEditor)
	void TestStartRotateTo(float CostTime, const FRotator& TargetRotation)
	{
		StartRotateTo(CostTime, TargetRotation);
	}
	UFUNCTION(BlueprintCallable, CallInEditor)
	void TestStartScaleTo(float CostTime, const FVector& TargetScale)
	{
		StartScaleTo(CostTime, TargetScale);
	}
	
	UPROPERTY(BlueprintAssignable)
	FOnStatusChanged OnStartMove;

	UFUNCTION(BlueprintImplementableEvent)
	void BP_OnStartMove();

	UPROPERTY(BlueprintAssignable)
	FOnStatusChanged OnStopMove;

	UFUNCTION(BlueprintImplementableEvent)
	void BP_OnStopMove();


	UFUNCTION(BlueprintCallable)
	void PlaySfxAudio(int SfxID);
	UFUNCTION(BlueprintCallable)
	void StopSfxAudio(int SfxID);

private:
	UPROPERTY(ReplicatedUsing = OnRep_LerpToTargetSyncInfo, Transient)
	FMoeLerpToTargetSyncInfo LerpToTargetSyncInfo;

	UFUNCTION()
	void OnRep_LerpToTargetSyncInfo();

	void InnerEnterMovingState();

	void InnerExitMovingState();
	
	bool HasAuthority() const;
	bool IsClient() const;
	bool IsServer() const;

	FMoeLerpToTargetSyncInfo MoveInfo_ForTick;
	// 只有两种状态，移动或者停止
	bool bIsMoving = false;
	// 初始化 ControlMoveRoot 时，保存的初始位置
	FTransform InitialTransform = FTransform::Identity;

	FTransform GetTransformByTime(float InTime);
	FTransform GetTransformByPercent(float Percent);

	TWeakObjectPtr<USceneComponent> ControlMoveRoot = nullptr;
};
