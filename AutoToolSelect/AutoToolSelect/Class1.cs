using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;
using StardewValley.Locations;
using System;
using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;
using StardewValley.Monsters;
using System.Collections.Generic;

namespace AutoToolSelect
{
    public class ModConfig
    {
        public SButton ActivationKey { get; set; } = SButton.LeftControl;
        public SButton ToggleKey { get; set; } = SButton.F5;
        public bool HoeSelect { get; set; } = true;
        public bool IfNoneToolChooseWeapon { get; set; } = true;
        public bool RideHorseCursor { get; set; } = true;
        public bool PickaxeOverWateringCan { get; set; } = true;
        public bool CursorOverToolHitLocation { get; set; } = false;
        public bool CheckWholeBackpack { get; set; } = false;
    }

    public interface IGenericModConfigMenuAPI
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);
        void AddKeybind(IManifest mod, Func<SButton> getValue, Action<SButton> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);
    }

    class AutoToolSelectMod : Mod
    {
        bool togglemod = false;
        bool buttonPressed = false;
        private ModConfig Config;
        Texture2D Lock;
        public override void Entry(IModHelper helper)
        {
            Lock = helper.ModContent.Load<Texture2D>("assets/Lock.png");
            Config = (ModConfig)helper.ReadConfig<ModConfig>();
            Helper.Events.Input.ButtonPressed += new EventHandler<ButtonPressedEventArgs>(ButtonPressed);
            Helper.Events.Input.ButtonReleased += new EventHandler<ButtonReleasedEventArgs>(ButtonReleased);
            Helper.Events.Display.RenderedHud += PostRenderHud;
            Helper.Events.GameLoop.GameLaunched += OnLaunched;
        }
        private void OnLaunched(object sender, GameLaunchedEventArgs e)
        {
            Config = Helper.ReadConfig<ModConfig>();
            var api = Helper.ModRegistry.GetApi<IGenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");
            api.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(Config)
            );

            api.AddKeybind(
                mod: this.ModManifest,
                name: () => "Activation Key",
                tooltip: () => "When hold, mod is activated",
                getValue: () => this.Config.ActivationKey,
                setValue: value => this.Config.ActivationKey = value
            );

            api.AddKeybind(
                mod: this.ModManifest,
                name: () => "Toggle Key",
                tooltip: () => "Press to turn on/off mod",
                getValue: () => this.Config.ToggleKey,
                setValue: value => this.Config.ToggleKey = value
            );

            api.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Hoe Select",
                tooltip: () => "Mod will select hoe when you target ground. I recommend not checking it",
                getValue: () => this.Config.HoeSelect,
                setValue: value => this.Config.HoeSelect = value
            );

            api.AddBoolOption(
                mod: this.ModManifest,
                name: () => "If None Tool Choose Weapon",
                tooltip: () => "Mod will select weapon/scythe when none tool was chosen. Works better with Hoe Select not checked",
                getValue: () => this.Config.IfNoneToolChooseWeapon,
                setValue: value => this.Config.IfNoneToolChooseWeapon = value
            );

            api.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Ride Horse Cursor",
                tooltip: () => "Mod will use cursor location to determine which tool should be chosen while riding horse (tractor) - for mouse and keyboard. If not checked mod will use tool hit location to - for controller",
                getValue: () => this.Config.RideHorseCursor,
                setValue: value => this.Config.RideHorseCursor = value
            );

            api.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Pickaxe Over Watering Can",
                tooltip: () => "Mod will select pickaxe on hoed dirt, instead of watering can. Can be used to collect clay. Works better with Hoe Select checked. I recommend not checking it",
                getValue: () => this.Config.PickaxeOverWateringCan,
                setValue: value => this.Config.PickaxeOverWateringCan = value
            );

            api.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Cursor Over Tool Hit Location",
                tooltip: () => "Switches to use cursor location to determine which tool should be chosen. Set it true if you use Ranged Tools or Combat Controls mods",
                getValue: () => this.Config.CursorOverToolHitLocation,
                setValue: value => this.Config.CursorOverToolHitLocation = value
            );

            api.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Check Whole Backpack",
                tooltip: () => "False - mod will check only toolbar for items to swap to. True - mod will check whole backpack instead",
                getValue: () => this.Config.CheckWholeBackpack,
                setValue: value => this.Config.CheckWholeBackpack = value
            );
        }

        private void ButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button == this.Config.ActivationKey && !togglemod)
            {
                Helper.Events.GameLoop.UpdateTicked += this.GameTicked;
                buttonPressed = true;
            }
            if (e.Button == this.Config.ToggleKey)
            {
                if (togglemod)
                {
                    togglemod = false;
                    Helper.Events.GameLoop.UpdateTicked -= this.GameTicked;
                }
                else
                {
                    togglemod = true;
                    Helper.Events.GameLoop.UpdateTicked += this.GameTicked;
                }
            }
        }

        private void ButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (e.Button == this.Config.ActivationKey && !togglemod)
            {
                Helper.Events.GameLoop.UpdateTicked -= this.GameTicked;
                buttonPressed = false;
            }
        }

        private void PostRenderHud(object sender, EventArgs e)
        {
            foreach (IClickableMenu menu in Game1.onScreenMenus)
            {
                if (menu is Toolbar && Game1.activeClickableMenu == null && (togglemod || buttonPressed))
                {
                    Vector2 position;
                    if (Game1.options.pinToolbarToggle || (double)Game1.GlobalToLocal(Game1.viewport, new Vector2((float)Game1.player.GetBoundingBox().Center.X, (float)Game1.player.GetBoundingBox().Center.Y)).Y <= (double)(Game1.viewport.Height / 2 + 64))
                    {
                        position = new Vector2((float)(Game1.uiViewport.Width / 2 - 384 + Game1.player.CurrentToolIndex * 64), (float)(Game1.uiViewport.Height - Utility.makeSafeMarginY(8) - 80));
                    }
                    else
                    {
                        position = new Vector2((float)(Game1.uiViewport.Width / 2 - 384 + Game1.player.CurrentToolIndex * 64), (float)(16 + Utility.makeSafeMarginY(8)));
                    }
                    Game1.spriteBatch.Draw(Lock, position, Color.White * 1f);
                }
            }
        }

        private void GameTicked(object sender, EventArgs e)
        {
            if (Context.IsWorldReady && Context.CanPlayerMove)
            {
                int range = Config.CheckWholeBackpack ? Game1.player.Items.Count : 12;
                Vector2 ToolLocationVector;
                if ((Game1.player.isRidingHorse() && Config.RideHorseCursor) || Config.CursorOverToolHitLocation)
                {
                    ToolLocationVector = Game1.currentCursorTile;
                }
                else
                {
                    ToolLocationVector = new Vector2((int)Game1.player.GetToolLocation().X / Game1.tileSize, (int)Game1.player.GetToolLocation().Y / Game1.tileSize);
                }
                Point ToolLocationPoint = new(((int)ToolLocationVector.X) * Game1.tileSize + Game1.tileSize / 2, ((int)ToolLocationVector.Y) * Game1.tileSize + Game1.tileSize / 2);
                Rectangle ToolRect = new(((int)ToolLocationVector.X) * Game1.tileSize, ((int)ToolLocationVector.Y) * Game1.tileSize, Game1.tileSize, Game1.tileSize);
                Rectangle PanRect = new(Game1.player.currentLocation.orePanPoint.X * 64 - 64, Game1.player.currentLocation.orePanPoint.Y * 64 - 64, 256, 256);
                if (this.Config.IfNoneToolChooseWeapon)
                {
                    SetWeapon(range);
                }
                if (Game1.player.currentLocation.doesTileHaveProperty((int)ToolLocationVector.X, (int)ToolLocationVector.Y, "Diggable", "Back") != null && this.Config.HoeSelect)
                {
                    SetTool(typeof(Hoe), range);
                }
                if (Game1.player.currentLocation.doesTileHaveProperty((int)ToolLocationVector.X, (int)ToolLocationVector.Y, "Water", "Back") != null && Game1.player.currentLocation is not VolcanoDungeon)
                {
                    SetTool(typeof(FishingRod), range);
                }
                if (Game1.player.currentLocation is AnimalHouse && Game1.player.currentLocation.doesTileHaveProperty((int)ToolLocationVector.X, (int)ToolLocationVector.Y, "Trough", "Back") != null && !(Game1.currentLocation as AnimalHouse).objects.ContainsKey(ToolLocationVector))
                {
                    SetItem("Hay", range);
                }
                if ((Game1.player.currentLocation is Farm || Game1.player.currentLocation.IsGreenhouse || (Game1.player.currentLocation is VolcanoDungeon && !(Game1.player.currentLocation as VolcanoDungeon).IsCooledLava((int)ToolLocationVector.X, (int)ToolLocationVector.Y))) && (Game1.player.currentLocation.doesTileHaveProperty((int)ToolLocationVector.X, (int)ToolLocationVector.Y, "Water", "Back") != null || Game1.player.currentLocation.doesTileHaveProperty((int)ToolLocationVector.X, (int)ToolLocationVector.Y, "WaterSource", "Back") != null || Game1.player.currentLocation.IsBuildableLocation() && (Game1.player.currentLocation).getBuildingAt(ToolLocationVector) != null && ((Game1.player.currentLocation.getBuildingAt(ToolLocationVector).buildingType.Equals("Well") && Game1.player.currentLocation.getBuildingAt(ToolLocationVector).daysOfConstructionLeft.Value <= 0) || Game1.player.currentLocation.getBuildingAt(ToolLocationVector).buildingType.Equals("Pet Bowl"))))
                {
                    SetTool(typeof(WateringCan), range);
                }
                if (PanRect.Contains(ToolLocationPoint) && (double)Utility.distance((float)Game1.player.StandingPixel.X, (float)PanRect.Center.X, (float)Game1.player.StandingPixel.Y, (float)PanRect.Center.Y) <= 192.0)
                {
                    SetTool(typeof(Pan), range);
                }
                if (Game1.player.currentLocation.objects.ContainsKey(ToolLocationVector))
                {
                    if (Game1.player.currentLocation.objects[ToolLocationVector].name.Equals("Artifact Spot") || Game1.player.currentLocation.objects[ToolLocationVector].name.Equals("Seed Spot"))
                    {
                        SetTool(typeof(Hoe), range);
                    }
                    if (Game1.player.currentLocation.objects[ToolLocationVector].IsBreakableStone())
                    {
                        SetTool(typeof(Pickaxe), range);
                    }
                    if (Game1.player.currentLocation.objects[ToolLocationVector].IsTwig())
                    {
                        SetTool(typeof(Axe), range);
                    }
                    if (Game1.player.currentLocation.objects[ToolLocationVector].IsWeeds() || Game1.player.currentLocation.objects[ToolLocationVector].name.Equals("Barrel"))
                    {
                        SetWeapon(range);
                    }
                    for (int i = 0; i < range; i++)
                    {
                        if (Game1.player.Items[i] != null && Game1.player.currentLocation.objects[ToolLocationVector].performObjectDropInAction(Game1.player.Items[i], true, Game1.player))
                        {
                            Game1.player.CurrentToolIndex = i % 12;
                            for (int j = 0; j < i / 12; j++)
                            {
                                ShiftToolbar(Game1.player);
                            }
                            break;
                        }
                    }
                }
                if (Game1.player.currentLocation.terrainFeatures.ContainsKey(ToolLocationVector))
                {
                    if (Game1.player.currentLocation.terrainFeatures[ToolLocationVector] is HoeDirt)
                    {
                        if ((Game1.player.currentLocation.terrainFeatures[ToolLocationVector] as HoeDirt).crop != null && (((Game1.player.currentLocation.terrainFeatures[ToolLocationVector] as HoeDirt).crop.GetHarvestMethod() == StardewValley.GameData.Crops.HarvestMethod.Scythe && (Game1.player.currentLocation.terrainFeatures[ToolLocationVector] as HoeDirt).crop.fullyGrown.Value) || (Game1.player.currentLocation.terrainFeatures[ToolLocationVector] as HoeDirt).crop.dead.Value))
                        {
                            SetScythe(range);
                        }
                        else if ((Game1.player.currentLocation.terrainFeatures[ToolLocationVector] as HoeDirt).crop != null && (Game1.player.currentLocation.terrainFeatures[ToolLocationVector] as HoeDirt).crop.whichForageCrop.Value == "2")
                        {
                            SetTool(typeof(Hoe), range);
                        }
                        else if (this.Config.PickaxeOverWateringCan)
                        {
                            SetTool(typeof(Pickaxe), range);
                        }
                        else
                        {
                            SetTool(typeof(WateringCan), range);
                        }
                    }
                    if (Game1.player.currentLocation.terrainFeatures[ToolLocationVector] is GiantCrop)
                    {
                        SetTool(typeof(Axe), range);
                    }
                    if (Game1.player.currentLocation.terrainFeatures[ToolLocationVector] is Tree)
                    {
                        if (Game1.player.currentLocation.getObjectAtTile((int)ToolLocationVector.X, (int)ToolLocationVector.Y) != null && Game1.player.currentLocation.getObjectAtTile((int)ToolLocationVector.X, (int)ToolLocationVector.Y).IsTapper())
                        {
                            SetScythe(range);
                        }
                        else
                        {
                            SetTool(typeof(Axe), range);
                        }
                    }
                    if (Game1.player.currentLocation.terrainFeatures[ToolLocationVector] is Grass)
                    {
                        SetWeapon(range);
                    }
                }
                foreach (FarmAnimal animal in Game1.player.currentLocation.animals.Values)
                {
                    if (animal.GetHarvestBoundingBox().Intersects(ToolRect) && animal.CanGetProduceWithTool(new Shears()) && animal.currentProduce.Value != null && animal.isAdult())
                    {
                        SetItem("Shears", range);
                    }
                    if (animal.GetHarvestBoundingBox().Intersects(ToolRect) && animal.CanGetProduceWithTool(new MilkPail()) && animal.currentProduce.Value != null && animal.isAdult())
                    {
                        SetItem("Milk Pail", range);
                    }
                }
                for (int i = Game1.player.currentLocation.resourceClumps.Count - 1; i >= 0; --i)
                {
                    if (Game1.player.currentLocation.resourceClumps[i].getBoundingBox().Contains(ToolLocationPoint))
                    {
                        if (Game1.player.currentLocation.resourceClumps[i].parentSheetIndex.Value == 600)
                        {
                            SetTool(typeof(Axe), range, 1);
                        }
                        if (Game1.player.currentLocation.resourceClumps[i].parentSheetIndex.Value == 602)
                        {
                            SetTool(typeof(Axe), range, 2);
                        }
                        if (Game1.player.currentLocation.resourceClumps[i].parentSheetIndex.Value == 148 || Game1.player.currentLocation.resourceClumps[i].parentSheetIndex.Value == 622)
                        {
                            SetTool(typeof(Pickaxe), range, 3);
                        }
                        if (Game1.player.currentLocation.resourceClumps[i].parentSheetIndex.Value == 672)
                        {
                            SetTool(typeof(Pickaxe), range, 2);
                        }
                        if (Game1.player.currentLocation.resourceClumps[i].parentSheetIndex.Value == 752 || Game1.player.currentLocation.resourceClumps[i].parentSheetIndex.Value == 754 || Game1.player.currentLocation.resourceClumps[i].parentSheetIndex.Value == 756 || Game1.player.currentLocation.resourceClumps[i].parentSheetIndex.Value == 758)
                        {
                            SetTool(typeof(Pickaxe), range);
                        }
                    }
                }
                if (Game1.player.currentLocation is MineShaft)
                {
                    foreach (Character monster in Game1.player.currentLocation.characters)
                    {
                        if (monster is Monster)
                        {
                            if (monster is RockCrab && !monster.Name.Equals("Stick Bug") && !monster.isMoving() && ToolRect.Contains(monster.Position))
                            {
                                SetTool(typeof(Pickaxe), range);
                            }
                            if (monster is RockCrab && monster.Name.Equals("Stick Bug") && !monster.isMoving() && ToolRect.Contains(monster.Position))
                            {
                                SetTool(typeof(Axe), range);
                            }
                        }
                    }
                }
            }
        }

        private static void SetTool(Type t, int range, int Level = 0)
        {
            for (int i = 0; i < range; i++)
            {
                if (Game1.player.Items[i] != null && Game1.player.Items[i].GetType() == t && (Game1.player.Items[i] as Tool).UpgradeLevel >= Level)
                {
                    Game1.player.CurrentToolIndex = i % 12;
                    for (int j = 0; j < i / 12; j++)
                    {
                        ShiftToolbar(Game1.player);
                    }
                    return;
                }
            }
        }
        private static void SetItem(string name, int range)
        {
            for (int i = 0; i < range; i++)
            {
                if (Game1.player.Items[i] != null && Game1.player.Items[i].Name.Equals(name))
                {
                    Game1.player.CurrentToolIndex = i % 12;
                    for (int j = 0; j < i / 12; j++)
                    {
                        ShiftToolbar(Game1.player);
                    }
                    return;
                }
            }
        }

        private static void SetScythe(int range)
        {
            for (int i = 0; i < range; i++)
            {
                if (Game1.player.Items[i] != null && Game1.player.Items[i].Name.Contains("Scythe"))
                {
                    Game1.player.CurrentToolIndex = i % 12;
                    for (int j = 0; j < i / 12; j++)
                    {
                        ShiftToolbar(Game1.player);
                    }
                    return;
                }
            }
            for (int i = 0; i < range; i++)
            {
                if (Game1.player.Items[i] is MeleeWeapon)
                {
                    Game1.player.CurrentToolIndex = i % 12;
                    for (int j = 0; j < i / 12; j++)
                    {
                        ShiftToolbar(Game1.player);
                    }
                    return;
                }
            }
        }

        private static void SetWeapon(int range)
        {
            if (Game1.player.currentLocation is Farm || Game1.player.currentLocation.IsGreenhouse)
            {
                SetScythe(range);
                return;
            }
            for (int i = 0; i < range; i++)
            {
                if (Game1.player.Items[i] is MeleeWeapon && !Game1.player.Items[i].Name.Contains("Scythe"))
                {
                    Game1.player.CurrentToolIndex = i % 12;
                    for (int j = 0; j < i / 12; j++)
                    {
                        ShiftToolbar(Game1.player);
                    }
                    return;
                }
            }
            for (int i = 0; i < range; i++)
            {
                if (Game1.player.Items[i] != null && Game1.player.Items[i].Name.Contains("Scythe"))
                {
                    Game1.player.CurrentToolIndex = i % 12;
                    for (int j = 0; j < i / 12; j++)
                    {
                        ShiftToolbar(Game1.player);
                    }
                    return;
                }
            }
        }
        private static void ShiftToolbar(Farmer player)
        {
            player.CurrentItem?.actionWhenStopBeingHeld(player);
            IList<Item> range = player.Items.GetRange(0, 12);
            player.Items.RemoveRange(0, 12);
            player.Items.AddRange(range);
            player.netItemStowed.Set(newValue: false);
            player.CurrentItem?.actionWhenBeingHeld(player);
            for (int j = 0; j < Game1.onScreenMenus.Count; j++)
            {
                if (Game1.onScreenMenus[j] is Toolbar toolbar)
                {
                    toolbar.shifted(true);
                    break;
                }
            }
        }
    }
}
