using System.Windows;

namespace CheemsUI;

/// <summary>Uiverse PauloRFJ three-position selector whose inactive lamps are neutral dark gray.</summary>
public sealed class CheemsDarkTrafficLightSwitch : CheemsTrafficLightSwitch
{
    static CheemsDarkTrafficLightSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CheemsDarkTrafficLightSwitch),
            new FrameworkPropertyMetadata(typeof(CheemsDarkTrafficLightSwitch)));

        SelectedSignalProperty.OverrideMetadata(
            typeof(CheemsDarkTrafficLightSwitch),
            new FrameworkPropertyMetadata(
                CheemsTrafficSignal.Green,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    }
}
