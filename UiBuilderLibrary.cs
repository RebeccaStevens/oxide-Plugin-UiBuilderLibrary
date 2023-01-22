using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Oxide.Plugins
{
  [Info("UI Builder Library", "BlueBeka", "0.0.3")]
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
        Interface.Oxide.LogWarning("Self already defined.");
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
        ui.Close((BasePlayer)player.Object, false);
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
    /// <param name="store">If the player doesn't already have any data, store a new copy.</param>
    /// <returns></returns>
    private PluginData.Structure GetPlayerData(BasePlayer player, bool store = true)
    {
      if (data.PlayerData.ContainsKey(player.userID))
        return data.PlayerData[player.userID];

      var playerData = new PluginData.Structure()
      {
        RenderScale = config.data.DefaultRenderScale,
        ScreenAspectRatio = config.data.DefaultScreenAspectRatio,
      };
      if (store)
        data.PlayerData[player.userID] = playerData;
      return playerData;
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
      var playerData = GetPlayerData(player, false);
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
      var playerData = GetPlayerData(player, false);
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
      public UI(string parentId, Func<RootElement.Instance, bool> rootBuilder)
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
          UI ui = uiRef.Target;
          if (ui != null)
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
        Root.Open(player, updatedElements, false);

        var rootIds = GetRootElementIds(updatedElements);
        var jsonObjects = updatedElements.SelectMany(element => ToJson(element, rootIds)).ToArray();
        if (jsonObjects.Length == 0)
        {
#if DEBUG
          Interface.Oxide.LogDebug("Nothing to display/remove.");
#endif
          return;
        }

        var json = $"[{string.Join(",", jsonObjects)}]";
        CuiHelper.AddUi(player, json);
      }

      /// <summary>
      /// Close this UI for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="sendCommand">Send a command to the client to close the UI.</param>
      public void Close(BasePlayer player, bool sendCommand = true)
      {
        var id = Root.Close(player);

        if (!sendCommand)
          return;

        CuiHelper.DestroyUi(player, id);
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
      private static HashSet<string> GetRootElementIds(IEnumerable<Element.Instance> elements)
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
      protected readonly HardWeakValueDictionary<ulong, Instance> InstanceCache = new HardWeakValueDictionary<ulong, Instance>();

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
        return InstanceCache.TryGetValueAndMakeHard(player.userID, out instance) ? (T)instance : null;
      }

      /// <summary>
      /// Open this element for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="updatedElements"></param>
      /// <param name="parentHasUpdates"></param>
      /// <returns>The id of the element instance that was opened.</returns>
      public abstract string Open(BasePlayer player, List<Instance> updatedElements, bool parentHasUpdates);

      /// <summary>
      /// Close this element for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <returns>The id of the element instance that was closed.</returns>
      public string Close(BasePlayer player)
      {
        if (!InstanceCache.ContainsKey(player.userID))
          return null;

        var instance = InstanceCache.GetValueAndMakeWeak(player.userID); ;
        instance.Close();
        return instance.Id;
      }

      /// <summary>
      /// An instance of the element for an indiviual player.
      /// </summary>
      public abstract class Instance
      {
        public string Id { get; }

        protected readonly Element Element;
        protected readonly Instance Parent;
        public BasePlayer Player { get; private set; }
        public BoundingBox Bounds { get; private set; }
        public bool Visible { get; set; } = true;

        protected readonly List<Element> Children = new List<Element>();
        private int BuildingChildIndex = 0;
        internal bool IsOpen = false;
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

        /// <summary>
        /// Open this element instance and its children.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="updatedElements"></param>
        /// <param name="parentHasUpdates"></param>
        /// <exception cref="Exception"></exception>
        public void Open(List<Instance> updatedElements, bool parentHasUpdates)
        {
          BuildingChildIndex = 0;
          var hasUpdates = Render() || parentHasUpdates || !IsOpen;

          if (hasUpdates)
            updatedElements.Add(this);

#if DEBUG
          if (Initialized && Visible && BuildingChildIndex != Children.Count)
            throw new Exception($"[{GetType().FullName}] Different number of children after update ({Children.Count} => {BuildingChildIndex}).");
#endif

          if (Visible)
            foreach (var child in Children)
              child.Open(Player, updatedElements, hasUpdates);

          IsOpen = true;
          Initialized = true;
        }

        /// <summary>
        /// Render this element.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public abstract bool Render();

        /// <summary>
        /// Mark this instance as closed Clear any internally stored data.
        /// </summary>
        public void Close()
        {
          IsOpen = false;
          foreach (var child in Children)
            child.Close(Player);
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
      protected internal Func<Instance, bool> Renderer;

      internal RootElement(string parentId, Func<Instance, bool> renderer) : base(parentId)
      {
        Renderer = renderer;
      }

      public Instance EnsureInstance(BasePlayer player)
      {
        var instance = GetInstance<Instance>(player);
        if (instance != null)
          return instance;

        instance = new Instance(this, player);
        InstanceCache.Add(player.userID, instance, true);
        return instance;
      }

      /// <summary>
      /// Open this element for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="updatedElements"></param>
      /// <param name="parentHasUpdates"></param>
      /// <returns>The id of the element instance that was opened.</returns>
      public override string Open(BasePlayer player, List<Element.Instance> updatedElements, bool parentHasUpdates)
      {
        var instance = EnsureInstance(player);
        instance.Open(updatedElements, parentHasUpdates);
        return instance.Id;
      }

      public new class Instance : PanelElement.Instance
      {
        internal Instance(Element element, BasePlayer player) : base(element, player, null)
        {
        }

        public override bool Render()
        {
          var renderer = ((RootElement)Element).Renderer;
          return renderer(this);
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

      public Instance EnsureInstance(BasePlayer player, Func<Instance, bool> renderer)
      {
        var instance = GetInstance<Instance>(player);
        if (instance != null)
        {
          instance.Renderer = renderer;
          return instance;
        }

        instance = new Instance(this, player, renderer);
        InstanceCache.Add(player.userID, instance, true);
        return instance;
      }

      /// <summary>
      /// Open this element for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="updatedElements"></param>
      /// <param name="parentHasUpdates"></param>
      /// <returns>The id of the element instance that was opened.</returns>
      public override string Open(BasePlayer player, List<Element.Instance> updatedElements, bool parentHasUpdates)
      {
        InstanceCache[player.userID].Open(updatedElements, parentHasUpdates);
        return InstanceCache[player.userID].Id;
      }

      public new class Instance : Element.Instance
      {
        public readonly CuiImageComponent Image = new CuiImageComponent();
        public bool CursorEnabled = false;
        public bool KeyboardEnabled = false;

        protected internal Func<Instance, bool> Renderer { get; set; }

        internal Instance(Element element, BasePlayer player, Func<Instance, bool> renderer) : base(element, player)
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

        public void AddPanel(Func<Instance, bool> renderer)
        {
          PanelElement element = Initialized ? AddChild<PanelElement>() : AddChild(new PanelElement(Element));
          element.EnsureInstance(Player, renderer);
        }

        public void AddLabel(Func<LabelElement.Instance, bool> renderer)
        {
          LabelElement element = Initialized ? AddChild<LabelElement>() : AddChild(new LabelElement(Element));
          element.EnsureInstance(Player, renderer);
        }

        public void AddButton(Func<ButtonElement.Instance, bool> renderer)
        {
          ButtonElement element = Initialized ? AddChild<ButtonElement>() : AddChild(new ButtonElement(Element));
          element.EnsureInstance(Player, renderer);
        }

        public void AddGameImage(Func<GameImageElement.Instance, bool> renderer)
        {
          GameImageElement element = Initialized ? AddChild<GameImageElement>() : AddChild(new GameImageElement(Element));
          element.EnsureInstance(Player, renderer);
        }

        public void AddRawImage(Func<RawImageElement.Instance, bool> renderer)
        {
          RawImageElement element = Initialized ? AddChild<RawImageElement>() : AddChild(new RawImageElement(Element));
          element.EnsureInstance(Player, renderer);
        }

        public void AddTabs(Func<TabsElement.Instance, bool> renderer)
        {
          TabsElement element = Initialized ? AddChild<TabsElement>() : AddChild(new TabsElement(Element));
          element.EnsureInstance(Player, renderer);
        }

        public void AddGrid(Func<GridElement.Instance, bool> renderer)
        {
          GridElement element = Initialized ? AddChild<GridElement>() : AddChild(new GridElement(Element));
          element.EnsureInstance(Player, renderer);
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

      public Instance EnsureInstance(BasePlayer player, Func<Instance, bool> renderer)
      {
        var instance = GetInstance<Instance>(player);
        if (instance != null)
        {
          instance.Renderer = renderer;
          return instance;
        }

        instance = new Instance(this, player, renderer);
        InstanceCache.Add(player.userID, instance, true);
        return instance;
      }

      /// <summary>
      /// Open this element for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="updatedElements"></param>
      /// <param name="parentHasUpdates"></param>
      /// <returns>The id of the element instance that was opened.</returns>
      public override string Open(BasePlayer player, List<Element.Instance> updatedElements, bool parentHasUpdates)
      {
        InstanceCache[player.userID].Open(updatedElements, parentHasUpdates);
        return InstanceCache[player.userID].Id;
      }

      public new class Instance : Element.Instance
      {
        public readonly CuiTextComponent Text = new CuiTextComponent();

        protected internal Func<Instance, bool> Renderer { get; set; }

        internal Instance(Element element, BasePlayer player, Func<Instance, bool> renderer) : base(element, player)
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

      public Instance EnsureInstance(BasePlayer player, Func<Instance, bool> renderer)
      {
        var instance = GetInstance<Instance>(player);
        if (instance != null)
        {
          instance.Renderer = renderer;
          return instance;
        }

        instance = new Instance(this, player, renderer);
        InstanceCache.Add(player.userID, instance, true);
        return instance;
      }

      /// <summary>
      /// Open this element for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="updatedElements"></param>
      /// <param name="parentHasUpdates"></param>
      /// <returns>The id of the element instance that was opened.</returns>
      public override string Open(BasePlayer player, List<Element.Instance> updatedElements, bool parentHasUpdates)
      {
        InstanceCache[player.userID].Open(updatedElements, parentHasUpdates);
        return InstanceCache[player.userID].Id;
      }

      public new class Instance : Element.Instance
      {
        public readonly CuiButtonComponent Button = new CuiButtonComponent();
        public readonly CuiTextComponent Text = new CuiTextComponent();

        protected internal Func<Instance, bool> Renderer { get; set; }

        internal Instance(Element element, BasePlayer player, Func<Instance, bool> renderer) : base(element, player)
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

      public Instance EnsureInstance(BasePlayer player, Func<Instance, bool> renderer)
      {
        var instance = GetInstance<Instance>(player);
        if (instance != null)
        {
          instance.Renderer = renderer;
          return instance;
        }

        instance = new Instance(this, player, renderer);
        InstanceCache.Add(player.userID, instance, true);
        return instance;
      }

      /// <summary>
      /// Open this element for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="updatedElements"></param>
      /// <param name="parentHasUpdates"></param>
      /// <returns>The id of the element instance that was opened.</returns>
      public override string Open(BasePlayer player, List<Element.Instance> updatedElements, bool parentHasUpdates)
      {
        InstanceCache[player.userID].Open(updatedElements, parentHasUpdates);
        return InstanceCache[player.userID].Id;
      }

      public new class Instance : Element.Instance
      {
        public readonly CuiRawImageComponent Image = new CuiRawImageComponent();

        protected internal Func<Instance, bool> Renderer { get; set; }

        internal Instance(Element element, BasePlayer player, Func<Instance, bool> renderer) : base(element, player)
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

      public Instance EnsureInstance(BasePlayer player, Func<Instance, bool> renderer)
      {
        var instance = GetInstance<Instance>(player);
        if (instance != null)
        {
          instance.Renderer = renderer;
          return instance;
        }

        instance = new Instance(this, player, renderer);
        InstanceCache.Add(player.userID, instance, true);
        return instance;
      }

      /// <summary>
      /// Open this element for the given player.
      /// </summary>
      /// <param name="player"></param>
      /// <param name="updatedElements"></param>
      /// <param name="parentHasUpdates"></param>
      /// <returns>The id of the element instance that was opened.</returns>
      public override string Open(BasePlayer player, List<Element.Instance> updatedElements, bool parentHasUpdates)
      {
        InstanceCache[player.userID].Open(updatedElements, parentHasUpdates);
        return InstanceCache[player.userID].Id;
      }

      public new class Instance : Element.Instance
      {
        public readonly CuiImageComponent Image = new CuiImageComponent();
        protected internal Func<Instance, bool> Renderer { get; set; }

        internal Instance(Element element, BasePlayer player, Func<Instance, bool> renderer) : base(element, player)
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

      public Instance EnsureInstance(BasePlayer player, Func<Instance, bool> renderer)
      {
        var instance = GetInstance<Instance>(player);
        if (instance != null)
        {
          instance.Renderer = renderer;
          return instance;
        }

        instance = new Instance(this, player, renderer);
        InstanceCache.Add(player.userID, instance, true);
        return instance;
      }

      public new class Instance : PanelElement.Instance
      {
        public bool Vertical = false;
        public double Gap = 0;
        public double MaxButtonSize = 0;

        private int _BuildingTabIndex = 0;
        private int TabsCount = 0;

        protected internal new Func<Instance, bool> Renderer { get; set; }

        internal Instance(Element element, BasePlayer player, Func<Instance, bool> renderer) : base(element, player, null)
        {
          Renderer = renderer;
        }

        public override bool Render()
        {
          _BuildingTabIndex = 0;
          return Renderer(this);
        }

        public void AddTab(Func<ButtonElement.Instance, bool> renderer)
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

      public Instance EnsureInstance(BasePlayer player, Func<Instance, bool> renderer)
      {
        var instance = GetInstance<Instance>(player);
        if (instance != null)
        {
          instance.Renderer = renderer;
          return instance;
        }

        instance = new Instance(this, player, renderer);
        InstanceCache.Add(player.userID, instance, true);
        return instance;
      }

      public new class Instance : PanelElement.Instance
      {
        public double GapX = 0;
        public double GapY = 0;

        public int Rows = 2;
        public int Columns = 2;

        private int _BuildingCellIndex = 0;

        protected internal new Func<Instance, bool> Renderer { get; set; }

        internal Instance(Element element, BasePlayer player, Func<Instance, bool> renderer) : base(element, player, null)
        {
          Renderer = renderer;
        }

        public override bool Render()
        {
          _BuildingCellIndex = 0;
          return Renderer(this);
        }

        public void AddCell(Func<PanelElement.Instance, bool> renderer)
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

    #region Collections

    /// <summary>
    /// A dictionary in which the values can be marked as weak references.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class HardWeakValueDictionary<TKey, TValue> where TValue : class // : IDictionary<TKey, TValue>
    {
      private Dictionary<TKey, HardWeakReference<TValue>> _dict = new Dictionary<TKey, HardWeakReference<TValue>>();
      private int _version, _cleanVersion;
      private int _cleanGeneration;
      private const int MinRehashInterval = 500;

      public ICollection<TKey> Keys => _dict.Keys;

      public ICollection<TValue> Values
      { get { throw new NotImplementedException(); } }

      public int Count => _dict.Count;

      public bool IsReadOnly => false;

      public TValue this[TKey key]
      {
        get
        {
#if DEBUG
          if (!_dict[key].IsHard)
            throw new Exception("Do not access via index when weak.");
#endif
          return _dict[key].GetTarget();
        }
        set
        {
          _dict[key] = new HardWeakReference<TValue>(value);
        }
      }

      public HardWeakValueDictionary()
      {
      }

      public void Add(TKey key, TValue value)
      {
        Add(key, value, true);
      }

      public void Add(TKey key, TValue value, bool hard)
      {
        AutoCleanup(2);

        HardWeakReference<TValue> reference;
        if (_dict.TryGetValue(key, out reference))
        {
          if (reference.IsAlive)
            throw new ArgumentException("An element with the same key already exists in this dictionary");

          reference.SetTarget(value);
          if (hard)
            reference.MakeHard();

          return;
        }

        _dict.Add(key, new HardWeakReference<TValue>(value, hard));
      }

      public bool MakeWeak(TKey key)
      {
        AutoCleanup(1);

        HardWeakReference<TValue> reference;
        if (!_dict.TryGetValue(key, out reference))
          return false;
        return reference.MakeWeak();
      }

      public bool MakeHard(TKey key)
      {
        AutoCleanup(1);

        HardWeakReference<TValue> reference;
        if (!_dict.TryGetValue(key, out reference))
          return false;
        return reference.MakeHard();
      }

      public bool ContainsKey(TKey key)
      {
        AutoCleanup(1);

        HardWeakReference<TValue> value;
        if (!_dict.TryGetValue(key, out value))
          return false;
        return value.IsAlive;
      }

      public bool Remove(TKey key)
      {
        AutoCleanup(1);

        HardWeakReference<TValue> reference;
        if (!_dict.TryGetValue(key, out reference))
          return false;
        _dict.Remove(key);
        return reference.IsAlive;
      }

      public bool TryGetValue(TKey key, out TValue value)
      {
        AutoCleanup(1);

        HardWeakReference<TValue> reference;
        if (_dict.TryGetValue(key, out reference))
          value = reference.GetTarget();
        else
          value = null;
        return value != null;
      }

      public bool TryGetValueAndMakeHard(TKey key, out TValue value)
      {
        AutoCleanup(1);

        HardWeakReference<TValue> reference;
        if (_dict.TryGetValue(key, out reference))
          value = reference.GetTarget();
        else
          value = null;
        if (value == null)
          return false;
        return reference.MakeHard();
      }

      public TValue GetValueAndMakeWeak(TKey key)
      {
        var reference = _dict[key];
        var value = reference.GetTarget();
        reference.MakeWeak();
        return value;
      }

      public void Add(KeyValuePair<TKey, TValue> item)
      {
        Add(item.Key, item.Value);
      }

      public void Clear()
      {
        _dict.Clear();
        _version = _cleanVersion = 0;
        _cleanGeneration = 0;
      }

      public bool Contains(KeyValuePair<TKey, TValue> item)
      {
        throw new NotImplementedException();
      }

      public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
      {
        throw new NotImplementedException();
      }

      public bool Remove(KeyValuePair<TKey, TValue> item)
      {
        throw new NotImplementedException();
      }

      public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
      {
        int nullCount = 0;

        foreach (KeyValuePair<TKey, HardWeakReference<TValue>> kvp in _dict)
        {
          TValue target = kvp.Value.GetTarget();
          if (target == null)
            nullCount++;
          else
            yield return new KeyValuePair<TKey, TValue>(kvp.Key, target);
        }

        if (nullCount > _dict.Count / 4)
          Cleanup();
      }

      //IEnumerator IEnumerable.GetEnumerator()
      //{
      //  return GetEnumerator();
      //}

      private void AutoCleanup(int incVersion)
      {
        _version += incVersion;

        // Cleanup the table every so often--less often for larger tables.
        long delta = _version - _cleanVersion;
        if (delta > MinRehashInterval + _dict.Count)
        {
          // A cleanup will be fruitless unless a GC has happened in the meantime.
          // WeakReferences can become zero only during the GC.
          int curGeneration = GC.CollectionCount(0);
          if (_cleanGeneration != curGeneration)
          {
            _cleanGeneration = curGeneration;
            Cleanup();
            _cleanVersion = _version;
          }
          else
            _cleanVersion += MinRehashInterval; // Wait a little while longer
        }
      }

      private void Cleanup()
      {
        // Remove all pairs whose value is nullified.
        // Due to the fact that you can't change a Dictionary while enumerating
        // it, we need an intermediate collection (the list of things to delete):
        List<TKey> deadKeys = new List<TKey>();

        foreach (KeyValuePair<TKey, HardWeakReference<TValue>> kvp in _dict)
          if (!kvp.Value.IsAlive)
            deadKeys.Add(kvp.Key);

        foreach (TKey key in deadKeys)
        {
          _dict.Remove(key);
        }
      }
    }

    #endregion Collections

    #region References

    /// <summary>
    /// A weak reference to an object of type `T`.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WeakReference<T> : System.WeakReference
    {
      public WeakReference(T target) : base(target)
      {
      }

      public WeakReference(T target, bool trackResurrection) : base(target, trackResurrection)
      {
      }

      protected WeakReference(SerializationInfo info, StreamingContext context) : base(info, context)
      {
      }

      public new T Target
      {
        get { return (T)base.Target; }
        set { base.Target = value; }
      }
    }

    /// <summary>
    /// A reference to an object of type `T` that can be toggled between a hard and weak reference.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class HardWeakReference<T> where T : class
    {
      public T Hard { get; private set; }
      public WeakReference<T> Weak { get; private set; }

      public bool IsHard
      { get { return Hard != null; } }

      public bool IsAlive
      { get { return IsHard || Weak.IsAlive; } }

      public HardWeakReference(T reference, bool hard = true)
      {
        if (hard)
          Hard = reference;
        Weak = new WeakReference<T>(reference);
      }

      public T GetTarget()
      {
        if (IsHard)
          return Hard;
        return Weak.Target;
      }

      public void SetTarget(T target)
      {
        Weak.Target = target;
        if (IsHard)
        {
          Hard = target;
        }
      }

      public bool MakeHard()
      {
        if (IsHard)
          return true;
        Hard = Weak.Target;
        return IsAlive;
      }

      public bool MakeWeak()
      {
        Hard = null;
        return IsAlive;
      }
    }

    #endregion References
  }
}
