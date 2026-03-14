using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Server;

namespace AnimationsLib;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public sealed class VanillaAnimationStartPacket
{
    public string Code { get; set; } = "";
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public sealed class VanillaAnimationStopPacket
{
    public string Code { get; set; } = "";
}

public sealed class VanillaAnimationsSynchronizerClient
{
    public VanillaAnimationsSynchronizerClient(ICoreClientAPI api)
    {
        _api = api;
        _channel = api.Network.RegisterChannel("AnimationsLib:vanilla-animations")
            .RegisterMessageType<VanillaAnimationStartPacket>()
            .RegisterMessageType<VanillaAnimationStopPacket>();
    }

    public void StartAnimation(string code)
    {
        _api.World.Player.Entity.StartAnimation(code);
        _channel.SendPacket(new VanillaAnimationStartPacket {  Code = code } );
    }

    public void StopAnimation(string code)
    {
        _api.World.Player.Entity.StopAnimation(code);
        _channel.SendPacket(new VanillaAnimationStopPacket { Code = code });
    }

    private readonly ICoreClientAPI _api;
    private readonly IClientNetworkChannel _channel;
}

public sealed class VanillaAnimationsSynchronizerServer
{
    public VanillaAnimationsSynchronizerServer(ICoreServerAPI api)
    {
        api.Network.RegisterChannel("AnimationsLib:vanilla-animations")
            .RegisterMessageType<VanillaAnimationStartPacket>()
            .RegisterMessageType<VanillaAnimationStopPacket>()
            .SetMessageHandler<VanillaAnimationStartPacket>(StartAnimation)
            .SetMessageHandler<VanillaAnimationStopPacket>(StopAnimation);
    }

    private void StartAnimation(IServerPlayer player, VanillaAnimationStartPacket packet)
    {
        player.Entity.StartAnimation(packet.Code);
    }

    private void StopAnimation(IServerPlayer player, VanillaAnimationStopPacket packet)
    {
        player.Entity.StopAnimation(packet.Code);
    }
}
