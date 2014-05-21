﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using ZTn.BNet.BattleNet;
using ZTn.BNet.D3;
using ZTn.BNet.D3.Artisans;
using ZTn.BNet.D3.Calculator;
using ZTn.BNet.D3.Calculator.Gems;
using ZTn.BNet.D3.Calculator.Helpers;
using ZTn.BNet.D3.Calculator.Sets;
using ZTn.BNet.D3.Careers;
using ZTn.BNet.D3.DataProviders;
using ZTn.BNet.D3.Helpers;
using ZTn.BNet.D3.Heroes;
using ZTn.BNet.D3.HeroFollowers;
using ZTn.BNet.D3.Items;
using ZTn.BNet.D3.Progresses;
using ZTn.BNet.D3.Skills;

namespace ZTn.BNet.D3ProfileExplorer
{
    public partial class GuiD3ProfileExplorer : Form
    {
        #region >> Fields

        private readonly Font defaultNodeFont;
        private readonly List<Host> hosts;

        #endregion

        public GuiD3ProfileExplorer()
        {
            InitializeComponent();

            D3Api.DataProvider = new CacheableDataProvider(new HttpRequestDataProvider());

            hosts = "hosts.json".CreateFromJsonFile<List<Host>>();
            guiBattleNetHostList.DataSource = hosts;
            guiBattleNetHostList.DisplayMember = "name";

            var langs = "languages.json".CreateFromJsonFile<List<Language>>();
            guiBattleNetLanguageList.DataSource = langs;
            guiBattleNetLanguageList.DisplayMember = "name";

            guiD3ProfileExplorerDllName.Text = Assembly.GetExecutingAssembly().GetName().Name;
            guiD3ProfileExplorerVersion.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            guiBattleNetDllName.Text = typeof(BattleTag).Assembly.GetName().Name;
            guiBattleNetVersion.Text = typeof(BattleTag).Assembly.GetName().Version.ToString();
            guiD3APIDllName.Text = typeof(D3Api).Assembly.GetName().Name;
            guiD3APIVersion.Text = typeof(D3Api).Assembly.GetName().Version.ToString();
            defaultNodeFont = new Font(guiD3ProfileTreeView.Font, FontStyle.Underline);
        }

        private void guiProfileLookup_Click(object sender, EventArgs e)
        {
            var battleTag = new BattleTag(guiBattleTag.Text);

            var node = new TreeNode("Career of " + battleTag + " on " + D3Api.Host);

            Career career;
            try
            {
                career = Career.CreateFromBattleTag(battleTag);
            }
            catch (FileNotInCacheException)
            {
                MessageBox.Show("Career was not found in cache: go online to retrieve it.");
                return;
            }
            catch (BNetResponseFailedException)
            {
                MessageBox.Show("Battle.net sent an http error: try again later.");
                return;
            }
            catch (BNetFailureObjectReturnedException)
            {
                MessageBox.Show("Battle.net sent an error: verify the battle tag.");
                return;
            }

            node.Tag = new BNetContext<Career>(D3Api.Host, battleTag, career);

            node.Nodes.AddRange(CreateNodeFromD3Object(career).ToArray());

            guiD3ProfileTreeView.Nodes.Add(node);
        }

