using AnimationsLib.Integration;
using AnimationsLib.Integration.Transpilers;
using AnimationsLib.Utils;
using OpenTK.Mathematics;
using System.Diagnostics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace AnimationsLib;

public sealed class ThirdPersonAnimationsBehavior : EntityBehavior, IDisposable
{
    public ThirdPersonAnimationsBehavior(Entity entity) : base(entity)
    {
        if (entity is not EntityPlayer player) throw new ArgumentException("Only for players");
        _player = player;
        _api = player.Api as ICoreClientAPI;
        _animationsManager = player.Api.ModLoader.GetModSystem<AnimationsLibSystem>().PlayerAnimationsManager;
        _animationSystem = player.Api.ModLoader.GetModSystem<AnimationsLibSystem>().ClientTpAnimationSynchronizer ?? throw new Exception();
        _settings = player.Api.ModLoader.GetModSystem<AnimationsLibSystem>().Settings;

        _composer = new(null, null, player);

        AnimationPatches.OnBeforeFrame += OnBeforeFrame;
        AnimationPatches.AnimationBehaviors[player.EntityId] = this;
        AnimationPatches.ActiveEntities.Add(player.EntityId);
        player.Api.ModLoader.GetModSystem<AnimationsLibSystem>().OnDispose += Dispose;

        _mainPlayer = (entity as EntityPlayer)?.PlayerUID == _api?.Settings.String["playeruid"];

        _MainHandIdleAnimationsController = new(player, request => Play(request, mainHand: true), () => Stop("main"), () => _player.RightHandItemSlot, ItemSlotType.MainHand);
        _OffHandIdleAnimationsController = new(player, request => Play(request, mainHand: false), () => Stop("mainOffhand"), () => _player.LeftHandItemSlot, ItemSlotType.OffHand);

        if (player.Api.Side == EnumAppSide.Client)
        {
            if (_existingBehaviors.TryGetValue(_player.PlayerUID, out ThirdPersonAnimationsBehavior? previousBehavior))
            {
                previousBehavior.PartialDispose();
            }

            _existingBehaviors[_player.PlayerUID] = this;
        }
    }

    public override string PropertyName() => "ThirdPersonAnimations";

