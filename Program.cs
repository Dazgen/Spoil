using System;
using System.IO;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Text.Json;
using System.Linq;

// Deserialize an NPC XML file into a single object that contains all NPC entries.
var folder = Path.Combine(AppContext.BaseDirectory, "npcs");
var files = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.xml") : Array.Empty<string>();
if (files.Length == 0)
{
    Console.WriteLine("No npc xml files found in 'npcs' folder.");
    return;
}

var serializer = new XmlSerializer(typeof(NpcList));
var collected = new List<Npc>();
var itemsDict = new Dictionary<int, string>();

// Filter range for NPC levels (change these values in code to adjust filtering)
int minLevel = 1;
int maxLevel = 99;

var itemValuesPath = Path.Combine(AppContext.BaseDirectory, "itemValues.json");
Dictionary<string, int> itemValues;
if (File.Exists(itemValuesPath))
{
    try
    {
        var json = File.ReadAllText(itemValuesPath);
        itemValues = JsonSerializer.Deserialize<Dictionary<string,int>>(json) ?? new Dictionary<string,int>();
    }
    catch
    {
        itemValues = new Dictionary<string,int>();
    }
}
else
{
    itemValues = new Dictionary<string,int>();
}

// Helper to add default only when missing
void Ensure(string name, int v) { if (!itemValues.ContainsKey(name)) itemValues[name] = v; }
Ensure("Adena", 1);
Ensure("Adamantite Nugget", 4750);
Ensure("Animal Bone", 625);
Ensure("Animal Skin", 750);
Ensure("Asofe", 25000);
Ensure("Braided Hemp", 300);
Ensure("Charcoal", 475);
Ensure("Coal", 800);
Ensure("Coarse Bone Powder", 6500);
Ensure("Cokes", 5200);
Ensure("Cord", 850);
Ensure("Crafted Leather", 27000);
Ensure("Durable Metal Plate", 90000);
Ensure("Enria", 52000);
Ensure("Hight Grade Suede", 4000);
Ensure("Iron Ore", 1100);
Ensure("Leather", 3800);
Ensure("Metal Hardener", 6000);
Ensure("Metalic Fiber", 900);
Ensure("Metallic Thread", 9000);
Ensure("Mithril Ore", 11500);
Ensure("Mold Glue", 27000);
Ensure("Mold Hardener", 31000);
Ensure("Mold Lubricant", 33000);
Ensure("Oriharukon Ore", 30000);
Ensure("Steel", 12000);
Ensure("Stone of Purity", 30000);
Ensure("Thons", 8000);
Ensure("Varnish", 850);
Ensure("Scroll: Enchant Weapon (Grade D)", 500000);
Ensure("Scroll: Enchant Weapon (Grade C)", 750000);
Ensure("Scroll: Enchant Weapon (Grade B)", 3000000);
Ensure("Scroll: Enchant Weapon (Grade A)", 10000000);
Ensure("Scroll: Enchant Weapon (Grade S)", 48000000);
Ensure("Scroll: Enchant Armor (Grade D)", 15000);
Ensure("Scroll: Enchant Armor (Grade C)", 90000);
Ensure("Scroll: Enchant Armor (Grade B)", 20000);
Ensure("Scroll: Enchant Armor (Grade A)", 1500000);
Ensure("Scroll: Enchant Armor (Grade S)", 3000000);


