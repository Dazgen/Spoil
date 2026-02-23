using System;
using System.Collections.Generic;
using System.Xml.Serialization;

[XmlRoot("list")]
public class NpcList
{
    [XmlElement("npc")]
    public List<Npc> Npcs { get; set; }
}

public class Npc
{
    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlAttribute("level")]
    public int Level { get; set; }

    [XmlAttribute("type")]
    public string Type { get; set; }

    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlElement("race")]
    public string Race { get; set; }

    [XmlElement("sex")]
    public string Sex { get; set; }

    [XmlElement("stats")]
    public Stats Stats { get; set; }

    [XmlElement("status")]
    public Status Status { get; set; }

    [XmlElement("skillList")]
    public SkillList SkillList { get; set; }

    [XmlElement("shots")]
    public Shots Shots { get; set; }

    [XmlElement("dropLists")]
    public DropLists DropLists { get; set; }

    [XmlElement("corpseTime")]
    public long CorpseTime { get; set; }

    [XmlElement("exCrtEffect")]
    public bool ExCrtEffect { get; set; }

    [XmlElement("collision")]
    public Collision Collision { get; set; }

    // Calculated total expected value of drops (populated at runtime)
    [XmlIgnore]
    public double TotalValue { get; set; }

    [XmlIgnore]
    public double TotalSpoil { get; set; }

    [XmlIgnore]
    public double TotalWealth { get; set; }
}

public class Stats
{
    [XmlAttribute("str")]
    public int Str { get; set; }

    [XmlAttribute("int")]
    public int Int { get; set; }

    [XmlAttribute("dex")]
    public int Dex { get; set; }

    [XmlAttribute("wit")]
    public int Wit { get; set; }

    [XmlAttribute("con")]
    public int Con { get; set; }

    [XmlAttribute("men")]
    public int Men { get; set; }

    [XmlElement("vitals")]
    public Vitals Vitals { get; set; }

    [XmlElement("attack")]
    public Attack Attack { get; set; }

    [XmlElement("defence")]
    public Defence Defence { get; set; }

    [XmlElement("speed")]
    public Speed Speed { get; set; }

    [XmlElement("hitTime")]
    public int HitTime { get; set; }
}

public class Vitals
{
    [XmlAttribute("hp")]
    public double Hp { get; set; }

    [XmlAttribute("hpRegen")]
    public double HpRegen { get; set; }

    [XmlAttribute("mp")]
    public double Mp { get; set; }

    [XmlAttribute("mpRegen")]
    public double MpRegen { get; set; }
}

public class Attack
{
    [XmlAttribute("physical")]
    public double Physical { get; set; }

    [XmlAttribute("magical")]
    public double Magical { get; set; }

    [XmlAttribute("random")]
    public double Random { get; set; }

    [XmlAttribute("critical")]
    public double Critical { get; set; }

    [XmlAttribute("accuracy")]
    public double Accuracy { get; set; }

    [XmlAttribute("attackSpeed")]
    public int AttackSpeed { get; set; }

    [XmlAttribute("type")]
    public string Type { get; set; }

    [XmlAttribute("range")]
    public int Range { get; set; }

    [XmlAttribute("distance")]
    public int Distance { get; set; }

    [XmlAttribute("width")]
    public int Width { get; set; }
}

public class Defence
{
    [XmlAttribute("physical")]
    public double Physical { get; set; }

    [XmlAttribute("magical")]
    public double Magical { get; set; }
}

public class Speed
{
    [XmlElement("walk")]
    public Walk Walk { get; set; }

    [XmlElement("run")]
    public Run Run { get; set; }
}

public class Walk
{
    [XmlAttribute("ground")]
    public int Ground { get; set; }
}

public class Run
{
    [XmlAttribute("ground")]
    public int Ground { get; set; }
}

public class Status
{
    [XmlAttribute("undying")]
    public bool Undying { get; set; }
}

public class SkillList
{
    [XmlElement("skill")]
    public List<Skill> Skills { get; set; }
}

public class Skill
{
    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlAttribute("level")]
    public int Level { get; set; }
}

public class Shots
{
    [XmlAttribute("soul")]
    public int Soul { get; set; }

    [XmlAttribute("spirit")]
    public int Spirit { get; set; }
}

public class DropLists
{
    [XmlElement("drop")]
    public List<Drop> Drop { get; set; }

    [XmlElement("spoil")]
    public Spoil Spoil { get; set; }
}

public class Drop
{
    [XmlElement("group")]
    public List<Group> Groups { get; set; }
}

public class Group
{
    [XmlAttribute("chance")]
    public double Chance { get; set; }

    [XmlElement("item")]
    public List<DropItem> Items { get; set; }
}

public class DropItem
{
    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlAttribute("min")]
    public int Min { get; set; }

    [XmlAttribute("max")]
    public int Max { get; set; }

    [XmlAttribute("chance")]
    public double Chance { get; set; }

    // Comment in XML holds the human-readable name for the item (e.g. "Recipe: Great Sword").
    // This value is not part of attributes and will be populated during parsing.
    [XmlIgnore]
    public string? Name { get; set; }

    // Calculated monetary value for this specific item occurrence (populated at runtime)
    [XmlIgnore]
    public double Value { get; set; }
}

public class Spoil
{
    [XmlElement("item")]
    public List<DropItem> Items { get; set; }
}

public class Collision
{
    [XmlElement("radius")]
    public Radius Radius { get; set; }

    [XmlElement("height")]
    public Height Height { get; set; }
}

public class Radius
{
    [XmlAttribute("normal")]
    public double Normal { get; set; }
}

public class Height
{
    [XmlAttribute("normal")]
    public double Normal { get; set; }
}