    public override void OnGameTick(float deltaTime)
    {
        if (!_player.IsRendered || _player.RightHandItemSlot == null || _player.LeftHandItemSlot == null) return;

        int mainHandItemId = _player.RightHandItemSlot.Itemstack?.Item?.Id ?? 0;
        int offhandItemId = _player.LeftHandItemSlot.Itemstack?.Item?.Id ?? 0;

        if (_mainHandItemId != mainHandItemId)
        {
            _mainHandItemId = mainHandItemId;
            InHandItemChanged(mainHand: true);
        }

        if (_offHandItemId != offhandItemId)
        {
            _offHandItemId = offhandItemId;
            InHandItemChanged(mainHand: false);
        }

        _MainHandIdleAnimationsController.Update();
        _OffHandIdleAnimationsController.Update();

        if (_api != null && _ownerEntityId == 0)
        {
            _ownerEntityId = _api.World?.Player?.Entity?.EntityId ?? 0;
        }
    }
    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
        switch (despawn.Reason)
        {
            case EnumDespawnReason.Death:
                break;
            case EnumDespawnReason.Combusted:
                break;
            case EnumDespawnReason.OutOfRange:
                break;
            case EnumDespawnReason.PickedUp:
                break;
            case EnumDespawnReason.Unload:
                break;
            case EnumDespawnReason.Disconnect:
                PartialDispose();
                break;
            case EnumDespawnReason.Expire:
                break;
            case EnumDespawnReason.Removed:
                break;
        }
    }

    public PlayerItemFrame? FrameOverride { get; set; } = null;


    public void Play(AnimationRequestByCode requestByCode, bool mainHand = true)
    {
        if (_animationsManager == null) return;

        if (!_animationsManager.GetAnimation(out Animation? animation, requestByCode.Animation, _player, firstPerson: false))
        {
            LoggerUtil.Verbose(_api, this, $"Animation '{requestByCode.Animation}' was not found");
            Debug.WriteLine($"Animation '{requestByCode.Animation}' was not found");
            return;
        }

        AnimationRequest request = new(animation, requestByCode);

        PlayRequest(request, mainHand);

        if (_mainPlayer) _animationSystem.SendPlayPacket(requestByCode, mainHand, entity.EntityId, GetCurrentItemId(mainHand));
    }
    public void Play(bool mainHand, string animation, string category = "main", float animationSpeed = 1, float weight = 1, bool easeOut = true)
    {
        AnimationRequestByCode request = new(animation, animationSpeed, weight, category, TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1), easeOut, null, null);
        Play(request, mainHand);
    }
    public void PlayReadyAnimation(bool mainHand = true)
    {
        (mainHand ? _MainHandIdleAnimationsController : _OffHandIdleAnimationsController).Start();
    }
    public void Stop(string category)
    {
        _composer.Stop(category);
        if (_mainPlayer) _animationSystem.SendStopPacket(category, entity.EntityId);
    }

    public void OnFrame(Entity targetEntity, ElementPose pose, AnimatorBase animator)
    {
        _frameApplied = true;

        if (!targetEntity.IsRendered || /*DebugWindowManager.PlayAnimationsInThirdPerson ||*/ IsFirstPerson(targetEntity)) return;

        if (FrameOverride != null)
        {
            ApplyFrame(FrameOverride.Value, pose, animator);
        }
        else
        {
            ApplyFrame(_lastFrame, pose, animator);
        }
    }



    private readonly Composer _composer;
    private readonly EntityPlayer _player;
    private readonly AnimationsLoader? _animationsManager;
    private readonly AnimationSyncrhonizerClient _animationSystem;

    private readonly IdleAnimationsController _MainHandIdleAnimationsController;
    private readonly IdleAnimationsController _OffHandIdleAnimationsController;
    private PlayerItemFrame _lastFrame = PlayerItemFrame.Zero;
    private readonly List<string> _offhandCategories = new();
    private readonly List<string> _mainHandCategories = new();
    private readonly bool _mainPlayer = false;
    private readonly Settings _settings;
    private bool _frameApplied = false;
    private int _offHandItemId = 0;
    private int _mainHandItemId = 0;
    private readonly ICoreClientAPI? _api;
    private bool _disposed = false;
    private Animatable? _animatable = null;
    private float _pitch = 0;
    private Vector3 _eyePosition = new();
    private float _eyeHeight = 0;
    private long _ownerEntityId = 0;

    private static readonly Dictionary<string, ThirdPersonAnimationsBehavior> _existingBehaviors = [];

    private void OnBeforeFrame(Entity targetEntity, float dt)
    {
        if (_settings.DisableThirdPersonAnimations) return;

        if (entity.EntityId != targetEntity.EntityId || !targetEntity.IsRendered) return;

        if (!_frameApplied) return;



        _lastFrame = _composer.Compose(TimeSpan.FromSeconds(dt));



        if (_composer.AnyActiveAnimations())
        {
            _animatable = (entity as EntityAgent)?.RightHandItemSlot?.Itemstack?.Item?.GetCollectibleBehavior(typeof(Animatable), true) as Animatable;
            _pitch = targetEntity.Pos.HeadPitch;
            _eyePosition = new((float)entity.LocalEyePos.X, (float)entity.LocalEyePos.Y, (float)entity.LocalEyePos.Z);
            _eyeHeight = (float)entity.Properties.EyeHeight;
        }

        _frameApplied = false;


    }
    private void ApplyFrame(PlayerItemFrame frame, ElementPose pose, AnimatorBase animator)
    {
        EnumAnimatedElement element = EnumAnimatedElement.Unknown;

        ExtendedElementPose? extendedPoseValue = null;
        if (pose is ExtendedElementPose extendedPose)
        {
            element = extendedPose.ElementNameEnum;
            extendedPoseValue = extendedPose;
        }
        else
        {
            return;
        }

        if (element == EnumAnimatedElement.Unknown && animator is not ClientItemAnimator)
        {
            return;
        }

        if (_animatable != null && frame.DetachedAnchor)
        {
            _animatable.DetachedAnchor = true;
        }

        if (_animatable != null && frame.SwitchArms)
        {
            _animatable.SwitchArms = true;
        }

        if (element == EnumAnimatedElement.LowerTorso) return;

        if (extendedPoseValue != null)
        {
            frame.Apply(extendedPoseValue, element, _eyePosition, _eyeHeight, _pitch, _composer.AnyActiveAnimations());
        }
        else
        {
            frame.Apply(pose, element, _eyePosition, _eyeHeight, _pitch, _composer.AnyActiveAnimations());
        }
    }
    private bool IsFirstPerson(Entity entity)
    {
        bool owner = _ownerEntityId == entity.EntityId;
        if (!owner) return false;

        bool firstPerson = entity.Api is ICoreClientAPI { World.Player.CameraMode: EnumCameraMode.FirstPerson };

        return firstPerson;
    }
    private void PlayRequest(AnimationRequest request, bool mainHand = true)
    {
        if (request.Category == GetIdleAnimationCategory(mainHand))
        {
            (mainHand ? _MainHandIdleAnimationsController : _OffHandIdleAnimationsController).Pause();
        }

        _composer.Play(request);
        if (mainHand)
        {
            _mainHandCategories.Add(request.Category);
        }
        else
        {
            _offhandCategories.Add(request.Category);
        }
    }

    private void InHandItemChanged(bool mainHand)
    {
        (mainHand ? _MainHandIdleAnimationsController : _OffHandIdleAnimationsController).Stop();

        string readyCategory = GetIdleAnimationCategory(mainHand);

        foreach (string category in _mainHandCategories.Where(element => element != readyCategory))
        {
            _composer.Stop(category);
        }
        _mainHandCategories.Clear();

        (mainHand ? _MainHandIdleAnimationsController : _OffHandIdleAnimationsController).Start();
    }

    private int GetCurrentItemId(bool mainHand) => mainHand ? _player.RightHandItemSlot.Itemstack?.Item?.Id ?? 0 : _player.LeftHandItemSlot.Itemstack?.Item?.Id ?? 0;

    private string GetIdleAnimationCategory(bool mainHand) => mainHand ? "main" : "mainOffhand";

    private void PartialDispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _offhandCategories.Clear();
            _mainHandCategories.Clear();
            AnimationPatches.OnBeforeFrame -= OnBeforeFrame;
            if (AnimationPatches.AnimationBehaviors[_player.EntityId] == this)
            {
                AnimationPatches.AnimationBehaviors.Remove(_player.EntityId);
                AnimationPatches.ActiveEntities.Remove(_player.EntityId);
            }
            _existingBehaviors.Remove(_player.PlayerUID);
        }
    }
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _offhandCategories.Clear();
            _mainHandCategories.Clear();
            AnimationPatches.OnBeforeFrame -= OnBeforeFrame;
            if (AnimationPatches.AnimationBehaviors[_player.EntityId] == this)
            {
                AnimationPatches.AnimationBehaviors.Remove(_player.EntityId);
                AnimationPatches.ActiveEntities.Remove(_player.EntityId);
            }
        }
        _existingBehaviors.Clear();
    }
}