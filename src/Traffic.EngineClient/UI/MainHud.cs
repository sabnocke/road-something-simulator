namespace RoadTrafficSim.UI;

using Godot;
using RoadTrafficSim.Interaction;

public partial class MainHud : Control
{
    [Export] public RoadBuilder RoadBuilder { get; set; } = null!;

    public override void _Ready()
    {
        // Build mode toggle
        var buildButton = new CheckButton
        {
            Text = "Road Tool",
            ButtonPressed = true,
            Position = new Vector2(20, 20)
        };
        buildButton.Toggled += toggled =>
        {
            RoadBuilder.IsBuildModeActive = toggled;
            if (!toggled) RoadBuilder.CancelPlacement();
        };
        AddChild(buildButton);
        
        // Help Label
        var infoLabel = new Label
        {
            Text = "Left-Click: Place / Chain Node\nRight-Click: Finish / Cancel Road",
            Position = new Vector2(20, 60)
        };
        AddChild(infoLabel);
    }
}