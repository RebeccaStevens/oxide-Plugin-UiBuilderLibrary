# UiBuilderLibrary

A Library that allows for easily creating complex UIs in Rust.\
By itself this plugin doesn't really do anything.

[![BSD 3 Clause license](https://img.shields.io/github/license/RebeccaStevens/eslint-config-rebeccastevens.svg?style=flat-square)](https://opensource.org/licenses/BSD-3-Clause)
[![Commitizen friendly](https://img.shields.io/badge/commitizen-friendly-brightgreen.svg?style=flat-square)](https://commitizen.github.io/cz-cli/)

## Configuration

Default configuration:

```jsonc
{
  "DefaultScreenAspectRatio": 1.7777777777777777, // 16 : 9
  "DefaultRenderScale": 1.0
}
```

## Example

```cs
var MyUi = new UI("Overlay", (rootPanel) =>
{
  rootPanel.CursorEnabled = true;
  rootPanel.Image.Color = "1 1 1 0.5";
  rootPanel.Bounds.MinX = 0.2;
  rootPanel.Bounds.MaxX = 0.8;
  rootPanel.Bounds.MinY = 0.2;
  rootPanel.Bounds.MaxY = 0.8;

  rootPanel.AddPanel((panel) => {
    // ...
  });
  rootPanel.AddLabel((label) => {
    // ...
  });
  rootPanel.AddButton((button) => {
    // ...
  });
  rootPanel.AddGameImage((gameImage) => {
    // ...
  });
  rootPanel.AddRawImage((rawImage) => {
    // ...
  });
  rootPanel.AddTabs((tabs) => {
    tabs.Vertical = true;
    tabs.AddTab((tab) => {
      // ...
    });
  });
  rootPanel.AddGrid((grid) => {
    grid.Rows = 3;
    grid.Columns = 4;
    for (var i = 0; i < grid.Rows * grid.Columns; i++) {
      grid.AddCell((cell) => {
        // ...
      });
    }
  });
}

MyUi.Open(player);
MyUi.Close(player);
```

## The Render Function

When defining an element, you need to provided a render function for it.
This function should perform all of the changes that you want to apply to the element.
This includes setting the element's bounds, color and children.

The render function must return a boolean.
This boolean is only used when refreshing the content of an already open element.
It indicates whether or not the element's content has changed since it was last rendered (it has no effect when opening a closed element).

## Dos and Don'ts

Each rendering of an element must add all the same children in the same order. You cannot dynamically add or remove them.

❌ Don't
```cs
if (comeCondition) {
  rootPanel.AddPanel((panel) => {});
}
```

✅ Do
```cs
rootPanel.AddPanel((panel) => {
  panel.Visible = comeCondition
});
```

## Development

### Bug Report or Feature Request

Open an issue on [GitHub](https://github.com/RebeccaStevens/uMod-Rust-Plugin-AutoCode/issues/new/choose).

### Want to contribute

Fork and clone the [GitHub repository](https://github.com/RebeccaStevens/uMod-Rust-Plugin-AutoCode). Send me a PR :)
