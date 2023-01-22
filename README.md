<div align="center">

# Ui Builder Library

[![BSD 3 Clause license](https://img.shields.io/github/license/RebeccaStevens/eslint-config-rebeccastevens.svg?style=flat-square)](https://opensource.org/licenses/BSD-3-Clause)
[![Commitizen friendly](https://img.shields.io/badge/commitizen-friendly-brightgreen.svg?style=flat-square)](https://commitizen.github.io/cz-cli/)

A Library that allows for easily creating complex UIs in Rust.\
By itself this plugin doesn't really do anything.

</div>

## Getting Started

This library is easiest used as a [hard dependance](https://umod.org/documentation/api/dependencies#hard-dependencies). To use it as such, add the following to the very top of you `cs` file.
```cs
// Requires: UiBuilderLibrary
```

You can then add this line in-order to easily access this library's classes.
```cs
using static Oxide.Plugins.UiBuilderLibrary;
```

Note: In order to use intellisense, make sure your code editor knows about this library.

### Example

```cs
// Requires: UiBuilderLibrary

using Oxide.Core.Libraries.Covalence;
using static Oxide.Plugins.UiBuilderLibrary;

namespace Oxide.Plugins
{
  public class MyPlugin : CovalencePlugin
  {
    private UI MyUi;

    private void Loaded()
    {
      MyUi = new UI("Overlay", (mainPanel) =>
      {
        mainPanel.CursorEnabled = true;
        mainPanel.Image.Color = "1 1 1 0.5";
        mainPanel.Bounds.MinX = 0.1;
        mainPanel.Bounds.MaxX = 0.9;
        mainPanel.Bounds.MinY = 0.2;
        mainPanel.Bounds.MaxY = 0.8;

        mainPanel.AddPanel((panel) => {
          // ...
        });
        mainPanel.AddLabel((label) => {
          // ...
        });
        mainPanel.AddButton((button) => {
          // ...
        });
        mainPanel.AddGameImage((gameImage) => {
          // ...
        });
        mainPanel.AddRawImage((rawImage) => {
          // ...
        });
        mainPanel.AddTabs((tabs) => {
          tabs.Vertical = true;
          tabs.AddTab((tab) => {
            // ...
          });
        });
        mainPanel.AddGrid((grid) => {
          grid.Rows = 3;
          grid.Columns = 4;
          for (var i = 0; i < grid.Rows * grid.Columns; i++)
          {
            grid.AddCell((cell) => {
              // ...
            });
          }
        });

        return false;
      });
    }

    [Command("open")]
    private void OpenCommand(IPlayer player, string label, string[] args)
    {
      MyUi.Open((BasePlayer)player.Object); // `Open` acts as refresh if the UI is already open.
    }

    [Command("close")]
    private void CloseCommand(IPlayer player, string label, string[] args)
    {
      MyUi.Close((BasePlayer)player.Object);
    }
  }
}
```

## The Render Function

When defining an element, you need to provided a render function for it.
This function should perform all of the changes that you want to apply to the element.
This includes setting the element's bounds, color and children.

The render function must return a boolean.
This boolean is only used when refreshing the content of an already open element.
It indicates whether or not the element's content has changed since it was last rendered (it has no effect when opening a closed element).

## Dos and Don'ts

### Defining Children

Each rendering of an element must add all the same children in the same order. You cannot dynamically add or remove them.
You can however, toggle their visibility.

❌ Don't
```cs
if (comeCondition) {
  mainPanel.AddPanel((panel) => {});
}
```

✅ Do
```cs
mainPanel.AddPanel((panel) => {
  panel.Visible = comeCondition
});
```

## Tips and Tricks

### Accessing the Player

The player that the element is being rendered for can be accessed via the `Player` property on the element.
```cs
new UI("Overlay", (mainPanel) => {
  var player = mainPanel.Player;
});
```

### Equal X and Y spacing between elements

You can obtain equal spacing between elements using the following pattern:
```cs
// Choose the size for the gaps.
var GapFactor = 0.01;

new UI("Overlay", (mainPanel) => {
  // Using the player's screen aspect ratio, calculate the x and y gap values.
  var screenAspectRatio = UI.GetScreenAspectRatio(mainPanel.Player);
  var screenGapX = GapFactor;
  var screenGapY = GapFactor * screenAspectRatio;

  // First set the bounds of this element.
  mainPanel.Bounds.MinX = 0.1;
  mainPanel.Bounds.MaxX = 0.9;
  mainPanel.Bounds.MinY = 0.2;
  mainPanel.Bounds.MaxY = 0.8;

  // Then calculate the new gap values for any child elements.
  var mainGapX = screenGapX / main.Bounds.GetWidth();
  var mainGapY = screenGapY / main.Bounds.GetHeight();

  mainPanel.AddPanel((subPanel) => {
    // The child can now use these gap size to define it's bounds.
    subPanel.Bounds.MinX = mainGapX;
    subPanel.Bounds.MaxX = 1 - mainGapX;
    subPanel.Bounds.MinY = mainGapY;
    subPanel.Bounds.MaxY = 1 - mainGapY;

    // If this child has it's own children that want to use gaps, calculate the new gap values for them.
    var subPanelGapX = mainGapX / subPanel.Bounds.GetWidth();
    var subPanelGapY = mainGapY / subPanel.Bounds.GetHeight();

    // ...
  });
});
```

## Configuration

Default configuration:

```jsonc
{
  "DefaultScreenAspectRatio": 1.7777777777777777, // 16 : 9
  "DefaultRenderScale": 1.0
}
```

## Development

### Bug Report or Feature Request

Open an issue on [GitHub](https://github.com/RebeccaStevens/uMod-Rust-Plugin-AutoCode/issues/new/choose).

### Want to contribute

Fork and clone the [GitHub repository](https://github.com/RebeccaStevens/uMod-Rust-Plugin-AutoCode). Send me a PR :)
