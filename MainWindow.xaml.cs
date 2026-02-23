using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace L2_Calc;

public partial class MainWindow : Window
{
    public ObservableCollection<Npc> Npcs { get; } = new();

    private string _baseFolder = AppContext.BaseDirectory;
    private string _itemsFilePath;
    private Dictionary<string, int> itemValues = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _itemsFilePath = Path.Combine(_baseFolder, "itemValues.json");

        // set initial level boxes
        MinLevelBox.Text = "1";
        MaxLevelBox.Text = "99";

        LoadAndPopulate();
    }

    private void LoadAndPopulate()
    {
        Npcs.Clear();
        StatusText.Text = "Loading...";

        // load item values from file if present
        if (File.Exists(_itemsFilePath))
        {
            try
            {
                var json = File.ReadAllText(_itemsFilePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                if (dict != null) itemValues = dict;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load item values: {ex.Message}");
            }
        }

        // fallback: if empty, try to load bundled default (file in repo root)
        if (itemValues.Count == 0 && File.Exists(Path.Combine(_baseFolder, "..", "..", "itemValues.json")))
        {
            try
            {
                var rootJson = Path.Combine(_baseFolder, "..", "..", "itemValues.json");
                var json = File.ReadAllText(rootJson);
                var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                if (dict != null) itemValues = dict;
            }
            catch { }
        }

        // find npcs folder
        var npcsFolder = Path.Combine(_baseFolder, "npcs");
        var files = Directory.Exists(npcsFolder) ? Directory.GetFiles(npcsFolder, "*.xml") : Array.Empty<string>();

        var serializer = new XmlSerializer(typeof(NpcList));
        var discoveredItems = new Dictionary<int, string>();

        int minLevel = int.TryParse(MinLevelBox.Text, out var ml) ? ml : 1;
        int maxLevel = int.TryParse(MaxLevelBox.Text, out var xl) ? xl : 999;

        foreach (var f in files)
        {
            try
            {
                using var stream = File.OpenRead(f);
                var list = (NpcList?)serializer.Deserialize(stream);
                if (list?.Npcs != null)
                {
                    var doc = XDocument.Load(f);
                    var npcElements = doc.Root?.Elements("npc");

                    foreach (var npc in list.Npcs)
                    {
                        XElement? npcElem = null;
                        if (npcElements != null)
                            npcElem = npcElements.FirstOrDefault(x => (int?)x.Attribute("id") == npc.Id);

                        if (npcElem != null)
                        {
                            var dropListsElem = npcElem.Element("dropLists");
                            if (dropListsElem != null)
                            {
                                foreach (var dropElem in dropListsElem.Elements("drop"))
                                {
                                    foreach (var groupElem in dropElem.Elements("group"))
                                    {
                                        foreach (var itemElem in groupElem.Elements("item"))
                                        {
                                            var idAttr = (int?)itemElem.Attribute("id");
                                            if (idAttr == null) continue;
                                            var comment = itemElem.NodesAfterSelf().OfType<XComment>().FirstOrDefault();
                                            var itemName = comment?.Value.Trim();
                                            if (!string.IsNullOrEmpty(itemName))
                                            {
                                                discoveredItems[(int)idAttr] = itemName;
                                                if (npc.DropLists?.Drop != null)
                                                {
                                                    foreach (var drop in npc.DropLists.Drop)
                                                    {
                                                        if (drop?.Groups == null) continue;
                                                        foreach (var grp in drop.Groups)
                                                        {
                                                            if (grp?.Items == null) continue;
                                                            foreach (var di in grp.Items.Where(x => x.Id == idAttr))
                                                            {
                                                                di.Name = itemName;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                // process skillList comments: <skill id="..." level="..." /> <!-- Skill Name -->
                                var skillListElem = npcElem.Element("skillList");
                                if (skillListElem != null && npc.SkillList?.Skills != null)
                                {
                                    foreach (var skillElem in skillListElem.Elements("skill"))
                                    {
                                        var idAttr = (int?)skillElem.Attribute("id");
                                        var levelAttr = (int?)skillElem.Attribute("level");
                                        if (idAttr == null) continue;
                                        var comment = skillElem.NodesAfterSelf().OfType<XComment>().FirstOrDefault();
                                        var skillName = comment?.Value.Trim();
                                        if (string.IsNullOrEmpty(skillName)) continue;

                                        foreach (var sk in npc.SkillList.Skills.Where(s => s.Id == idAttr && s.Level == levelAttr))
                                        {
                                            sk.Name = skillName;
                                        }
                                    }
                                }

                                var spoilElem = dropListsElem.Element("spoil");
                                if (spoilElem != null)
                                {
                                    foreach (var itemElem in spoilElem.Elements("item"))
                                    {
                                        var idAttr = (int?)itemElem.Attribute("id");
                                        if (idAttr == null) continue;
                                        var comment = itemElem.NodesAfterSelf().OfType<XComment>().FirstOrDefault();
                                        var itemName = comment?.Value.Trim();
                                            if (!string.IsNullOrEmpty(itemName))
                                            {
                                                discoveredItems[(int)idAttr] = itemName;
                                                if (npc.DropLists?.Spoil?.Items != null)
                                                {
                                                    foreach (var si in npc.DropLists.Spoil.Items.Where(x => x.Id == idAttr))
                                                    {
                                                        // assign the item name to the spoil item (no asterisk on item)
                                                        si.Name = itemName;

                                                        // if this spoil item can drop more than 1 (min>1 or max>1),
                                                        // mark the NPC name with a trailing '*' (avoid duplicates)
                                                        if ((si.Min > 1) || (si.Max > 1))
                                                        {
                                                            if (!string.IsNullOrEmpty(npc.Name) && !npc.Name.EndsWith("*"))
                                                            {
                                                                npc.Name += "*";
                                                            }
                                                        }
                                                    }
                                                }
                                            if (npc.DropLists?.Drop != null)
                                            {
                                                foreach (var drop in npc.DropLists.Drop)
                                                {
                                                    if (drop?.Groups == null) continue;
                                                    foreach (var grp in drop.Groups)
                                                    {
                                                        if (grp?.Items == null) continue;
                                                        foreach (var di in grp.Items.Where(x => x.Id == idAttr))
                                                        {
                                                            di.Name = itemName;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                // apply level filter
                                if (npc.Level >= minLevel && npc.Level <= maxLevel)
                                    Npcs.Add(npc);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // Now compute values same as before
        foreach (var npc in Npcs)
        {
            double total = 0;
            double spoilTotal = 0;

            if (npc.DropLists?.Drop != null)
            {
                foreach (var drop in npc.DropLists.Drop)
                {
                    if (drop?.Groups == null) continue;
                    foreach (var grp in drop.Groups)
                    {
                        if (grp?.Items == null) continue;
                        var groupChance = grp.Chance / 100.0;
                        foreach (var item in grp.Items)
                        {
                            var name = item.Name;
                            if (string.IsNullOrEmpty(name)) continue;
                            if (!itemValues.TryGetValue(name, out var val))
                            {
                                // add with 0 so we persist for manual editing later
                                itemValues[name] = 0;
                                val = 0;
                            }

                            var itemChance = item.Chance / 100.0;
                            var totalChance = groupChance * itemChance;
                            var avgQty = (item.Min + item.Max) / 2.0;
                            var contribution = totalChance * val * avgQty;
                            item.Value = contribution;
                            total += contribution;
                        }
                    }
                }
            }

            if (npc.DropLists?.Spoil?.Items != null)
            {
                foreach (var sitem in npc.DropLists.Spoil.Items)
                {
                    var name = sitem.Name;
                    if (string.IsNullOrEmpty(name)) continue;
                    if (!itemValues.TryGetValue(name, out var sval))
                    {
                        itemValues[name] = 0;
                        sval = 0;
                    }
                    var itemChance = sitem.Chance / 100.0;
                    var avgQty = (sitem.Min + sitem.Max) / 2.0;
                    var contribution = itemChance * sval * avgQty;
                    sitem.Value = contribution;
                    spoilTotal += contribution;
                }
            }

            // If NPC has skills that indicate a multiplier in their name (e.g. "HP Increase (2x)")
            // use those multipliers as divisors for the calculated values.
            double divisor = 1.0;
            if (npc.SkillList?.Skills != null)
            {
                foreach (var sk in npc.SkillList.Skills)
                {
                    if (string.IsNullOrEmpty(sk.Name))
                        continue;
                    var m = Regex.Match(sk.Name, "\\((\\d+)x\\)", RegexOptions.IgnoreCase);
                    if (m.Success && int.TryParse(m.Groups[1].Value, out var mult) && mult > 0)
                    {
                        divisor *= mult;
                    }
                }
            }

            if (divisor > 1.0)
            {
                // scale per-item values as well so UI remains consistent
                if (npc.DropLists?.Drop != null)
                {
                    foreach (var drop in npc.DropLists.Drop)
                    {
                        if (drop?.Groups == null) continue;
                        foreach (var grp in drop.Groups)
                        {
                            if (grp?.Items == null) continue;
                            foreach (var item in grp.Items)
                            {
                                item.Value /= divisor;
                            }
                        }
                    }
                }

                if (npc.DropLists?.Spoil?.Items != null)
                {
                    foreach (var sitem in npc.DropLists.Spoil.Items)
                        sitem.Value /= divisor;
                }

                total /= divisor;
                spoilTotal /= divisor;
            }

            npc.TotalValue = total;
            npc.TotalSpoil = spoilTotal;
            npc.TotalWealth = total + spoilTotal;
        }        

        // sort by TotalWealth desc
        var sorted = Npcs.OrderByDescending(n => n.TotalWealth).ToList();
        Npcs.Clear();
        foreach (var n in sorted) Npcs.Add(n);

        StatusText.Text = $"Loaded {Npcs.Count} NPCs";
        FooterText.Text = $"Files scanned: {files.Length}";
    }

    private void ReloadBtn_Click(object sender, RoutedEventArgs e)
    {
        LoadAndPopulate();
    }

    // single-click on a row toggles its details; ignore clicks that originate
    // from inside the RowDetails area so inner interactions don't collapse the row
    private void NpcGrid_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var src = e.OriginalSource as DependencyObject;
        while (src != null)
        {
            // if click happened inside row details area, ignore
            // avoid direct type reference to DataGridRowDetailsPresenter (prevents XAML/code-gen issues)
            var t = src.GetType();
            if (string.Equals(t.Name, "DataGridRowDetailsPresenter", StringComparison.Ordinal))
                return;

            // if we reached the row itself, toggle details
            if (src is DataGridRow row)
            {
                row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                return;
            }

            src = System.Windows.Media.VisualTreeHelper.GetParent(src);
        }
    }
}