        private List<TreeNode> CreateNodeFromD3Object(Object d3Object)
        {
            var newNodes = new List<TreeNode>();

            if (d3Object == null)
            {
                return newNodes;
            }

            var type = d3Object.GetType();

            if (type.IsArray)
            {
                foreach (var o in (Object[])d3Object)
                {
                    var newNode = new TreeNode(String.Format("[{0}]", o.GetType().Name));
                    newNode.Nodes.AddRange(CreateNodeFromD3Object(o).ToArray());
                    InsertContextMenu(newNode, (dynamic)o);
                    UpdateNodeText(newNode, (dynamic)o);
                    newNodes.Add(newNode);
                }
            }
            else if (type.IsEnum)
            {
                var newNode = new TreeNode(d3Object.ToString());
                newNodes.Add(newNode);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                foreach (var o in (IList)d3Object)
                {
                    var newNode = new TreeNode(String.Format("[{0}]", o.GetType().Name));
                    newNode.Nodes.AddRange(CreateNodeFromD3Object(o).ToArray());
                    InsertContextMenu(newNode, (dynamic)o);
                    UpdateNodeText(newNode, (dynamic)o);
                    newNodes.Add(newNode);
                }
            }
            else if (type == typeof(Dictionary<string, JToken>))
            {
                foreach (var o in (Dictionary<string, JToken>)d3Object)
                {
                    var newNode = new TreeNode(String.Format("{0}: {1}", o.Key, o.Value));
                    InsertContextMenu(newNode, (dynamic)o);
                    UpdateNodeText(newNode, (dynamic)o);
                    newNodes.Add(newNode);
                }
            }
            else if (type.FullName.Contains("ZTn.BNet.D3"))
            {
                foreach (var propertyInfo in type.GetProperties())
                {
                    var d3ObjectValue = propertyInfo.GetValue(d3Object, null);
                    if ((d3ObjectValue != null))
                    {
                        var newNode = new TreeNode(propertyInfo.Name);
                        newNode.Nodes.AddRange(CreateNodeFromD3Object(d3ObjectValue).ToArray());
                        InsertContextMenu(newNode, (dynamic)d3ObjectValue);
                        UpdateNodeText(newNode, (dynamic)d3ObjectValue);
                        newNodes.Add(newNode);
                    }
                }
                var fieldInfos = type.GetFields();
                foreach (var fieldInfo in fieldInfos.Where(info => !info.IsStatic))
                {
                    var d3ObjectValue = fieldInfo.GetValue(d3Object);
                    if (d3ObjectValue != null)
                    {
                        var newNode = new TreeNode(fieldInfo.Name);
                        newNode.Nodes.AddRange(CreateNodeFromD3Object(d3ObjectValue).ToArray());
                        InsertContextMenu(newNode, (dynamic)d3ObjectValue);
                        UpdateNodeText(newNode, (dynamic)d3ObjectValue);
                        newNodes.Add(newNode);
                    }
                }
            }
            else
            {
                var newNode = new TreeNode(d3Object.ToString());
                newNodes.Add(newNode);
            }

            return newNodes;
        }

        #region >> InsertContextMenu Overloads

        private void InsertContextMenu(TreeNode node, Object d3Object)
        {
            // Catch "all": no context menu
        }

        private void InsertContextMenu(TreeNode node, Hero d3Object)
        {
            node.Tag = new BNetContext<Hero>(D3Api.Host, new BattleTag(guiBattleTag.Text), d3Object);
            node.ContextMenuStrip = guiHeroContextMenu;
            node.NodeFont = defaultNodeFont;
        }

        private void InsertContextMenu(TreeNode node, HeroSummary d3Object)
        {
            node.Tag = new BNetContext<HeroSummary>(D3Api.Host, new BattleTag(guiBattleTag.Text), d3Object);
            node.ContextMenuStrip = guiHeroSummaryContextMenu;
            node.NodeFont = defaultNodeFont;
        }

        private void InsertContextMenu(TreeNode node, Item d3Object)
        {
            node.Tag = d3Object;
            node.ContextMenuStrip = guiItemContextMenu;
            node.NodeFont = defaultNodeFont;
        }

        private void InsertContextMenu(TreeNode node, ItemSummary d3Object)
        {
            node.Tag = d3Object;
            node.ContextMenuStrip = guiItemSummaryContextMenu;
            node.NodeFont = defaultNodeFont;
        }

        private void InsertContextMenu(TreeNode node, CareerArtisan d3Object)
        {
            node.Tag = d3Object;
            node.ContextMenuStrip = guiCareerArtisanContextMenu;
            node.NodeFont = defaultNodeFont;
        }

        private void InsertContextMenu(TreeNode node, Skill d3Object)
        {
            node.Tag = d3Object;
            node.ContextMenuStrip = guiSkillContextMenu;
            node.NodeFont = defaultNodeFont;
        }

