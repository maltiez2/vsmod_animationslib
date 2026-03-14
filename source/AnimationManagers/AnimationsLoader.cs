using AnimationsLib.Utils;
using Newtonsoft.Json.Linq;
using System.Diagnostics.CodeAnalysis;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AnimationsLib;

public sealed class AnimationsLoader
{
    public Dictionary<string, Animation> Animations { get; private set; } = [];

    public AnimationsLoader(ICoreClientAPI api, ParticleEffectsManager particleEffectsManager)
    {
        _api = api;
    }
    
    public void LoadAnimations()
    {
        List<IAsset> animations = _api.Assets.GetManyInCategory("config", "animations");

        Dictionary<string, Animation> animationsByCode = [];
        foreach (Dictionary<string, Animation> assetAnimations in animations.Select(FromAsset))
        {
            foreach ((string code, Animation animation) in assetAnimations)
            {
                animationsByCode.Add(code, animation);
            }
        }

        Animations = animationsByCode;
    }

    public Animation? GetAnimation(string code, params string[] tags)
    {
        return GetAnimationRecursive(code, tags);
    }

    public Animation? GetAnimation(string code, EntityPlayer player, bool firstPerson = true)
    {
        return GetAnimationRecursive(code, GetTags(player, firstPerson));
    }

    public bool GetAnimation([NotNullWhen(true)] out Animation? animation, string code, EntityPlayer player, bool firstPerson = true)
    {
        animation = GetAnimationRecursive(code, GetTags(player, firstPerson));
        return animation != null;
    }

    public static List<string> GetTags(EntityPlayer player, bool firstPerson = true)
    {
        List<string> tags = [];

        if (!firstPerson)
        {
            tags.Add("tp");
        }

        float intoxication = player.WatchedAttributes.GetFloat("intoxication");

        if (intoxication > 0.1f)
        {
            tags.Add("drunk");
        }

        string modelPrefix = player.WatchedAttributes.GetString("skinModel", "").Replace(':', '-');
        if (modelPrefix != "")
        {
            tags.Add(modelPrefix);
        }

        return tags;
    }



    private readonly ICoreClientAPI _api;

    private Animation? GetAnimationRecursive(string code, IEnumerable<string> tags)
    {
        foreach (string tag in tags)
        {
            string newCode = code + "-" + tag;

            Animation? result = GetAnimationRecursive(newCode, tags.Except([tag]));

            if (result != null) return result;
        }

        foreach (string tag in tags)
        {
            Animation? result = GetAnimationRecursive(code, tags.Except([tag]));

            if (result != null) return result;
        }

        if (Animations.TryGetValue(code, out Animation finalResult))
        {
            return finalResult;
        }

        return null;
    }

    private Dictionary<string, Animation> FromAsset(IAsset asset)
    {
        Dictionary<string, Animation> result = [];

        string domain = asset.Location.Domain;

        JsonObject json;

        try
        {
            json = JsonObject.FromJson(asset.ToText());
        }
        catch (Exception exception)
        {
            LoggerUtil.Error(_api, this, $"Error on parsing animations file '{asset.Location}'.\nException: {exception}");
            return result;
        }

        foreach (KeyValuePair<string, JToken?> entry in json.Token as JObject)
        {
            string code = entry.Key;

            try
            {
                JsonObject animationJson = new(entry.Value);

                Animation animation = animationJson.AsObject<AnimationJson>().ToAnimation();

                string animationCode = code.Contains(':') ? code : $"{domain}:{code}";

                result.TryAdd(animationCode, animation);
            }
            catch (Exception exception)
            {
                LoggerUtil.Error(_api, this, $"Error on parsing animation '{code}' from '{asset.Location}'.\nException: {exception}");
            }
        }

        return result;
    }
}