using AnimationsLib.Integration;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace AnimationsLib.Integration;

internal static class HarmonyPatchesManager
{
    public static void Patch(ICoreAPI api)
    {
        _api = api;

        PatchUniversalSide(api);

        if (api is ICoreClientAPI clientApi)
        {
            PatchClientSide(clientApi);
        }
    }
    public static void Unpatch()
    {
        UnpatchUniversalSide();
        UnpatchClientSide();
        _api = null;
    }


    private const string _harmonyId = "AnimationsLib:";
    private const string _harmonyIdTranspilers = _harmonyId + "Transpilers";
    private const string _harmonyIdAnimation = _harmonyId + "Animation";

    private static ICoreAPI? _api;
    private static bool _patchedUniversalSide = false;
    private static bool _patchedClientSide = false;


    private static void PatchClientSide(ICoreClientAPI api)
    {
        if (_patchedClientSide)
        {
            return;
        }
        _patchedClientSide = true;
    }
    private static void UnpatchClientSide()
    {
        if (!_patchedClientSide)
        {
            return;
        }
        _patchedClientSide = false;
    }

    private static void PatchUniversalSide(ICoreAPI api)
    {
        if (_patchedUniversalSide)
        {
            return;
        }
        _patchedUniversalSide = true;

        new Harmony(_harmonyIdTranspilers).PatchAll();

        AnimationPatches.Patch(_harmonyIdAnimation, api);
    }
    private static void UnpatchUniversalSide()
    {
        if (!_patchedUniversalSide)
        {
            return;
        }
        _patchedUniversalSide = false;

        new Harmony(_harmonyIdTranspilers).UnpatchAll();

        if (_api != null)
        {
            AnimationPatches.Unpatch(_harmonyIdAnimation, _api);
        }
    }
}