        private void InsertContextMenu(TreeNode node, Follower d3Object)
        {
            node.Tag = d3Object;
            node.ContextMenuStrip = guiFollowerContextMenu;
            node.NodeFont = defaultNodeFont;
        }

        #endregion

        #region >> UpdateNodeText Overloads

        private static void UpdateNodeText(TreeNode node, Object d3Object)
        {
        }

        private static void UpdateNodeText(TreeNode node, Affix d3Object)
        {
            var sources = new[] { d3Object.Attributes.Primary, d3Object.Attributes.Secondary, d3Object.Attributes.Passive };
            var text = sources.Where(s => s != null)
                .SelectMany(s => s)
                .Select(s => "[" + s.Text + "]")
                .Aggregate((c, s) => c + s);
            node.Text += " >> " + text;
        }

        private static void UpdateNodeText(TreeNode node, ActProgress d3Object)
        {
            node.Text += " >> " + d3Object.completed;
        }

        private static void UpdateNodeText(TreeNode node, bool d3Object)
        {
            node.Text += " >> " + d3Object;
        }

        private static void UpdateNodeText(TreeNode node, ItemTextAttribute d3Object)
        {
            node.Text += " >> " + d3Object.Text;
        }

        private static void UpdateNodeText(TreeNode node, CareerArtisan d3Object)
        {
            node.Text += " >> " + d3Object.slug;
        }

        private static void UpdateNodeText(TreeNode node, float d3Object)
        {
            node.Text += " >> " + d3Object;
        }

        private static void UpdateNodeText(TreeNode node, HeroSummary d3Object)
        {
            node.Text += String.Format(" >> L:{1:D2} P:{2:D2} - {0}", d3Object.name, d3Object.level, d3Object.paragonLevel);
        }

        private static void UpdateNodeText(TreeNode node, int d3Object)
        {
            node.Text += " >> " + d3Object;
        }

        private static void UpdateNodeText(TreeNode node, ItemSummary d3Object)
        {
            node.Text += " >> " + d3Object.Name;
        }

        private static void UpdateNodeText(TreeNode node, ItemValueRange d3Object)
        {
            node.Text += " >> [ " + d3Object.Min + " - " + d3Object.Max + "]";
        }

        private static void UpdateNodeText(TreeNode node, Set d3Object)
        {
            node.Text += " >> " + d3Object.name;
        }

        private static void UpdateNodeText(TreeNode node, Skill d3Object)
        {
            node.Text += " >> " + d3Object.name;
        }

        private static void UpdateNodeText(TreeNode node, Quest d3Object)
        {
            node.Text += " >> " + d3Object.name;
        }

        private static void UpdateNodeText(TreeNode node, Recipe d3Object)
        {
            node.Text += " >> " + d3Object.name;
        }

        private static void UpdateNodeText(TreeNode node, String d3Object)
        {
            node.Text += " >> " + d3Object;
        }

        #endregion

        #region >> OnNodeClick Overloads

        private void OnNodeClick(Object d3Object)
        {
            D3ObjectLiveUrl.Text = "";
        }

        private void OnNodeClick(BNetContext<Career> d3Object)
        {
            guiBattleNetHostList.SelectedItem = hosts.FirstOrDefault(h => h.Url == d3Object.Host);
            guiBattleTag.Text = d3Object.BattleTag.ToString();
            D3ObjectLiveUrl.Text = D3Api.GetCareerUrl(d3Object.BattleTag);
        }

        private void OnNodeClick(BNetContext<Hero> d3Object)
        {
            guiBattleNetHostList.SelectedItem = hosts.FirstOrDefault(h => h.Url == d3Object.Host);
            guiBattleTag.Text = d3Object.BattleTag.ToString();
            D3ObjectLiveUrl.Text = D3Api.GetHeroUrlFromHeroId(d3Object.BattleTag, d3Object.Data.id);
        }

