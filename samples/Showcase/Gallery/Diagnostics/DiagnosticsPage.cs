using Godot;

namespace Samples.Showcase.Gallery.Diagnostics;

/// <summary>
/// Diagnostics — compile-time source-generator codes (DOTPUDICA0xx).
/// See DiagnosticsDemo.Bad.cs and Wiki/Diagnostics.md to trigger live examples.
/// </summary>
public partial class DiagnosticsPage : ShowcasePageWindow
{
    public override void _Ready()
    {
        var body = ShowcaseUi.AttachPageBody(this, scroll: true);
        var root = body.Root;

        ShowcaseUi.AddSubtitle(root,
            $"{DiagnosticCatalog.All.Count} compile-time analyzer codes · define DOTPUDICA_DIAGNOSTICS_DEMO to trigger live examples");

        foreach (var info in DiagnosticCatalog.All)
            root.AddChild(BuildCard(info));
    }

    private static Control BuildCard(DiagnosticInfo info)
    {
        var cardBody = ShowcaseUi.CreateCardBody(out var panel);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 10);
        cardBody.AddChild(header);

        header.AddChild(new Label
        {
            Text = info.Id,
            Modulate = ShowcaseTheme.Section,
            CustomMinimumSize = new Vector2(110, 0)
        });

        header.AddChild(new Label
        {
            Text = info.Title,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Modulate = ShowcaseTheme.Text
        });

        header.AddChild(new Label
        {
            Text = info.Severity,
            Modulate = info.Severity == "Error" ? ShowcaseTheme.Danger : ShowcaseTheme.Warning
        });

        cardBody.AddChild(new Label
        {
            Text = info.Description,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = ShowcaseTheme.Muted
        });

        return panel;
    }
}
