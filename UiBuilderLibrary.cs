using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
  [Info("UI Builder Library", "BlueBeka", "0.0.1")]
  [Description("Allows for easily creating complex UIs.")]
  public class UiBuilderLibrary : RustPlugin
  {
    private static UiBuilderLibrary SelfRef;

    private PluginConfig config;
    private PluginData data;

    #region Hooks

    private void Init()
    {
#if DEBUG
      if (SelfRef != null)
        throw new Exception("Self already defined.");
#endif
      SelfRef = this;
      config = new PluginConfig(this);
      data = new PluginData(this);
    }

    private void Loaded()
    {
      LoadConfig();
      data.Load();
    }

    private void OnServerSave()
    {
      data.Save();
    }

    private void Unload()
    {
      data.Save();
      foreach (var ui in UI.GetAllUis())
        ui.CloseAll();
    }

    private void OnUserDisconnected(IPlayer player)
    {
      foreach (var ui in UI.GetAllUis())
        ui.Clear((BasePlayer)player.Object);
    }

    protected override void SaveConfig()
    {
      base.SaveConfig();
      config.Save();
    }

    protected override void LoadConfig()
    {
      base.LoadConfig();
      config.Load();
    }

    #endregion Hooks

    #region Config and Data

    /// <summary>
    /// The config for this plugin.
    /// </summary>
    private class PluginConfig
    {
      // The plugin.
      private readonly UiBuilderLibrary plugin;

      public Data data;

      public PluginConfig(UiBuilderLibrary plugin)
      {
        this.plugin = plugin;
      }

      /// <summary>
      /// Save the config data.
      /// </summary>
      public void Save()
      {
        SaveSerializable(data);
      }

      /// <summary>
      /// Load the config data.
      /// </summary>
      public void Load()
      {
        data = LoadSerializable<Data>();
      }

      /// <summary>
      /// Save a serializable config object to a file.
      /// </summary>
      private void SaveSerializable<T>(T sData)
      {
        SaveSerializable(null, sData);
      }

      /// <summary>
      /// Save a serializable config object to a file.
      /// </summary>
      private void SaveSerializable<T>(string file, T sData)
      {
        Interface.Oxide.LogDebug("Saving");
        plugin.Config.WriteObject(sData, false, string.IsNullOrEmpty(file) ? $"oxide/config/{plugin.Name}.json" : $"oxide/config/{plugin.Name}/{file}.json");
      }

      /// <summary>
      /// Load a serializable config object from a file.
      /// </summary>
      private T LoadSerializable<T>()
      {
        return LoadSerializable<T>(null);
      }

      /// <summary>
      /// Load a serializable config object from a file.
      /// </summary>
      private T LoadSerializable<T>(string file)
      {
        return plugin.Config.ReadObject<T>(string.IsNullOrEmpty(file) ? $"oxide/config/{plugin.Name}.json" : $"oxide/config/{plugin.Name}/{file}.json");
      }

      /// <summary>
      /// The config data for this plugin.
      /// </summary>
      public class Data
      {
        public double DefaultScreenAspectRatio = 16.0 / 9.0;
        public double DefaultRenderScale = 1.0;
      }
    }

    /// <summary>
    /// Everything related to the data that this plugin stores.
    /// </summary>
    private class PluginData
    {
      // The filename to save to.
      private readonly string Filename;

      // The actual data.
      public Dictionary<ulong, Structure> PlayerData { get; protected set; }

      public PluginData(UiBuilderLibrary plugin, string file = null)
      {
        Filename = string.IsNullOrEmpty(file) ? plugin.Name : $"{plugin.Name}/{file}";
      }

      /// <summary>
      /// The data structure this plugin stores.
      /// </summary>
      public class Structure
      {
        public double ScreenAspectRatio;
        public double RenderScale;
      }

      /// <summary>
      /// Save the data.
      /// </summary>
      public void Save()
      {
        Interface.Oxide.DataFileSystem.WriteObject(Filename, PlayerData);
      }

      /// <summary>
      /// Load the data.
      /// </summary>
      public void Load()
      {
        PlayerData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Structure>>(Filename);
        if (PlayerData == null)
          PlayerData = new Dictionary<ulong, Structure>();
      }
    }

    /// <summary>
    /// Get the player data for the given player.
    /// This data can then be mutated.
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    private PluginData.Structure GetPlayerData(BasePlayer player)
    {
      if (data.PlayerData.ContainsKey(player.userID))
        return data.PlayerData[player.userID];

      return data.PlayerData[player.userID] = new PluginData.Structure()
      {
        RenderScale = config.data.DefaultRenderScale,
        ScreenAspectRatio = config.data.DefaultScreenAspectRatio,
      };
    }

    #endregion Config and Data

    #region API methods

    /// <summary>
    /// Get the Screen Aspect Ratio for the given player.
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public double GetScreenAspectRatio(BasePlayer player)
    {
      var playerData = GetPlayerData(player);
      return playerData.ScreenAspectRatio;
    }

    /// <summary>
    /// Sets the Screen Aspect Ratio for the given player.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="value"></param>
    public void SetScreenAspectRatio(BasePlayer player, double value)
    {
      var playerData = GetPlayerData(player);
      playerData.ScreenAspectRatio = value;
    }

    /// <summary>
    /// Get the Render Scale for the given player.
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public double GetRenderScale(BasePlayer player)
    {
      var playerData = GetPlayerData(player);
      return playerData.RenderScale;
    }

    /// <summary>
    /// Sets the Render Scale for the given player.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="value"></param>
    public void SetRenderScale(BasePlayer player, double value)
    {
      var playerData = GetPlayerData(player);
      playerData.RenderScale = value;
    }

    #endregion API methods

    #region API classes

    public class UI
    {
      private static readonly List<WeakReference<UI>> AllUis = new List<WeakReference<UI>>();

      public static readonly string FontBold = "robotocondensed-bold.ttf";
      public static readonly string FontRegular = "robotocondensed-regular.ttf";
      public static readonly string FontMono = "droidsansmono.ttf";
      public static readonly string FontMarker = "permanentmarker.ttf";

      private PanelElement Root { get; }

      /// <summary>
      /// Create a new UI.
      /// </summary>
      /// <param name="parentId">The parent of this UI. For top-level UIs, one of: "Overlay", "Hud.Menu", "Hud" or "Under"</param>
      /// <param name="rootBuilder">Callback function that builds the UI.</param>
      public UI(string parentId, Element.BuildUiFor<PanelElement> rootBuilder)
      {
        Root = new PanelElement(parentId, rootBuilder);
        AllUis.Add(new WeakReference<UI>(this));
      }

      /// <summary>
      /// Get a collection of all the UIs that exist.
      /// </summary>
      /// <returns></returns>
      internal static IEnumerable<UI> GetAllUis()
      {
        var foundUis = new List<UI>();
        var missingUis = new HashSet<WeakReference<UI>>();

        foreach (var uiRef in AllUis)
        {
          UI ui;
          if (uiRef.TryGetTarget(out ui))
            foundUis.Add(ui);
          else
            missingUis.Add(uiRef);
        }

        if (missingUis.Count > 0)
          AllUis.RemoveAll(ui => missingUis.Contains(ui));

        return foundUis;
      }

      /// <summary>
      /// Open this UI for the given player.
      /// </summary>
      /// <param name="player"></param>
      public void Open(BasePlayer player)
      {
        var updatedElements = new List<Element>();
        Root.Build(player, updatedElements, false);

        var roots = GetRootElements(updatedElements);
        var jsonObjects = updatedElements.SelectMany(element => ToJson(element, roots)).ToArray();
        if (jsonObjects.Length == 0)
        {
          Interface.Oxide.LogDebug("Nothing to display/remove.");
          return;
        }

        var json = $"[{string.Join(",", jsonObjects)}]";
        CuiHelper.AddUi(player, json);
      }

      /// <summary>
      /// Close this UI for the given player.
      /// </summary>
      /// <param name="player"></param>
      public void Close(BasePlayer player)
      {
        Clear(player);
        CuiHelper.DestroyUi(player, Root.Id);
      }

      /// <summary>
      /// Close this UI for all players.
      /// </summary>
      public void CloseAll()
      {
        foreach (BasePlayer player in BasePlayer.activePlayerList)
        {
          Close(player);
        }
      }

      /// <summary>
      /// Clear any internally stored UI data for this player.
      /// </summary>
      /// <param name="player"></param>
      internal void Clear(BasePlayer player)
      {
        Root.Clear(player);
      }

      /// <summary>
      /// Get the Screen Aspect Ratio for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <returns></returns>
      public static double GetScreenAspectRatio(BasePlayer player)
      {
        return SelfRef.GetScreenAspectRatio(player);
      }

      /// <summary>
      /// Get the Render Scale for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <returns></returns>
      public static double GetRenderScale(BasePlayer player)
      {
        return SelfRef.GetRenderScale(player);
      }

      /// <summary>
      /// Get a JSON representation of the given Element.
      /// </summary>
      /// <param name="element">What to encode.</param>
      /// <param name="roots">List of the root element ids.</param>
      /// <returns></returns>
      private static IEnumerable<string> ToJson(Element element, HashSet<string> roots)
      {
        var cuiElements = element.GetCuiElements();

        var settings = new JsonSerializerSettings();
        settings.DefaultValueHandling = DefaultValueHandling.Ignore;

        return cuiElements.Select(cuiElement =>
        {
          if (element.Visible)
          {
            var json = JsonConvert.SerializeObject(cuiElement, Formatting.None, settings).Replace("\\n", "\n");
            if (roots.Contains(cuiElement.Name))
              return $"{{\"destroyUi\":\"{cuiElement.Name}\",{json.Substring(1)}";
            return json;
          }

          return $"{{\"destroyUi\":\"{cuiElement.Name}\",\"name\":\"{cuiElement.Name}\"}}";
        });
      }

      /// <summary>
      /// Get a collection of the root element ids from the given collection of elements.
      /// </summary>
      /// <param name="elements"></param>
      /// <returns></returns>
      private static HashSet<string> GetRootElements(IEnumerable<Element> elements)
      {
        var names = new HashSet<string>(elements.Select(element => element.Id));
        return new HashSet<string>(elements.Where(element => !names.Contains(element.GetParentId())).Select(element => element.Id));
      }
    }

    /// <summary>
    /// The base class of all elements.
    /// </summary>
    public abstract class Element
    {
      /// <summary>
      /// The same as `BuildUiFor` but allows for obmitting the player from the callback definition.
      /// </summary>
      /// <see cref="BuildUiFor"/>
      public delegate bool BuildUi<in T>(T self) where T : Element;

      /// <summary>
      /// Build an Element for the given player.
      /// </summary>
      /// <typeparam name="T">The type of element being built.</typeparam>
      /// <param name="element">The element being built.</param>
      /// <param name="player">The player the element is being built for.</param>
      /// <returns>True iff the UI element needs to be refreshed on player's screen.</returns>
      public delegate bool BuildUiFor<in T>(T element, BasePlayer player) where T : Element;

      public string Id { get; }

      public bool Visible = true;

      // `Parent` should be null if `ParentId` is set.
      private readonly string ParentId;

      private readonly Element Parent;
      protected readonly List<Element> Children = new List<Element>();

      protected bool Initialized = false;
      private readonly HashSet<ulong> OpenFor = new HashSet<ulong>();
      private int BuildingChildIndex = 0;

      public BoundingBox Bounds { get; }

      public Element(Element parent)
      {
        Id = CuiHelper.GetGuid();
        Parent = parent;
        ParentId = null;
        Bounds = new BoundingBox(Parent);
      }

      public Element(string parentId)
      {
        Id = CuiHelper.GetGuid();
        Parent = null;
        ParentId = parentId;
        Bounds = new BoundingBox(Parent);
      }

      /// <summary>
      /// Get the id of this element's parent.
      /// </summary>
      /// <returns></returns>
      public string GetParentId()
      {
        return Parent?.Id ?? ParentId;
      }

      /// <summary>
      /// Add a child to this element.
      /// If the element has already been initialized, use the other version of this function.
      /// </summary>
      /// <param name="element"></param>
      protected void AddChild(Element element)
      {
#if DEBUG
        if (Initialized)
          throw new Exception("Cannot add child once initialized.");
        if (element == null)
          throw new Exception("Cannot add null as child.");
        if (element == this)
          throw new Exception("Cannot add self as child.");
#endif

        Children.Add(element);
      }

      /// <summary>
      /// Mark that a child has been added to this element.
      /// If the element has not yet been initialized, use the other version of this function.
      /// </summary>
      /// <typeparam name="T">The type of the child.</typeparam>
      /// <returns>The child.</returns>
      protected T AddChild<T>() where T : Element
      {
#if DEBUG
        if (!Initialized)
          throw new Exception("Not yet initialized.");
#endif

        var element = (T)Children[BuildingChildIndex];
        BuildingChildIndex++;
        return element;
      }

      /// <summary>
      /// Build this element and its children.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="updatedElements"></param>
      /// <param name="parentHasUpdates"></param>
      /// <exception cref="Exception"></exception>
      public void Build(BasePlayer player, List<Element> updatedElements, bool parentHasUpdates)
      {
        parentHasUpdates = parentHasUpdates || !OpenFor.Contains(player.userID);

        BuildingChildIndex = 0;

        var hasUpdates = DoBuild(player) || parentHasUpdates;

        if (hasUpdates)
          updatedElements.Add(this);

#if DEBUG
        if (Initialized && Visible && BuildingChildIndex != Children.Count)
          throw new Exception($"[{GetType().Name}] Different number of children after update ({Children.Count} => {BuildingChildIndex}).");
#endif

        if (Visible)
          foreach (var child in Children)
            child.Build(player, updatedElements, hasUpdates);

        OpenFor.Add(player.userID);
        Initialized = true;
      }

      /// <summary>
      /// Build this element.
      /// </summary>
      /// <param name="player"></param>
      /// <returns></returns>
      public abstract bool DoBuild(BasePlayer player);

      /// <summary>
      /// Clear any internally stored data for this player.
      /// </summary>
      /// <param name="player"></param>
      public void Clear(BasePlayer player)
      {
        foreach (var child in Children)
          child.Clear(player);

        OpenFor.Remove(player.userID);
      }

      /// <summary>
      /// Get all the CuiElements for this element.
      /// </summary>
      /// <returns></returns>
      public IEnumerable<CuiElement> GetCuiElements()
      {
        var parentId = Parent?.Id ?? ParentId;
#if DEBUG
        if (parentId == null)
          throw new Exception("Element doesn't have parent.");
#endif
        return GetCuiElements(parentId);
      }

      /// <summary>
      /// Get all the CuiElements for this element using the given parent id.
      /// </summary>
      /// <returns></returns>
      protected internal abstract IEnumerable<CuiElement> GetCuiElements(string parentId);

      public class BoundingBox
      {
        private readonly Element Parent;

        public double MinX { get; set; } = 0;
        public double MinY { get; set; } = 0;
        public double MaxX { get; set; } = 1;
        public double MaxY { get; set; } = 1;

        private readonly CuiRectTransformComponent cui = new CuiRectTransformComponent();

        public BoundingBox(Element parent)
        {
          Parent = parent;
        }

        /// <summary>
        /// Gets the aspect ratio of this element on the given player's screen.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public double GetAspectRatio(BasePlayer player)
        {
          return GetRelativeWidth(player) / GetRelativeHeight(player);
        }

        /// <summary>
        /// Get the width of this element (as a percentage of its parent's width).
        /// </summary>
        /// <returns></returns>
        public double GetWidth()
        {
          return MaxX - MinX;
        }

        /// <summary>
        /// Get the height of this element (as a percentage of its parent's height).
        /// </summary>
        /// <returns></returns>
        public double GetHeight()
        {
          return MaxY - MinY;
        }

        /// <summary>
        /// Get the width of this element as a percentage of the player's screen width.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public double GetRelativeWidth(BasePlayer player)
        {
          if (Parent == null)
          {
            return GetWidth();
          }
          return GetWidth() * Parent.Bounds.GetRelativeWidth(player);
        }

        /// <summary>
        /// Get the height of this element as a percentage of the player's screen **width** (not height).
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public double GetRelativeHeight(BasePlayer player)
        {
          if (Parent == null)
          {
            return GetHeight() / UI.GetScreenAspectRatio(player);
          }
          return GetHeight() * Parent.Bounds.GetRelativeHeight(player);
        }

        /// <summary>
        /// Get the `CuiRectTransformComponent` this object represents.
        /// </summary>
        /// <returns></returns>
        public CuiRectTransformComponent GetCuiComponent()
        {
          cui.AnchorMin = $"{MinX} {MinY}";
          cui.AnchorMax = $"{MaxX} {MaxY}";
          return cui;
        }
      }
    }

    /// <summary>
    /// A Panel.
    /// </summary>
    public class PanelElement : Element
    {
      public readonly CuiImageComponent Image = new CuiImageComponent();
      public bool CursorEnabled = false;
      public bool KeyboardEnabled = false;

      protected internal BuildUiFor<PanelElement> BuilderFor { get; set; }

      internal PanelElement(Element parent, BuildUi<PanelElement> builder) : this(parent, (self, player) => builder(self))
      {
      }

      internal PanelElement(Element parent, BuildUiFor<PanelElement> builder) : base(parent)
      {
        BuilderFor = builder;
      }

      internal PanelElement(string parentId, BuildUi<PanelElement> builder) : this(parentId, (self, player) => builder(self))
      {
      }

      internal PanelElement(string parentId, BuildUiFor<PanelElement> builder) : base(parentId)
      {
        BuilderFor = builder;
      }

      protected internal override IEnumerable<CuiElement> GetCuiElements(string parentId)
      {
        CuiElement cuiElement = new CuiElement()
        {
          Name = Id,
          Parent = parentId,
        };

        cuiElement.Components.Add(Image);
        cuiElement.Components.Add(Bounds.GetCuiComponent());

        if (CursorEnabled)
          cuiElement.Components.Add(new CuiNeedsCursorComponent());

        if (KeyboardEnabled)
          cuiElement.Components.Add(new CuiNeedsKeyboardComponent());

        return new CuiElement[] { cuiElement };
      }

      public override bool DoBuild(BasePlayer player)
      {
        return BuilderFor(this, player);
      }

      public void AddPanel(BuildUi<PanelElement> builder)
      {
        AddPanel((self, player) => builder(self));
      }

      public void AddPanel(BuildUiFor<PanelElement> builder)
      {
        if (Initialized)
        {
          var element = AddChild<PanelElement>();
          element.BuilderFor = builder;
          return;
        }

        AddChild(new PanelElement(this, builder));
      }

      public void AddGameImage(BuildUi<GameImageElement> builder)
      {
        AddGameImage((self, player) => builder(self));
      }

      public void AddGameImage(BuildUiFor<GameImageElement> builder)
      {
        if (Initialized)
        {
          var element = AddChild<GameImageElement>();
          element.BuilderFor = builder;
          return;
        }

        AddChild(new GameImageElement(this, builder));
      }

      public void AddRawImage(BuildUi<RawImageElement> builder)
      {
        AddRawImage((self, player) => builder(self));
      }

      public void AddRawImage(BuildUiFor<RawImageElement> builder)
      {
        if (Initialized)
        {
          var element = AddChild<RawImageElement>();
          element.BuilderFor = builder;
          return;
        }

        AddChild(new RawImageElement(this, builder));
      }

      public void AddLabel(BuildUi<LabelElement> builder)
      {
        AddLabel((self, player) => builder(self));
      }

      public void AddLabel(BuildUiFor<LabelElement> builder)
      {
        if (Initialized)
        {
          var element = AddChild<LabelElement>();
          element.BuilderFor = builder;
          return;
        }

        AddChild(new LabelElement(this, builder));
      }

      public void AddButton(BuildUi<ButtonElement> builder)
      {
        AddButton((self, player) => builder(self));
      }

      public void AddButton(BuildUiFor<ButtonElement> builder)
      {
        if (Initialized)
        {
          var element = AddChild<ButtonElement>();
          element.BuilderFor = builder;
          return;
        }

        AddChild(new ButtonElement(this, builder));
      }

      public void AddTabs(BuildUi<TabsElement> builder)
      {
        AddTabs((self, player) => builder(self));
      }

      public void AddTabs(BuildUiFor<TabsElement> builder)
      {
        if (Initialized)
        {
          var element = AddChild<TabsElement>();
          element.BuilderFor = (self, player) => builder((TabsElement)self, player);
          return;
        }

        AddChild(new TabsElement(this, builder));
      }

      public void AddGrid(BuildUi<GridElement> builder)
      {
        AddGrid((self, player) => builder(self));
      }

      public void AddGrid(BuildUiFor<GridElement> builder)
      {
        if (Initialized)
        {
          var element = AddChild<GridElement>();
          element.BuilderFor = (self, player) => builder((GridElement)self, player);
          return;
        }

        AddChild(new GridElement(this, builder));
      }
    }

    /// <summary>
    /// A Label
    /// </summary>
    public class LabelElement : Element
    {
      protected internal BuildUiFor<LabelElement> BuilderFor { get; set; }

      public readonly CuiTextComponent Text = new CuiTextComponent();

      internal LabelElement(Element parent, BuildUi<LabelElement> builder) : this(parent, (self, player) => builder(self))
      {
      }

      internal LabelElement(Element parent, BuildUiFor<LabelElement> builder) : base(parent)
      {
        BuilderFor = builder;
      }

      public override bool DoBuild(BasePlayer player)
      {
        return BuilderFor(this, player);
      }

      protected internal override IEnumerable<CuiElement> GetCuiElements(string parentId)
      {
        CuiElement cuiElement = new CuiElement()
        {
          Name = Id,
          Parent = parentId,
        };

        if (!string.IsNullOrEmpty(Text?.Text))
          cuiElement.Components.Add(Text);

        cuiElement.Components.Add(Bounds.GetCuiComponent());

        return new CuiElement[] { cuiElement };
      }
    }

    /// <summary>
    /// A Text Button
    /// </summary>
    public class ButtonElement : Element
    {
      protected internal BuildUiFor<ButtonElement> BuilderFor { get; set; }

      public readonly CuiButtonComponent Button = new CuiButtonComponent();
      public readonly CuiTextComponent Text = new CuiTextComponent();

      internal ButtonElement(Element parent, BuildUi<ButtonElement> builder) : this(parent, (self, player) => builder(self))
      {
      }

      internal ButtonElement(Element parent, BuildUiFor<ButtonElement> builder) : base(parent)
      {
        BuilderFor = builder;
      }

      public override bool DoBuild(BasePlayer player)
      {
        return BuilderFor(this, player);
      }

      protected internal override IEnumerable<CuiElement> GetCuiElements(string parentId)
      {
        var cuiElements = new List<CuiElement>
        {
          new CuiElement()
          {
            Name = Id,
            Parent = parentId,
            Components =
            {
              Button,
              Bounds.GetCuiComponent()
            }
          }
        };

        if (!string.IsNullOrEmpty(Text?.Text))
        {
          cuiElements.Add(new CuiElement
          {
            Name = CuiHelper.GetGuid(),
            Parent = Id,
            Components = {
              Text,
              new CuiRectTransformComponent()
            }
          });
        }

        return cuiElements;
      }
    }

    /// <summary>
    /// A raw image.
    /// </summary>
    public class RawImageElement : Element
    {
      protected internal BuildUiFor<RawImageElement> BuilderFor { get; set; }

      public readonly CuiRawImageComponent Image = new CuiRawImageComponent();

      internal RawImageElement(Element parent, BuildUi<RawImageElement> builder) : this(parent, (self, player) => builder(self))
      {
      }

      internal RawImageElement(Element parent, BuildUiFor<RawImageElement> builder) : base(parent)
      {
        BuilderFor = builder;
      }

      public override bool DoBuild(BasePlayer player)
      {
        return BuilderFor(this, player);
      }

      protected internal override IEnumerable<CuiElement> GetCuiElements(string parentId)
      {
        CuiElement cuiElement = new CuiElement()
        {
          Name = Id,
          Parent = parentId,
          Components = {
            Image,
            Bounds.GetCuiComponent()
          }
        };

        return new CuiElement[] { cuiElement };
      }
    }

    /// <summary>
    /// An game image.
    /// </summary>
    public class GameImageElement : Element
    {
      protected internal BuildUiFor<GameImageElement> BuilderFor { get; set; }

      public readonly CuiImageComponent Image = new CuiImageComponent();

      internal GameImageElement(Element parent, BuildUi<GameImageElement> builder) : this(parent, (self, player) => builder(self))
      {
      }

      internal GameImageElement(Element parent, BuildUiFor<GameImageElement> builder) : base(parent)
      {
        BuilderFor = builder;
      }

      public override bool DoBuild(BasePlayer player)
      {
        return BuilderFor(this, player);
      }

      protected internal override IEnumerable<CuiElement> GetCuiElements(string parentId)
      {
        CuiElement cuiElement = new CuiElement()
        {
          Name = Id,
          Parent = parentId,
          Components = {
            Image,
            Bounds.GetCuiComponent()
          }
        };

        return new CuiElement[] { cuiElement };
      }
    }

    /// <summary>
    /// Text Tabs
    /// </summary>
    public class TabsElement : PanelElement
    {
      protected internal BuildUiFor<TabsElement> TabsBuilderFor { get; set; }

      public bool Vertical = false;
      public double Gap = 0;
      public double MaxButtonSize = 0;

      private int _BuildingTabIndex = 0;
      private int TabsCount = 0;

      internal TabsElement(Element parent, BuildUi<TabsElement> builder) : this(parent, (self, player) => builder(self))
      {
      }

      internal TabsElement(Element parent, BuildUiFor<TabsElement> builder) : base(parent, (self, player) => builder((TabsElement)self, player))
      {
        TabsBuilderFor = (tabs, player) =>
        {
          _BuildingTabIndex = 0;
          return BuilderFor(tabs, player);
        };
      }

      public override bool DoBuild(BasePlayer player)
      {
        return TabsBuilderFor(this, player);
      }

      public void AddTab(BuildUi<ButtonElement> builder)
      {
        AddTab((self, player) => builder(self));
      }

      public void AddTab(BuildUiFor<ButtonElement> builder)
      {
        if (!Initialized)
        {
          TabsCount++;
        }

        AddButton((button, player) =>
        {
          SetTabDefaults(button);
          _BuildingTabIndex++;
          return builder(button, player);
        });
      }

      private void SetTabDefaults(ButtonElement button)
      {
        var gapSize = Gap;
        var buttonSize = 1.0 / TabsCount - gapSize * (TabsCount - 1) / TabsCount;
        buttonSize = MaxButtonSize > 0 ? Math.Min(buttonSize, MaxButtonSize) : buttonSize;
        var offset = (double)_BuildingTabIndex / TabsCount * (1 + gapSize);

        if (Vertical)
        {
          button.Bounds.MinX = 0;
          button.Bounds.MaxX = 1;
          button.Bounds.MinY = 1 - offset - buttonSize;
          button.Bounds.MaxY = 1 - offset;
        }
        else
        {
          button.Bounds.MinX = offset;
          button.Bounds.MaxX = offset + buttonSize;
          button.Bounds.MinY = 0;
          button.Bounds.MaxY = 1;
        }
      }
    }

    /// <summary>
    /// A Grid of panels
    /// </summary>
    public class GridElement : PanelElement
    {
      protected internal BuildUiFor<GridElement> GridBuilderFor { get; set; }

      public double GapX = 0;
      public double GapY = 0;

      public int Rows = 2;
      public int Columns = 2;

      private int _BuildingTabIndex = 0;

      internal GridElement(Element parent, BuildUi<GridElement> builder) : this(parent, (self, player) => builder(self))
      {
      }

      internal GridElement(Element parent, BuildUiFor<GridElement> builder) : base(parent, (self, player) => builder((GridElement)self, player))
      {
        GridBuilderFor = (cells, player) =>
        {
          _BuildingTabIndex = 0;
          return BuilderFor(cells, player);
        };
      }

      public override bool DoBuild(BasePlayer player)
      {
        return GridBuilderFor(this, player);
      }

      public void AddCell(BuildUi<PanelElement> builder)
      {
        AddCell((self, player) => builder(self));
      }

      public void AddCell(BuildUiFor<PanelElement> builder)
      {
        AddPanel((cell, player) =>
        {
          SetCellDefaults(cell);
          _BuildingTabIndex++;
          return builder(cell, player);
        });
      }

      private void SetCellDefaults(PanelElement cell)
      {
        var column = _BuildingTabIndex % Columns;
        var width = 1.0 / Columns - GapX * (Columns - 1) / Columns;
        var offsetX = (double)column / Columns * (1 + GapX);

        var row = _BuildingTabIndex / Columns;
        var height = 1.0 / Rows - GapY * (Rows - 1) / Rows;
        var offsetY = (double)row / Rows * (1 + GapY);

        cell.Bounds.MinX = offsetX;
        cell.Bounds.MaxX = offsetX + width;
        cell.Bounds.MinY = 1 - offsetY - height;
        cell.Bounds.MaxY = 1 - offsetY;
      }
    }

    #endregion API classes
  }
}
