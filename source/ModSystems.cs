using AnimationsLib.Integration;
using AnimationsLib.Integration.Transpilers;
using AnimationsLib.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;

namespace AnimationsLib;

public sealed class AnimationsLibSystem : ModSystem
{
    public Settings Settings { get; set; } = new();
    public AnimationsLoader? PlayerAnimationsManager { get; private set; }
    public ParticleEffectsManager? ParticleEffectsManager { get; private set; }
    public VanillaAnimationsSynchronizerClient? ClientVanillaAnimations { get; private set; }
    public VanillaAnimationsSynchronizerServer? ServerVanillaAnimations { get; private set; }
    public AnimationSyncrhonizerClient? ClientTpAnimationSynchronizer { get; private set; }
    public AnimationSyncrhonizerServer? ServerTpAnimationSynchronizer { get; private set; }
    public SoundsSynchronizerClient? ClientSoundsSynchronizer { get; private set; }
    public SoundsSynchronizerServer? ServerSoundsSynchronizer { get; private set; }

    public IShaderProgram? AnimatedItemShaderProgram => _shaderProgram;
    public IShaderProgram? AnimatedItemShaderProgramFirstPerson => _shaderProgramFirstPerson;

    public event Action? OnDispose;

    public override void Start(ICoreAPI api)
    {
        _api = api;

        api.RegisterEntityBehaviorClass("AnimationsLib:FirstPersonAnimations", typeof(FirstPersonAnimationsBehavior));
        api.RegisterEntityBehaviorClass("AnimationsLib:ThirdPersonAnimations", typeof(ThirdPersonAnimationsBehavior));
        api.RegisterCollectibleBehaviorClass("AnimationsLib:Animatable", typeof(Animatable));
        api.RegisterCollectibleBehaviorClass("AnimationsLib:AnimatableAttachable", typeof(AnimatableAttachable));

        HarmonyPatchesManager.Patch(api);

        ExtendedElementPose.NameHashCache = new(api, "ExtendedElementPose names");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        api.Event.ReloadShader += LoadAnimatedItemShaders;
        _ = LoadAnimatedItemShaders();
        ParticleEffectsManager = new(api);
        PlayerAnimationsManager = new(api, ParticleEffectsManager);
        ClientVanillaAnimations = new(api);
        ClientTpAnimationSynchronizer = new(api);
        ClientSoundsSynchronizer = new(api);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        ParticleEffectsManager = new(api);
        ServerVanillaAnimations = new(api);
        ServerTpAnimationSynchronizer = new(api);
        ServerSoundsSynchronizer = new(api);
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        PlayerAnimationsManager?.LoadAnimations();
    }

    public override void Dispose()
    {
        if (_api is ICoreClientAPI clientApi)
        {
            clientApi.Event.ReloadShader -= LoadAnimatedItemShaders;
        }

        ExtendedElementPose.NameHashCache?.Dispose();
        ExtendedElementPose.NameHashCache = null;

        OnDispose?.Invoke();
    }


    private ShaderProgram? _shaderProgram;
    private ShaderProgram? _shaderProgramFirstPerson;
    private ICoreAPI? _api;

    private bool LoadAnimatedItemShaders()
    {
        if (_api is not ICoreClientAPI clientApi) return false;

        _shaderProgram = clientApi.Shader.NewShaderProgram() as ShaderProgram;
        _shaderProgramFirstPerson = clientApi.Shader.NewShaderProgram() as ShaderProgram;

        if (_shaderProgram == null || _shaderProgramFirstPerson == null) return false;

        _shaderProgram.AssetDomain = Mod.Info.ModID;
        clientApi.Shader.RegisterFileShaderProgram("customstandard", AnimatedItemShaderProgram);
        _shaderProgram.Compile();

        _shaderProgramFirstPerson.AssetDomain = Mod.Info.ModID;
        clientApi.Shader.RegisterFileShaderProgram("customstandardfirstperson", AnimatedItemShaderProgramFirstPerson);
        _shaderProgramFirstPerson.Compile();

        return true;
    }
}