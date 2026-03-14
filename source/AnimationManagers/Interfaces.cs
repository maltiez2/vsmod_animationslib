using AnimationsLib;
using Vintagestory.API.Common;

namespace AnimationsLib;

public enum ItemSlotType
{
    MainHand,
    OffHand
}

public enum IdleAnimationType
{
    Unknown = 0,
    Idle,
    Ready,
    Walk,
    Run,
    Swim,
    SwimIdle
}

public interface IHasIdleAnimations
{
    AnimationRequestByCode? GetIdleAnimation(EntityPlayer player, ItemSlot slot, ItemSlotType slotType, IdleAnimationType animationType);
}
