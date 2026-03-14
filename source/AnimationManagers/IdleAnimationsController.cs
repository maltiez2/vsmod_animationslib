using Vintagestory.API.Common;

namespace AnimationsLib;

public sealed class IdleAnimationsController
{
    public IdleAnimationsController(EntityPlayer player, Action<AnimationRequestByCode> playAnimationCallback, Action stopAnimationCallback, Func<ItemSlot> slotGetter, ItemSlotType slotType)
    {
        _player = player;
        _playAnimationCallback = playAnimationCallback;
        _stopAnimationCallback = stopAnimationCallback;
        _slotGetter = slotGetter;
        _slotType = slotType;
    }

    public void Start()
    {
        _currentItemId = GetItemId();
        _currentAnimation = InternalAnimationType.New;
        PlayNextAnimation();
    }

    public void Pause()
    {
        if (_currentAnimation == InternalAnimationType.None) return;

        _currentItemId = 0;
        _currentAnimation = InternalAnimationType.None;
    }

    public void Stop()
    {
        if (_currentAnimation == InternalAnimationType.None) return;

        _currentItemId = 0;
        _currentAnimation = InternalAnimationType.None;
        _stopAnimationCallback?.Invoke();
    }

    public void Update()
    {
        if (!NeedsUpdate()) return;

        PlayNextAnimation();
    }



    private enum InternalAnimationType
    {
        None = -1,
        New,
        Idle,
        Ready,
        Walk,
        Run,
        SwimIdle,
        Swim
    }
    private enum PlayerState
    {
        Idle,
        Walk,
        Run,
        SwimIdle,
        Swim
    }

    private static readonly InternalAnimationType[] _repeatedAnimations = [
        InternalAnimationType.Walk,
        InternalAnimationType.Run,
        InternalAnimationType.Swim,
        InternalAnimationType.SwimIdle,
        InternalAnimationType.Ready
    ];

    private static readonly (InternalAnimationType from, InternalAnimationType to)[] _consecutiveAnimations = [
        (InternalAnimationType.Ready, InternalAnimationType.Idle)
    ];

    private readonly EntityPlayer _player;
    private readonly Action<AnimationRequestByCode> _playAnimationCallback;
    private readonly Action _stopAnimationCallback;
    private readonly Func<ItemSlot> _slotGetter;
    private readonly ItemSlotType _slotType;

    private InternalAnimationType _currentAnimation = InternalAnimationType.None;
    private int _currentItemId = 0;

    private (AnimationRequestByCode? request, InternalAnimationType animationType) GetNextAnimation()
    {
        ItemSlot slot = _slotGetter.Invoke();
        if (!HasAnyAnimations(slot)) return (null, InternalAnimationType.None);

        InternalAnimationType nextAnimationType = GetNextExistingAnimationType(_player, slot, _slotType, _currentAnimation);
        AnimationRequestByCode? animationRequest = GetAnimation(_player, slot, _slotType, nextAnimationType);

        if (animationRequest == null) return (null, nextAnimationType);

        float animationSpeed = GetAnimationSpeed(_player, nextAnimationType);

        AnimationRequestByCode nextAnimationRequest = new(animationRequest.Value, animationSpeed, AnimationCallback);

        return (nextAnimationRequest, nextAnimationType);
    }

    private bool AnimationCallback()
    {
        if (_currentItemId != GetItemId())
        {
            _currentItemId = GetItemId();
            Stop();
            return true;
        }

        if (!_repeatedAnimations.Contains(_currentAnimation))
        {
            return true;
        }

        PlayNextAnimation();

        return true;
    }

    private void PlayNextAnimation()
    {
        (AnimationRequestByCode? request, InternalAnimationType animationType) = GetNextAnimation();
        if (request != null)
        {
            _playAnimationCallback.Invoke(request.Value);
            _currentAnimation = animationType;
        }
        else
        {
            _stopAnimationCallback?.Invoke();
            _currentAnimation = InternalAnimationType.None;
        }
    }

    private bool NeedsUpdate()
    {
        ItemSlot slot = _slotGetter.Invoke();
        if (!HasAnyAnimations(slot)) return false;

        InternalAnimationType nextAnimationType = GetNextExistingAnimationType(_player, slot, _slotType, _currentAnimation);

        if (_consecutiveAnimations.Contains((_currentAnimation, nextAnimationType))) return false;

        if (nextAnimationType == _currentAnimation) return false;

        AnimationRequestByCode? animationRequest = GetAnimation(_player, slot, _slotType, nextAnimationType);

        if (animationRequest == null) return false;

        return true;
    }

    private int GetItemId() => _slotGetter.Invoke().Itemstack?.Item?.Id ?? 0;

