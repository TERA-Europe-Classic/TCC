using System.Windows;
using TCC.Data;
using TCC.UI.Windows.Widgets;
using TeraDataLite;

namespace TCC.Tests;

public class ClassPositionsTests
{
    [Fact]
    public void SetAllCopiesPositionAndButtonPlacementToEveryClass()
    {
        var positions = new ClassPositions();

        positions.SetAll(new Point(.42, .24), ButtonsPosition.Below);

        foreach (Class cls in Enum.GetValues(typeof(Class)))
        {
            Assert.Equal(new Point(.42, .24), positions.Position(cls));
            Assert.Equal(ButtonsPosition.Below, positions.Buttons(cls));
        }
    }
}
