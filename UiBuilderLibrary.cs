#define DEBUG

using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using static Oxide.Plugins.UiBuilderLibrary.Element.Instance;

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

      private RootElement Root { get; }

      /// <summary>
      /// Create a new UI.
      /// </summary>
      /// <param name="parentId">The parent of this UI. For top-level UIs, one of: "Overlay", "Hud.Menu", "Hud" or "Under"</param>
      /// <param name="rootBuilder">Callback function that builds the UI.</param>
      public UI(string parentId, RootElement.RenderUi rootBuilder)
      {
        Root = new RootElement(parentId, rootBuilder);
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
        var updatedElements = new List<Element.Instance>();
        Root.Build(player, updatedElements, false);
        var root = Root.GetInstance<RootElement.Instance>(player);

        var roots = GetRootElements(updatedElements);
        var jsonObjects = updatedElements.SelectMany(element => ToJson(element, roots)).ToArray();
        if (jsonObjects.Length == 0)
        {
          Interface.Oxide.LogDebug("Nothing to display/remove.");
          return;
        }

        var json = $"[{string.Join(",", jsonObjects)}]";
        CuiHelper.AddUi(player, json);
        root.Open = true;
      }

      /// <summary>
      /// Close this UI for the given player.
      /// </summary>
      /// <param name="player"></param>
      public void Close(BasePlayer player)
      {
        var root = Root.GetInstance<RootElement.Instance>(player);
        if (root == null)
          return;

        root.Clear();
        CuiHelper.DestroyUi(player, root.Id);
        root.Open = false;
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
      /// Set the Screen Aspect Ratio for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="value"></param>
      /// <returns></returns>
      public static void SetScreenAspectRatio(BasePlayer player, double value)
      {
        SelfRef.SetScreenAspectRatio(player, value);
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
      /// Set the Render Scale for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="value"></param>
      /// <returns></returns>
      public static void SetRenderScale(BasePlayer player, double value)
      {
        SelfRef.SetRenderScale(player, value);
      }

      /// <summary>
      /// Get a JSON representation of the given Element.
      /// </summary>
      /// <param name="element">What to encode.</param>
      /// <param name="roots">List of the root element ids.</param>
      /// <returns></returns>
      private static IEnumerable<string> ToJson(Element.Instance element, HashSet<string> roots)
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
      private static HashSet<string> GetRootElements(IEnumerable<Element.Instance> elements)
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
      protected readonly Dictionary<ulong, Instance> InstanceCache = new Dictionary<ulong, Instance>();

      // `Parent` should be null if `ParentId` is set.
      private readonly string ParentId;

      private readonly Element Parent;

      internal Element(Element parent)
      {
        Parent = parent;
        ParentId = null;
      }

      internal Element(string parentId)
      {
        Parent = null;
        ParentId = parentId;
      }

      /// <summary>
      /// Get the instance of this element for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <returns></returns>
      public T GetInstance<T>(BasePlayer player) where T : Instance
      {
        Instance instance = null;
        return InstanceCache.TryGetValue(player.userID, out instance) ? (T)instance : null;
      }

      /// <summary>
      /// Build this element for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="updatedElements"></param>
      /// <param name="parentHasUpdates"></param>
      /// <exception cref="Exception"></exception>
      public abstract void Build(BasePlayer player, List<Instance> updatedElements, bool parentHasUpdates);

      /// <summary>
      /// Clear any internally stored data for this player.
      /// </summary>
      /// <param name="player"></param>
      public void Clear(BasePlayer player)
      {
        if (!InstanceCache.ContainsKey(player.userID))
          return;

        InstanceCache[player.userID].Clear();
        InstanceCache.Remove(player.userID);
      }

      /// <summary>
      /// An instance of the element for an indiviual player.
      /// </summary>
      public abstract class Instance
      {
        /// <summary>
        /// Build an Element for the given player.
        /// </summary>
        /// <typeparam name="T">The type of instance being rendered.</typeparam>
        /// <param name="instance">The instance being rendered.</param>
        /// <returns>True iff the UI element needs to be refreshed on player's screen.</returns>
        public delegate bool RenderUi<in T>(T instance) where T : Instance;

        public string Id { get; }

        protected readonly Element Element;
        protected readonly Instance Parent;
        public BasePlayer Player { get; private set; }
        public BoundingBox Bounds { get; private set; }
        public bool Visible { get; set; } = true;

        protected readonly List<Element> Children = new List<Element>();
        private int BuildingChildIndex = 0;
        internal bool Initialized;

        public Instance(Element element, BasePlayer player)
        {
          Id = CuiHelper.GetGuid();
          Element = element;
          Player = player;
          Parent = Element.Parent == null ? null : Element.Parent.GetInstance<Instance>(Player);
          Bounds = new BoundingBox(Parent);
          Initialized = false;
        }

        /// <summary>
        /// Get the id of this element's parent.
        /// </summary>
        /// <returns></returns>
        public string GetParentId()
        {
          return Parent?.Id ?? Element.ParentId;
        }

        public bool IsOpen()
        {
          return IsOpen(this);
        }

        private bool IsOpen(Instance instance)
        {
          if (instance == null)
            return false;

          if (instance.GetType() != typeof(RootElement.Instance))
            return IsOpen(instance.Element.Parent.GetInstance<Instance>(Player));

          var root = (RootElement.Instance)instance;
          return root.Open;
        }

        /// <summary>
        /// Build this element instance and its children.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="updatedElements"></param>
        /// <param name="parentHasUpdates"></param>
        /// <exception cref="Exception"></exception>
        public void Build(List<Instance> updatedElements, bool parentHasUpdates)
        {
          BuildingChildIndex = 0;
          var hasUpdates = Render() || parentHasUpdates || !IsOpen();

          if (hasUpdates)
            updatedElements.Add(this);

#if DEBUG
          if (Initialized && Visible && BuildingChildIndex != Children.Count)
            throw new Exception($"[{GetType().FullName}] Different number of children after update ({Children.Count} => {BuildingChildIndex}).");
#endif

          if (Visible)
            foreach (var child in Children)
              child.Build(Player, updatedElements, hasUpdates);

          Initialized = true;
        }

        /// <summary>
        /// Render this element.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public abstract bool Render();

        /// <summary>
        /// Clear any internally stored data.
        /// </summary>
        public void Clear()
        {
          foreach (var child in Children)
            child.Clear(Player);
        }

        /// <summary>
        /// Add a child to this element.
        /// If the element has already been initialized, use the other version of this function.
        /// </summary>
        /// <param name="element"></param>
        protected T AddChild<T>(T element) where T : Element
        {
#if DEBUG
          if (Initialized)
            throw new Exception("Cannot add child once initialized.");
          if (element == null)
            throw new Exception("Cannot add null as child.");
#endif

          Children.Add(element);
          return element;
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
        /// Get all the CuiElements for this element.
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<CuiElement> GetCuiElements();
      }

      public class BoundingBox
      {
        private readonly Instance Parent;

        public double MinX { get; set; } = 0;
        public double MinY { get; set; } = 0;
        public double MaxX { get; set; } = 1;
        public double MaxY { get; set; } = 1;

        public BoundingBox(Instance parent)
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
          return new CuiRectTransformComponent()
          {
            AnchorMin = $"{MinX} {MinY}",
            AnchorMax = $"{MaxX} {MaxY}",
          };
        }
      }
    }

    /// <summary>
    /// The top level element.
    /// </summary>
    public class RootElement : PanelElement
    {
      public delegate bool RenderUi(Instance panel, BasePlayer player);

      protected internal RenderUi Renderer;

      internal RootElement(string parentId, RenderUi renderer) : base(parentId)
      {
        Renderer = renderer;
      }

      public Instance GetOrCreateInstance(BasePlayer player)
      {
        return GetInstance<Instance>(player) ?? new Instance(this, player);
      }

      /// <summary>
      /// Build this element for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="updatedElements"></param>
      /// <param name="parentHasUpdates"></param>
      /// <exception cref="Exception"></exception>
      public override void Build(BasePlayer player, List<Element.Instance> updatedElements, bool parentHasUpdates)
      {
        InstanceCache[player.userID] = GetOrCreateInstance(player);
        InstanceCache[player.userID].Build(updatedElements, parentHasUpdates);
      }

      public new class Instance : PanelElement.Instance
      {
        internal bool Open = false;

        internal Instance(Element element, BasePlayer player) : base(element, player, null)
        {
        }

        public override bool Render()
        {
          var renderer = ((RootElement)Element).Renderer;
          return renderer(this, Player);
        }
      }
    }

    /// <summary>
    /// A Panel.
    /// </summary>
    public class PanelElement : Element
    {
      internal PanelElement(Element parent) : base(parent)
      {
      }

      internal PanelElement(string parentId) : base(parentId)
      {
      }

      public Instance GetOrCreateInstance(BasePlayer player, RenderUi<Instance> renderer)
      {
        var instance = GetInstance<Instance>(player);
        if (instance == null)
          instance = new Instance(this, player, renderer);
        else
          instance.Renderer = renderer;

        InstanceCache[player.userID] = instance;
        return instance;
      }

      /// <summary>
      /// Build this element for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="updatedElements"></param>
      /// <param name="parentHasUpdates"></param>
      /// <exception cref="Exception"></exception>
      public override void Build(BasePlayer player, List<Element.Instance> updatedElements, bool parentHasUpdates)
      {
        InstanceCache[player.userID].Build(updatedElements, parentHasUpdates);
      }

      public new class Instance : Element.Instance
      {
        public readonly CuiImageComponent Image = new CuiImageComponent();
        public bool CursorEnabled = false;
        public bool KeyboardEnabled = false;

        protected internal RenderUi<Instance> Renderer { get; set; }

        internal Instance(Element element, BasePlayer player, RenderUi<Instance> renderer) : base(element, player)
        {
          Renderer = renderer;
        }

        public override bool Render()
        {
          return Renderer(this);
        }

        public override IEnumerable<CuiElement> GetCuiElements()
        {
          CuiElement cuiElement = new CuiElement()
          {
            Name = Id,
            Parent = GetParentId(),
          };

          cuiElement.Components.Add(Image);
          cuiElement.Components.Add(Bounds.GetCuiComponent());

          if (CursorEnabled)
            cuiElement.Components.Add(new CuiNeedsCursorComponent());

          if (KeyboardEnabled)
            cuiElement.Components.Add(new CuiNeedsKeyboardComponent());

          return new CuiElement[] { cuiElement };
        }

        public void AddPanel(RenderUi<Instance> renderer)
        {
          PanelElement element = Initialized ? AddChild<PanelElement>() : AddChild(new PanelElement(Element));
          element.GetOrCreateInstance(Player, renderer);
        }

        public void AddLabel(RenderUi<LabelElement.Instance> renderer)
        {
          LabelElement element = Initialized ? AddChild<LabelElement>() : AddChild(new LabelElement(Element));
          element.GetOrCreateInstance(Player, renderer);
        }

        public void AddButton(RenderUi<ButtonElement.Instance> renderer)
        {
          ButtonElement element = Initialized ? AddChild<ButtonElement>() : AddChild(new ButtonElement(Element));
          element.GetOrCreateInstance(Player, renderer);
        }

        public void AddGameImage(RenderUi<GameImageElement.Instance> renderer)
        {
          GameImageElement element = Initialized ? AddChild<GameImageElement>() : AddChild(new GameImageElement(Element));
          element.GetOrCreateInstance(Player, renderer);
        }

        public void AddRawImage(RenderUi<RawImageElement.Instance> renderer)
        {
          RawImageElement element = Initialized ? AddChild<RawImageElement>() : AddChild(new RawImageElement(Element));
          element.GetOrCreateInstance(Player, renderer);
        }

        public void AddTabs(RenderUi<TabsElement.Instance> renderer)
        {
          TabsElement element = Initialized ? AddChild<TabsElement>() : AddChild(new TabsElement(Element));
          element.GetOrCreateInstance(Player, renderer);
        }

        public void AddGrid(RenderUi<GridElement.Instance> renderer)
        {
          GridElement element = Initialized ? AddChild<GridElement>() : AddChild(new GridElement(Element));
          element.GetOrCreateInstance(Player, renderer);
        }
      }
    }

    /// <summary>
    /// A Label
    /// </summary>
    public class LabelElement : Element
    {
      internal LabelElement(Element parent) : base(parent)
      {
      }

      public Instance GetOrCreateInstance(BasePlayer player, RenderUi<Instance> renderer)
      {
        var instance = GetInstance<Instance>(player);
        if (instance == null)
          instance = new Instance(this, player, renderer);
        else
          instance.Renderer = renderer;

        InstanceCache[player.userID] = instance;
        return instance;
      }

      /// <summary>
      /// Build this element for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="updatedElements"></param>
      /// <param name="parentHasUpdates"></param>
      /// <exception cref="Exception"></exception>
      public override void Build(BasePlayer player, List<Element.Instance> updatedElements, bool parentHasUpdates)
      {
        InstanceCache[player.userID].Build(updatedElements, parentHasUpdates);
      }

      public new class Instance : Element.Instance
      {
        public readonly CuiTextComponent Text = new CuiTextComponent();

        protected internal RenderUi<Instance> Renderer { get; set; }

        internal Instance(Element element, BasePlayer player, RenderUi<Instance> renderer) : base(element, player)
        {
          Renderer = renderer;
        }

        public override bool Render()
        {
          return Renderer(this);
        }

        public override IEnumerable<CuiElement> GetCuiElements()
        {
          CuiElement cuiElement = new CuiElement()
          {
            Name = Id,
            Parent = GetParentId(),
          };

          if (!string.IsNullOrEmpty(Text?.Text))
            cuiElement.Components.Add(Text);

          cuiElement.Components.Add(Bounds.GetCuiComponent());

          return new CuiElement[] { cuiElement };
        }
      }
    }

    /// <summary>
    /// A Text Button
    /// </summary>
    public class ButtonElement : Element
    {
      internal ButtonElement(Element parent) : base(parent)
      {
      }

      public Instance GetOrCreateInstance(BasePlayer player, RenderUi<Instance> renderer)
      {
        var instance = GetInstance<Instance>(player);
        if (instance == null)
          instance = new Instance(this, player, renderer);
        else
          instance.Renderer = renderer;

        InstanceCache[player.userID] = instance;
        return instance;
      }

      /// <summary>
      /// Build this element for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="updatedElements"></param>
      /// <param name="parentHasUpdates"></param>
      /// <exception cref="Exception"></exception>
      public override void Build(BasePlayer player, List<Element.Instance> updatedElements, bool parentHasUpdates)
      {
        InstanceCache[player.userID].Build(updatedElements, parentHasUpdates);
      }

      public new class Instance : Element.Instance
      {
        public readonly CuiButtonComponent Button = new CuiButtonComponent();
        public readonly CuiTextComponent Text = new CuiTextComponent();

        protected internal RenderUi<Instance> Renderer { get; set; }

        internal Instance(Element element, BasePlayer player, RenderUi<Instance> renderer) : base(element, player)
        {
          Renderer = renderer;
        }

        public override bool Render()
        {
          return Renderer(this);
        }

        public override IEnumerable<CuiElement> GetCuiElements()
        {
          var cuiElements = new List<CuiElement>
          {
            new CuiElement()
            {
              Name = Id,
              Parent = GetParentId(),
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
    }

    /// <summary>
    /// A raw image.
    /// </summary>
    public class RawImageElement : Element
    {
      internal RawImageElement(Element parent) : base(parent)
      {
      }

      public Instance GetOrCreateInstance(BasePlayer player, RenderUi<Instance> renderer)
      {
        var instance = GetInstance<Instance>(player);
        if (instance == null)
          instance = new Instance(this, player, renderer);
        else
          instance.Renderer = renderer;

        InstanceCache[player.userID] = instance;
        return instance;
      }

      /// <summary>
      /// Build this element for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="updatedElements"></param>
      /// <param name="parentHasUpdates"></param>
      /// <exception cref="Exception"></exception>
      public override void Build(BasePlayer player, List<Element.Instance> updatedElements, bool parentHasUpdates)
      {
        InstanceCache[player.userID].Build(updatedElements, parentHasUpdates);
      }

      public new class Instance : Element.Instance
      {
        public readonly CuiRawImageComponent Image = new CuiRawImageComponent();

        protected internal RenderUi<Instance> Renderer { get; set; }

        internal Instance(Element element, BasePlayer player, RenderUi<Instance> renderer) : base(element, player)
        {
          Renderer = renderer;
        }

        public override bool Render()
        {
          return Renderer(this);
        }

        public override IEnumerable<CuiElement> GetCuiElements()
        {
          CuiElement cuiElement = new CuiElement()
          {
            Name = Id,
            Parent = GetParentId(),
            Components = {
              Image,
              Bounds.GetCuiComponent()
            }
          };

          return new CuiElement[] { cuiElement };
        }
      }
    }

    /// <summary>
    /// An game image.
    /// </summary>
    public class GameImageElement : Element
    {
      internal GameImageElement(Element parent) : base(parent)
      {
      }

      public Instance GetOrCreateInstance(BasePlayer player, RenderUi<Instance> renderer)
      {
        var instance = GetInstance<Instance>(player);
        if (instance == null)
          instance = new Instance(this, player, renderer);
        else
          instance.Renderer = renderer;

        InstanceCache[player.userID] = instance;
        return instance;
      }

      /// <summary>
      /// Build this element for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="updatedElements"></param>
      /// <param name="parentHasUpdates"></param>
      /// <exception cref="Exception"></exception>
      public override void Build(BasePlayer player, List<Element.Instance> updatedElements, bool parentHasUpdates)
      {
        InstanceCache[player.userID].Build(updatedElements, parentHasUpdates);
      }

      public new class Instance : Element.Instance
      {
        public readonly CuiImageComponent Image = new CuiImageComponent();
        protected internal RenderUi<Instance> Renderer { get; set; }

        internal Instance(Element element, BasePlayer player, RenderUi<Instance> renderer) : base(element, player)
        {
          Renderer = renderer;
        }

        public override bool Render()
        {
          return Renderer(this);
        }

        public override IEnumerable<CuiElement> GetCuiElements()
        {
          CuiElement cuiElement = new CuiElement()
          {
            Name = Id,
            Parent = GetParentId(),
            Components = {
              Image,
              Bounds.GetCuiComponent()
            }
          };

          return new CuiElement[] { cuiElement };
        }
      }
    }

    /// <summary>
    /// Text Tabs
    /// </summary>
    public class TabsElement : PanelElement
    {
      internal TabsElement(Element parent) : base(parent)
      {
      }

      public Instance GetOrCreateInstance(BasePlayer player, RenderUi<Instance> renderer)
      {
        var instance = GetInstance<Instance>(player);
        if (instance == null)
          instance = new Instance(this, player, renderer);
        else
          instance.Renderer = renderer;

        InstanceCache[player.userID] = instance;
        return instance;
      }

      public new class Instance : PanelElement.Instance
      {
        public bool Vertical = false;
        public double Gap = 0;
        public double MaxButtonSize = 0;

        private int _BuildingTabIndex = 0;
        private int TabsCount = 0;

        protected internal new RenderUi<Instance> Renderer { get; set; }

        internal Instance(Element element, BasePlayer player, RenderUi<Instance> renderer) : base(element, player, null)
        {
          Renderer = renderer;
        }

        public override bool Render()
        {
          _BuildingTabIndex = 0;
          return Renderer(this);
        }

        public void AddTab(RenderUi<ButtonElement.Instance> renderer)
        {
          if (!Initialized)
          {
            TabsCount++;
          }

          AddButton((button) =>
          {
            SetTabDefaults(button);
            _BuildingTabIndex++;
            return renderer(button);
          });
        }

        private void SetTabDefaults(ButtonElement.Instance button)
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
    }

    /// <summary>
    /// A Grid of panels
    /// </summary>
    public class GridElement : PanelElement
    {
      internal GridElement(Element parent) : base(parent)
      {
      }

      public Instance GetOrCreateInstance(BasePlayer player, RenderUi<Instance> renderer)
      {
        var instance = GetInstance<Instance>(player);
        if (instance == null)
          instance = new Instance(this, player, renderer);
        else
          instance.Renderer = renderer;

        InstanceCache[player.userID] = instance;
        return instance;
      }

      public new class Instance : PanelElement.Instance
      {
        public double GapX = 0;
        public double GapY = 0;

        public int Rows = 2;
        public int Columns = 2;

        private int _BuildingCellIndex = 0;

        protected internal new RenderUi<Instance> Renderer { get; set; }

        internal Instance(Element element, BasePlayer player, RenderUi<Instance> renderer) : base(element, player, null)
        {
          Renderer = renderer;
        }

        public override bool Render()
        {
          _BuildingCellIndex = 0;
          return Renderer(this);
        }

        public void AddCell(RenderUi<PanelElement.Instance> renderer)
        {
          AddPanel((cell) =>
          {
            SetCellDefaults(cell);
            _BuildingCellIndex++;
            return renderer(cell);
          });
        }

        private void SetCellDefaults(PanelElement.Instance cell)
        {
          var column = _BuildingCellIndex % Columns;
          var width = 1.0 / Columns - GapX * (Columns - 1) / Columns;
          var offsetX = (double)column / Columns * (1 + GapX);

          var row = _BuildingCellIndex / Columns;
          var height = 1.0 / Rows - GapY * (Rows - 1) / Rows;
          var offsetY = (double)row / Rows * (1 + GapY);

          cell.Bounds.MinX = offsetX;
          cell.Bounds.MaxX = offsetX + width;
          cell.Bounds.MinY = 1 - offsetY - height;
          cell.Bounds.MaxY = 1 - offsetY;
        }
      }
    }

    #endregion API classes
  }
}