    private static float GetAnimationSpeed(EntityPlayer player, InternalAnimationType animationType)
    {
        return animationType switch
        {
            InternalAnimationType.Walk => GetWalkAnimationSpeed(player),
            InternalAnimationType.Run => GetWalkAnimationSpeed(player),
            InternalAnimationType.Swim => GetWalkAnimationSpeed(player),
            _ => 1
        };
    }
    private static float GetWalkAnimationSpeed(EntityPlayer player)
    {
        const double _stepPeriod = 0.95f;
        const double _defaultFrequency = 1.2f;

        EntityControls controls = player.Controls;
        double frequency = controls.MovespeedMultiplier * player.GetWalkSpeedMultiplier(0.3) * (controls.Sprint ? 0.9 : 1.2) * (controls.Sneak ? 1.2f : 1);

        return (float)(frequency / _defaultFrequency / _stepPeriod);
    }

    private static bool HasAnyAnimations(ItemSlot slot)
    {
        return !slot.Empty && slot.Itemstack?.Collectible?.GetCollectibleInterface<IHasIdleAnimations>() != null;
    }
    private static bool HasAnimation(EntityPlayer player, ItemSlot slot, ItemSlotType slotType, InternalAnimationType animationType)
    {
        return GetAnimation(player, slot, slotType, animationType) != null;
    }
    private static IdleAnimationType ConvertAnimationType(InternalAnimationType animationType)
    {
        return animationType switch
        {
            InternalAnimationType.None => IdleAnimationType.Unknown,
            InternalAnimationType.New => IdleAnimationType.Unknown,
            InternalAnimationType.Idle => IdleAnimationType.Idle,
            InternalAnimationType.Ready => IdleAnimationType.Ready,
            InternalAnimationType.Walk => IdleAnimationType.Walk,
            InternalAnimationType.Run => IdleAnimationType.Run,
            InternalAnimationType.SwimIdle => IdleAnimationType.SwimIdle,
            InternalAnimationType.Swim => IdleAnimationType.Swim,
            _ => throw new NotImplementedException(),
        };
    }
    private static AnimationRequestByCode? GetAnimation(EntityPlayer player, ItemSlot slot, ItemSlotType slotType, InternalAnimationType animationType)
    {
        IHasIdleAnimations? idleAnimationsProvider = slot.Itemstack?.Collectible.GetCollectibleInterface<IHasIdleAnimations>();

        if (idleAnimationsProvider == null)
        {
            return null;
        }

        IdleAnimationType globalAnimationType = ConvertAnimationType(animationType);
        if (globalAnimationType == IdleAnimationType.Unknown)
        {
            return null;
        }

        return idleAnimationsProvider.GetIdleAnimation(player, slot, slotType, globalAnimationType);
    }
    private static InternalAnimationType GetNextExistingAnimationType(EntityPlayer player, ItemSlot slot, ItemSlotType slotType, InternalAnimationType animationType)
    {
        InternalAnimationType result = GetNextAnimationType(player, animationType);
        while (!HasAnimation(player, slot, slotType, result) && result != InternalAnimationType.None)
        {
            result = NextAnimationTypeIfNotExists(result);
        }
        return result;
    }
    private static InternalAnimationType NextAnimationTypeIfNotExists(InternalAnimationType animationType)
    {
        return animationType switch
        {
            InternalAnimationType.None => InternalAnimationType.None,
            InternalAnimationType.New => InternalAnimationType.Ready,
            InternalAnimationType.Idle => InternalAnimationType.None,
            InternalAnimationType.Ready => InternalAnimationType.Idle,
            InternalAnimationType.Walk => InternalAnimationType.Idle,
            InternalAnimationType.Run => InternalAnimationType.Walk,
            InternalAnimationType.Swim => InternalAnimationType.Idle,
            InternalAnimationType.SwimIdle => InternalAnimationType.Swim,
            _ => InternalAnimationType.None
        };
    }
    private static InternalAnimationType GetNextAnimationType(EntityPlayer player, InternalAnimationType currentAnimation)
    {
        PlayerState playerState = GetPlayerState(player);

        return (currentAnimation, playerState) switch
        {
            (InternalAnimationType.None, _) => InternalAnimationType.None,
            (InternalAnimationType.New, _) => InternalAnimationType.Ready,
            (InternalAnimationType.Ready, PlayerState.Idle) => InternalAnimationType.Idle,
            (_, PlayerState.Idle) => InternalAnimationType.Idle,
            (_, PlayerState.Walk) => InternalAnimationType.Walk,
            (_, PlayerState.Run) => InternalAnimationType.Run,
            (_, PlayerState.Swim) => InternalAnimationType.Swim,
            (_, PlayerState.SwimIdle) => InternalAnimationType.SwimIdle,
            _ => InternalAnimationType.None
        };
    }
    private static PlayerState GetPlayerState(EntityPlayer player)
    {
        bool triesToMove = player.Controls.Forward || player.Controls.Right || player.Controls.Left;
        bool triesToRun = player.Controls.Sprint && triesToMove;
        bool swimming = player.Swimming;

        return (triesToMove, triesToRun, swimming) switch
        {
            (false, false, false) => PlayerState.Idle,
            (true, false, false) => PlayerState.Walk,
            (true, true, false) => PlayerState.Run,
            (true, _, true) => PlayerState.Swim,
            (false, _, true) => PlayerState.SwimIdle,
            _ => PlayerState.Idle,
        };
    }
}