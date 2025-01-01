using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared.Nodes;

namespace EffectZones;

public class EffectZones : BaseSettingsPlugin<EffectZonesSettings>
{
    private readonly ConditionalWeakTable<string, Func<string, bool>> _pathMatchers = [];

    private IngameUIElements _ingameUi;

    private readonly ConditionalWeakTable<Entity, Stopwatch> _rejectedCache = [];

    public override bool Initialise()
    {
        Settings.RemoveMatchedUnknownEffects.OnPressed += () =>
        {
            var contentToRemove = new HashSet<string>();
            foreach (var node in Settings.UnknownEffects.Content)
            {
                if (Settings.BlacklistTemplates.Content.Any(x => IsMatch(x.Value, node.Value)) ||
                    Settings.EntityGroups.Content.Any(g => g.PathTemplates.Content.Any(p => IsMatch(p.Value, node.Value))))
                {
                    contentToRemove.Add(node.Value);
                }
            }

            if (contentToRemove.Any())
            {
                Settings.UnknownEffects.Content.RemoveAll(x => contentToRemove.Contains(x.Value));
            }
        };
        return true;
    }

    public override void Tick()
    {
        _ingameUi = GameController.Game.IngameState.IngameUi;
    }

    private bool IsMatch(string template, string path)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return false;
        }

        return _pathMatchers.GetValue(template, t =>
        {
            var regexes = t.Split('&').Select(x => x.StartsWith('!')
                    ? (false, x[1..])
                    : (true, x))
                .Select(p => (p.Item1, new Regex(p.Item2, RegexOptions.IgnoreCase)))
                .ToList();
            return s => regexes.All(x => x.Item2.IsMatch(s) == x.Item1);
        })!(path);
    }

    public override void Render()
    {
        if (!Settings.IgnoreFullscreenPanels &&
            _ingameUi.FullscreenPanels.Any(x => x.IsVisible) ||
            !Settings.IgnoreLargePanels &&
            _ingameUi.LargePanels.Any(x => x.IsVisible))
            return;

        var entityLists = new List<IEnumerable<Entity>>
        {
            GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Effect] ?? [],
            GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Monster]?.Where(x => 
                x?.Metadata?.Contains("MonsterMods", StringComparison.OrdinalIgnoreCase) == true) ?? [],
        };

        var entityList = entityLists.SelectMany(list => list).ToList();

        foreach (var entity in entityList)
        {
            if (entity == null) continue;
            if (_rejectedCache.TryGetValue(entity, out var stopwatch))
            {
                if (stopwatch.Elapsed > TimeSpan.FromSeconds(1))
                {
                    _rejectedCache.Remove(entity);
                }
                else
                {
                    continue;
                }
            }

            if (!entity.TryGetComponent<Animated>(out var animated) || animated.BaseAnimatedObjectEntity is not { } baseEntity)
            {
                _rejectedCache.AddOrUpdate(entity, Stopwatch.StartNew());
                continue;
            }

            if (entity.DistancePlayer >= Settings.EntityLookupRange)
            {
                continue;
            }

            if (Settings.BlacklistTemplates.Content.Any(x => IsMatch(x.Value, baseEntity.Path)))
            {
                _rejectedCache.AddOrUpdate(entity, Stopwatch.StartNew());
                continue;
            }

            var matchingGroup = Settings.EntityGroups.Content.FirstOrDefault(g => g.PathTemplates.Content.Any(p => IsMatch(p.Value, baseEntity.Path)));
            if (matchingGroup != null)
            {
                float? baseRadius =
                    matchingGroup.BaseSizeOverride.Value is > 0 and { } baseOverride
                        ? baseOverride
                        : entity.TryGetComponent<GroundEffect>(out var effect) && effect.EffectDescription?.BaseSize is { } groundEffectSize
                            ? groundEffectSize
                            : animated.MiscAnimated?.BaseSize;
                if (baseRadius == null)
                {
                    DebugWindow.LogError($"Unable to grab base radius for entity {entity} {baseEntity}");
                    _rejectedCache.AddOrUpdate(entity, Stopwatch.StartNew());
                    continue;
                }

                var scale = matchingGroup.IgnoreScale ? 1 : entity.GetComponent<Positioned>()?.Scale;
                if (scale == null)
                {
                    DebugWindow.LogError($"Unable to grab scale for entity {entity}  {baseEntity}");
                    _rejectedCache.AddOrUpdate(entity, Stopwatch.StartNew());
                    continue;
                }

                var finalRadius = baseRadius.Value * scale.Value * matchingGroup.CustomScale;
                if (matchingGroup.CircleColor.Value.A > 0)
                {
                    Graphics.DrawFilledCircleInWorld(entity.Pos, finalRadius, matchingGroup.CircleColor);
                }

                if (matchingGroup.BorderColor.Value.A > 0 && matchingGroup.BorderThickness.Value > 0)
                {
                    Graphics.DrawCircleInWorld(entity.Pos, finalRadius, matchingGroup.BorderColor, matchingGroup.BorderThickness);
                }
            }
            else
            {
                if (Settings.CollectUnknownEffects)
                {
                    if (!Settings.UnknownEffects.Content.Any(x => x.Value == baseEntity.Path))
                    {
                        Settings.UnknownEffects.Content.Add(new TextNode(baseEntity.Path));
                    }
                }

                _rejectedCache.AddOrUpdate(entity, Stopwatch.StartNew());
            }
        }
    }
}