        private void OnNodeClick(BNetContext<HeroSummary> d3Object)
        {
            guiBattleNetHostList.SelectedItem = hosts.FirstOrDefault(h => h.Url == d3Object.Host);
            guiBattleTag.Text = d3Object.BattleTag.ToString();
            D3ObjectLiveUrl.Text = D3Api.GetHeroUrlFromHeroId(d3Object.BattleTag, d3Object.Data.id);
        }

        private void OnNodeClick(Item d3Object)
        {
            D3ObjectLiveUrl.Text = D3Api.GetItemUrlFromTooltipParams(d3Object.TooltipParams);
        }

        private void OnNodeClick(ItemSummary d3Object)
        {
            D3ObjectLiveUrl.Text = D3Api.GetItemUrlFromTooltipParams(d3Object.TooltipParams);
        }

        private void OnNodeClick(CareerArtisan d3Object)
        {
            D3ObjectLiveUrl.Text = D3Api.GetArtisanUrlFromSlug(d3Object.slug);
        }

        #endregion

        private void guiD3ProfileTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null && e.Node.Tag != null)
            {
                OnNodeClick((dynamic)e.Node.Tag);
            }
            else
            {
                OnNodeClick((Object)null);
            }
        }

        private void guiD3ProfileTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                guiD3ProfileTreeView.SelectedNode = e.Node;
            }
        }

        private void exploreHeroToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var heroSummaryInformation = (BNetContext<HeroSummary>)guiD3ProfileTreeView.SelectedNode.Tag;

            var node = new TreeNode("Hero " + heroSummaryInformation.BattleTag + " / " + heroSummaryInformation.Data.id + " (" + heroSummaryInformation.Data.name + ")");

            Hero hero;
            try
            {
                hero = Hero.CreateFromHeroId(heroSummaryInformation.BattleTag, heroSummaryInformation.Data.id);
            }
            catch (FileNotInCacheException)
            {
                MessageBox.Show("Hero was not found in cache: go online to retrieve it.");
                return;
            }

            node.Nodes.AddRange(CreateNodeFromD3Object(hero).ToArray());
            InsertContextMenu(node, hero);

            guiD3ProfileTreeView.Nodes.Add(node);
        }

        private void exploreItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var itemSummary = (ItemSummary)guiD3ProfileTreeView.SelectedNode.Tag;

            if (itemSummary.TooltipParams != null)
            {
                var node = new TreeNode("Item [ " + itemSummary.Name + " ]");

                Item item;
                try
                {
                    item = Item.CreateFromTooltipParams(itemSummary.TooltipParams);
                }
                catch (FileNotInCacheException)
                {
                    MessageBox.Show("Item was not found in cache: go online to retrieve it.");
                    return;
                }

                InsertContextMenu(node, item);

                node.Nodes.AddRange(CreateNodeFromD3Object(item).ToArray());

                guiD3ProfileTreeView.Nodes.Add(node);
            }
        }

        private void exploreCareerArtisanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var careerArtisan = (CareerArtisan)guiD3ProfileTreeView.SelectedNode.Tag;

            var node = new TreeNode("Artisan " + careerArtisan.slug);

            var artisan = Artisan.CreateFromSlug(careerArtisan.slug);

            node.Nodes.AddRange(CreateNodeFromD3Object(artisan).ToArray());

            guiD3ProfileTreeView.Nodes.Add(node);
        }

        private void guiBattleNetHostList_TextChanged(object sender, EventArgs e)
        {
            D3Api.Host = ((Host)guiBattleNetHostList.SelectedItem).Url;
        }

        private void guiBattleNetLanguageList_TextChanged(object sender, EventArgs e)
        {
            D3Api.Locale = ((Language)guiBattleNetLanguageList.SelectedItem).Code;
        }

        private void guiOfflineMode_CheckStateChanged(object sender, EventArgs e)
        {
            var dataProvider = D3Api.DataProvider as CacheableDataProvider;
            if (dataProvider == null)
            {
                return;
            }

            dataProvider.FetchMode = (guiOfflineMode.Checked ? FetchMode.Offline : FetchMode.Online);
        }

        private void d3CalculatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var heroInformation = (BNetContext<Hero>)guiD3ProfileTreeView.SelectedNode.Tag;
            var hero = heroInformation.Data;

            new D3CalculatorForm(hero).Show();
        }

        private void d3CalculatorFollowerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var follower = (Follower)guiD3ProfileTreeView.SelectedNode.Tag;

            var slugToHeroClass = new Dictionary<string, HeroClass>
            {
                { "enchantress", HeroClass.EnchantressFollower },
                { "scoundrel", HeroClass.ScoundrelFollower },
                { "templar", HeroClass.TemplarFollower }
            };

            new D3CalculatorForm(follower, slugToHeroClass[follower.slug]).Show();
        }

        private void buildUniqueItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var heroInformation = (BNetContext<Hero>)guiD3ProfileTreeView.SelectedNode.Tag;
            var hero = heroInformation.Data;

            var heroItems = new[]
            {
                hero.items.bracers,
                hero.items.feet,
                hero.items.hands,
                hero.items.head,
                hero.items.leftFinger,
                hero.items.legs,
                hero.items.neck,
                hero.items.rightFinger,
                hero.items.shoulders,
                hero.items.torso,
                hero.items.waist
            };

            var items = heroItems
                .Where(hi => hi != null)
                .Select(hi => Item.CreateFromTooltipParams(hi.TooltipParams))
                .ToList();

            var mainHand = Item.CreateFromTooltipParams(hero.items.mainHand.TooltipParams);

            Item offHand;
            if (hero.items.offHand != null)
            {
                offHand = Item.CreateFromTooltipParams(hero.items.offHand.TooltipParams);
            }
            else
            {
                offHand = new Item { AttributesRaw = new ItemAttributes() };
            }

            var heroStuff = new StatsItem(mainHand, offHand, items.ToArray());
            heroStuff.Update();

            var node = new TreeNode("Unique Item for " + hero.name);
            node.Nodes.AddRange(CreateNodeFromD3Object(heroStuff).ToArray());

            guiD3ProfileTreeView.Nodes.Add(node);
        }

        private void buildUniqueItemFollowerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var follower = (Follower)guiD3ProfileTreeView.SelectedNode.Tag;

            var followerItems = new[]
            {
                follower.items.special,
                follower.items.leftFinger,
                follower.items.neck,
                follower.items.rightFinger
            };

            var items = followerItems
                .Where(fi => fi != null)
                .Select(fi => Item.CreateFromTooltipParams(fi.TooltipParams))
                .ToList();

            var mainHand = Item.CreateFromTooltipParams(follower.items.mainHand.TooltipParams);

            Item offHand;
            if (follower.items.offHand != null)
            {
                offHand = Item.CreateFromTooltipParams(follower.items.offHand.TooltipParams);
            }
            else
            {
                offHand = new Item(new ItemAttributes());
            }

            var heroStuff = new StatsItem(mainHand, offHand, items.ToArray());
            heroStuff.Update();

            var node = new TreeNode("Unique Item for " + follower.slug + " follower");
            node.Nodes.AddRange(CreateNodeFromD3Object(heroStuff).ToArray());

            guiD3ProfileTreeView.Nodes.Add(node);
        }

        private void d3CalculatorHeroSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var heroSummaryInformation = (BNetContext<HeroSummary>)guiD3ProfileTreeView.SelectedNode.Tag;

            Hero hero;
            try
            {
                hero = Hero.CreateFromHeroId(heroSummaryInformation.BattleTag, heroSummaryInformation.Data.id);
            }
            catch (FileNotInCacheException)
            {
                MessageBox.Show("Hero was not found in cache: go online to retrieve it.");
                return;
            }

            new D3CalculatorForm(hero).Show();
        }

        private void getItemLargeIconToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var itemSummary = (ItemSummary)guiD3ProfileTreeView.SelectedNode.Tag;
            if (itemSummary.Icon != null)
            {
                var picture = D3Api.GetItemIcon(itemSummary.Icon, "large");
                using (var imageStream = new MemoryStream(picture.Bytes))
                {
                    guiD3Icon.Image = Image.FromStream(imageStream);
                }
            }
        }

        private void getItemSmallIconToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var itemSummary = (ItemSummary)guiD3ProfileTreeView.SelectedNode.Tag;
            if (itemSummary.Icon != null)
            {
                var picture = D3Api.GetItemIcon(itemSummary.Icon);
                using (var imageStream = new MemoryStream(picture.Bytes))
                {
                    guiD3Icon.Image = Image.FromStream(imageStream);
                }
            }
        }

        private void getSkillIcon42ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var skill = (Skill)guiD3ProfileTreeView.SelectedNode.Tag;
            if (skill.icon != null)
            {
                var picture = D3Api.GetSkillIcon(skill.icon);
                using (var imageStream = new MemoryStream(picture.Bytes))
                {
                    guiD3Icon.Image = Image.FromStream(imageStream);
                }
            }
        }

        private void getSkillIcon64ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var skill = (Skill)guiD3ProfileTreeView.SelectedNode.Tag;
            if (skill.icon != null)
            {
                var picture = D3Api.GetSkillIcon(skill.icon, "64");
                using (var imageStream = new MemoryStream(picture.Bytes))
                {
                    guiD3Icon.Image = Image.FromStream(imageStream);
                }
            }
        }

        private void guiLoadKnownGems_Click(object sender, EventArgs e)
        {
            var node = new TreeNode("Known Gems (loaded from d3gem.json)");

            KnownGems knownGems;
            try
            {
                knownGems = KnownGems.GetKnownGemsFromJsonFile("d3gem.json");
            }
            catch (FileNotInCacheException)
            {
                MessageBox.Show("Known gems file was not found");
                return;
            }

            node.Nodes.AddRange(CreateNodeFromD3Object(knownGems).ToArray());

            guiD3ProfileTreeView.Nodes.Add(node);
        }

        private void getMetaItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var itemSummary = (ItemSummary)guiD3ProfileTreeView.SelectedNode.Tag;

            if (itemSummary.Id != null)
            {
                var node = new TreeNode("Item (meta) [ " + itemSummary.Name + " ]");

                Item item;
                try
                {
                    item = Item.CreateFromTooltipParams("item/" + itemSummary.Id);
                }
                catch (FileNotInCacheException)
                {
                    MessageBox.Show("Item was not found in cache: go online to retrieve it.");
                    return;
                }

                node.Nodes.AddRange(CreateNodeFromD3Object(item).ToArray());

                guiD3ProfileTreeView.Nodes.Add(node);
            }
        }

        private void simplifyItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var simplifiedItem = ((Item)guiD3ProfileTreeView.SelectedNode.Tag).Simplify();

            var node = new TreeNode("Item (simplified) [ " + simplifiedItem.Name + " ]");

            InsertContextMenu(node, simplifiedItem);

            node.Nodes.AddRange(CreateNodeFromD3Object(simplifiedItem).ToArray());

            guiD3ProfileTreeView.Nodes.Add(node);
        }

        private void guiUpdateKnownGems_Click(object sender, EventArgs e)
        {
            var socketColors = new List<string> { "Amethyst", "Diamond", "Emerald", "Ruby", "Topaz" };

            var sockets = new List<Item>();

            foreach (var gemColor in socketColors)
            {
                for (var index = 1; index < 20; index++)
                {
                    var id = String.Format("{0}_{1:00}", gemColor, index);
                    sockets.Add(Item.CreateFromTooltipParams("item/" + id));
                }
            }

            sockets.WriteToJsonFile("d3gem.json");

            var node = new TreeNode("Updated Gems from battle.net (saved from d3gem.json)");

            var knownSets = KnownGems.GetKnownGemsFromJsonFile("d3gem.json");

            node.Nodes.AddRange(CreateNodeFromD3Object(knownSets).ToArray());

            guiD3ProfileTreeView.Nodes.Add(node);
        }

        private void guiBattleTag_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                guiProfileLookup_Click(sender, e);
            }
        }
    }
}