foreach (var f in files)
{
    try
    {
        using var stream = File.OpenRead(f);
        var list = (NpcList?)serializer.Deserialize(stream);
        if (list?.Npcs != null)
        {
            // Load the XML with LINQ to XML to capture comments that hold item names
            var doc = XDocument.Load(f);
            var npcElements = doc.Root?.Elements("npc");

            foreach (var npc in list.Npcs)
            {
                // find xml element for this npc by id
                XElement? npcElem = null;
                if (npcElements != null)
                    npcElem = npcElements.FirstOrDefault(x => (int?)x.Attribute("id") == npc.Id);

                if (npcElem != null)
                {
                    var dropListsElem = npcElem.Element("dropLists");
                    if (dropListsElem != null)
                    {
                        // process <drop> -> <group> -> <item /> comments
                        foreach (var dropElem in dropListsElem.Elements("drop"))
                        {
                            foreach (var groupElem in dropElem.Elements("group"))
                            {
                                foreach (var itemElem in groupElem.Elements("item"))
                                {
                                    var idAttr = (int?)itemElem.Attribute("id");
                                    if (idAttr == null)
                                        continue;

                                    // attempt to get comment immediately after the item element
                                    var comment = itemElem.NodesAfterSelf().OfType<XComment>().FirstOrDefault();
                                    var itemName = comment?.Value.Trim();
                                    if (!string.IsNullOrEmpty(itemName))
                                    {
                                        itemsDict[(int)idAttr] = itemName;

                                        // set name on deserialized objects that match this id
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

                        // process spoil items (they are direct children of <spoil>)
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
                                    itemsDict[(int)idAttr] = itemName;
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
                                    // set spoil item names too
                                    if (npc.DropLists?.Spoil?.Items != null)
                                    {
                                        foreach (var si in npc.DropLists.Spoil.Items.Where(x => x.Id == idAttr))
                                            si.Name = itemName;
                                    }
                                }
                            }
                        }

                        // apply level filter before adding
                        if (npc.Level >= minLevel && npc.Level <= maxLevel)
                            collected.Add(npc);
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Skipping {Path.GetFileName(f)}: {ex.Message}");
    }
}

Console.WriteLine($"Collected {collected.Count} npc(s) with dropLists from {files.Length} files.");
if (collected.Count > 0)
{
    var first = collected[0];
    Console.WriteLine($"First NPC: id={first.Id} name={first.Name} level={first.Level} type={first.Type}");
}

// Example: print number of discovered named items
Console.WriteLine($"Discovered {itemsDict.Count} item names from comments.");

// Calculate TotalValue, TotalSpoil and TotalWealth for each collected NPC
foreach (var npc in collected)
{
    double total = 0.0;
    double spoilTotal = 0.0;

    if (npc.DropLists?.Drop != null)
    {
        foreach (var drop in npc.DropLists.Drop)
        {
            if (drop?.Groups == null) continue;
            foreach (var grp in drop.Groups)
            {
                if (grp?.Items == null) continue;
                // group.Chance is parsed as a percentage in the XML (e.g. "70" meaning 70%)
                var groupChance = grp.Chance / 100.0;
                foreach (var item in grp.Items)
                {
                    if (string.IsNullOrEmpty(item.Name))
                        continue;
                    if (!itemValues.TryGetValue(item.Name, out var val))
                    {
                        // if missing, add with 0 value and continue using 0
                        val = 0;
                        itemValues[item.Name] = 0;
                    }

                    // item.Chance is also a percentage in the XML
                    var itemChance = item.Chance / 100.0;
                    var totalChance = groupChance * itemChance;

                    // average quantity between min and max
                    var avgQty = (item.Min + item.Max) / 2.0;
                    var contribution = totalChance * val * avgQty;
                    // set calculated value on the item object
                    item.Value = contribution;
                    total += contribution;
                }
            }
        }
    }

    // process spoil items: no group chance, use item.Chance directly
    if (npc.DropLists?.Spoil?.Items != null)
    {
        foreach (var sitem in npc.DropLists.Spoil.Items)
        {
            if (string.IsNullOrEmpty(sitem.Name))
                continue;
            if (!itemValues.TryGetValue(sitem.Name, out var sval))
            {
                sval = 0;
                itemValues[sitem.Name] = 0;
            }

            var itemChance = sitem.Chance / 100.0;
            var avgQty = (sitem.Min + sitem.Max) / 2.0;
            var contribution = itemChance * sval * avgQty;
            // set calculated value on the spoil item object
            sitem.Value = contribution;
            spoilTotal += contribution;
        }
    }

    npc.TotalValue = total;
    npc.TotalSpoil = spoilTotal;
    npc.TotalWealth = npc.TotalValue + npc.TotalSpoil;
}

// Sort collected list by TotalValue descending
collected = collected.OrderByDescending(n => n.TotalWealth).ToList();

if (collected.Count > 0)
{
    var top = collected[0];
    Console.WriteLine($"Top NPC by TotalValue: id={top.Id} name={top.Name} totalValue={top.TotalWealth:F2}");
}

// Serialize itemValues back to file so newly discovered items (with 0 value) are persisted.
try
{
    var options = new JsonSerializerOptions { WriteIndented = true };
    var json = JsonSerializer.Serialize(itemValues, options);    
    File.WriteAllText(itemValuesPath, json);
    Console.WriteLine($"Saved item values to {itemValuesPath}");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to save item values: {ex.Message}");
}
