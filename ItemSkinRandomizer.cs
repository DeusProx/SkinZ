// Reference: Rust.Workshop

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

using Facepunch.Steamworks;
using Rust;

namespace Oxide.Plugins
{
    [Info("Item Skin Randomizer", "Mughisi", "1.3.1", ResourceId = 1328)]
    [Description("Simple plugin that will select a random skin for an item when crafting.")]
    class ItemSkinRandomizer : RustPlugin
    {
        private RandomizerConfig config;
        private readonly Dictionary<string, List<int>> skinsCache = new Dictionary<string, List<int>>();
        private readonly List<int> randomizedTasks = new List<int>();
        private readonly FieldInfo skins2 = typeof (ItemDefinition).GetField("_skins2", BindingFlags.NonPublic | BindingFlags.Instance);

        public class RandomizerConfig
        {
            public bool EnablePermissions;
            public bool EnableDefaultSkin;
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<RandomizerConfig>();
        }

        protected override void LoadDefaultConfig()
        {
            config = new RandomizerConfig { EnableDefaultSkin = false, EnablePermissions = false };
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private void OnServerInitialized()
        {
            webrequest.EnqueueGet("http://s3.amazonaws.com/s3.playrust.com/icons/inventory/rust/schema.json", ReadScheme, this);
            if (config.EnablePermissions) permission.RegisterPermission("itemskinrandomizer.use", this);
        }

        private void OnItemCraft(ItemCraftTask task, BasePlayer crafter)
        {
            if (config.EnablePermissions && !permission.UserHasPermission(crafter.UserIDString, "itemskinrandomizer.use")) return;
//            PrintToChat(crafter, "A"+task.blueprint.targetItem.shortname);
            var skins = GetSkins(task.blueprint.targetItem);
//            PrintToChat(crafter,"B:"+skins2);
//            randomizedTasks.Add(task.taskUID);
//           task.skinID = int.Parse("799006291");
//            if (skins.Count == 0 || task.skinID != 0) return;
            randomizedTasks.Add(task.taskUID);
            task.skinID = skins.GetRandom();
            //task.skinID = int.Parse("818376843");
            Puts("Skinid "+task.skinID);           
        }

        private void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            if (!randomizedTasks.Contains(task.taskUID)) return;
            if (task.amount == 0)
            {
                randomizedTasks.Remove(task.taskUID);
                return;
            }
            var skins = GetSkins(task.blueprint.targetItem);
            task.skinID = skins.GetRandom();
        }

        private void ReadScheme(int code, string response)
        {
            if (response != null && code == 200)
            {
                var schema = JsonConvert.DeserializeObject<Rust.Workshop.ItemSchema>(response);
                var defs = new List<Inventory.Definition>();
                foreach (var item in schema.items)
                {
                    if (string.IsNullOrEmpty(item.itemshortname)) continue;
                    var steamItem = Global.SteamServer.Inventory.CreateDefinition((int)item.itemdefid);
                    steamItem.Name = item.name;
                    steamItem.SetProperty("itemshortname", item.itemshortname);
                    steamItem.SetProperty("workshopid", item.workshopid);
                    steamItem.SetProperty("workshopdownload", item.workshopdownload);
                    defs.Add(steamItem);
                }

                Global.SteamServer.Inventory.Definitions = defs.ToArray();
//		ProntToChat(ItemSkinDirectory);

                foreach (var item in ItemManager.itemList)
                    skins2.SetValue(item, Global.SteamServer.Inventory.Definitions.Where(x => (x.GetStringProperty("itemshortname") == item.shortname) && !string.IsNullOrEmpty(x.GetStringProperty("workshopdownload"))).ToArray());

                Puts($"Loaded {Global.SteamServer.Inventory.Definitions.Length} approved workshop skins.");
            }
            else
            {
                PrintWarning($"Failed to load approved workshop skins... Error {code}");
            }
        }

        private List<int> GetSkins(ItemDefinition def)
        {
            ItemSkinDirectory.Skin[] skins10;
            skins10 = ItemSkinDirectory.ForItem(def);
            Puts(skins10.Length.ToString());
            List<int> skins =  new List<int>();
            //foreach (ItemSkinDirectory.Skin val in skins10) Puts("[DEBUG] "+val.name+" - "+val.id);
            foreach (ItemSkinDirectory.Skin val in skins10) skins.Add(val.id);
            return skins;
        }
            /*PrintToChat(crafter, "1:"+skinsCache.TryGetValue(def.shortname, out skins));
            if (skinsCache.TryGetValue(def.shortname, out skins)) return skins;
            skins = new List<int>();
            PrintToChat(crafter, "2:"+def.skins+":"+def.skins2);
            if (config.EnableDefaultSkin) skins.Add(0);
            if (def.skins != null) skins.AddRange(def.skins.Select(skin => skin.id));
            if (def.skins2 != null) skins.AddRange(def.skins2.Select(skin => skin.Id));
            skinsCache.Add(def.shortname, skins);
            //PrintToChat(crafter, "3:"+defs.ToArray().toString())
	PropertyInfo [] pi = def.GetType().GetProperties();
	foreach (PropertyInfo p in pi)
	{
	    PrintToChat(crafter,p.Name.ToString() + "    " + p.GetValue(def, null));
	}
            return skins;
        }*/
    }
}
