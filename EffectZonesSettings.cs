using System.Drawing;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using Newtonsoft.Json;

namespace EffectZones;

public class EffectZonesSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);
    public ToggleNode IgnoreFullscreenPanels { get; set; } = new();
    public ToggleNode IgnoreLargePanels { get; set; } = new();
    public RangeNode<int> EntityLookupRange { get; set; } = new RangeNode<int>(120, 0, 200);

    [Menu(null, CollapsedByDefault = true)]
    public ContentNode<EntityGroup> EntityGroups { get; set; } = new ContentNode<EntityGroup>() { ItemFactory = () => new EntityGroup() };

    [Menu(null, CollapsedByDefault = true)]
    public ContentNode<TextNode> BlacklistTemplates { get; set; } = new ContentNode<TextNode>() { UseFlatItems = true, ItemFactory = () => new TextNode("^$") };

    public ToggleNode CollectUnknownEffects { get; set; } = new ToggleNode(true);

    [JsonIgnore]
    public ButtonNode RemoveMatchedUnknownEffects { get; set; } = new ButtonNode();
    [JsonIgnore]
    public ButtonNode RemoveAllUnknownEffects { get; set; } = new ButtonNode();

    [Menu(null, CollapsedByDefault = true)]
    public ContentNode<TextNode> UnknownEffects { get; set; } = new ContentNode<TextNode>() { UseFlatItems = true, };
    [Menu(null, CollapsedByDefault = true)]
    public ContentNode<TextNode> LethalUnknownEffects { get; set; } = new ContentNode<TextNode>() { UseFlatItems = true, };
    public ToggleNode EnableDebugging { get; set; } = new ToggleNode(false);
}

public class EntityGroup
{
    public ContentNode<TextNode> PathTemplates { get; set; } = new ContentNode<TextNode>() { UseFlatItems = true, ItemFactory = () => new TextNode("^$") };
    public ColorNode CircleColor { get; set; } = new ColorNode(Color.Transparent);
    public RangeNode<float> BorderThickness { get; set; } = new RangeNode<float>(1, 0, 10);
    public ColorNode BorderColor { get; set; } = new ColorNode(Color.White);
    public RangeNode<float> CustomScale { get; set; } = new RangeNode<float>(1, 0, 10);
	public ToggleNode IgnoreBaseSize { get; set; } = new ToggleNode(false);
    public RangeNode<float> CustomSize { get; set; } = new RangeNode<float>(1, 0, 100);
    public RangeNode<int> BaseSizeOverride { get; set; } = new RangeNode<int>(0, 0, 2000);
    public ToggleNode IgnoreScale { get; set; } = new ToggleNode(false);
    public ToggleNode PlayAlert { get; set; } = new ToggleNode(false);
}