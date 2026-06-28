#!/usr/bin/env python3
"""Generate TCC runtime databases from the Elinu Classic+ datacenter."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
import xml.etree.ElementTree as ET
from pathlib import Path


LANGUAGES = {
    "EU-EN": "DataCenter_Final_EUR",
    "EU-FR": "DataCenter_Final_FRA",
    "EU-GER": "DataCenter_Final_GER",
    "RU": "DataCenter_Final_RUS",
}

DATABASE_HASH_FILE_NAME = "database-hashes.json"
OUTPUT_KEEP_NAMES = {".git", ".github", "README.md", DATABASE_HASH_FILE_NAME, "opcodes"}
MAX_CHARACTER_LEVEL = 65
DISABLED_CONTENT_RE = re.compile(
    "(?:apex|awaken|apog(?:e|\u00e9)e|(?:\u00e9|e)veil|erweck|erwach|\u043f\u0440\u043e\u0431\u0443\u0436)",
    re.IGNORECASE,
)

CLASS_ALIASES = {
    "elementalist": "Mystic",
    "engineer": "Gunner",
    "soulless": "Reaper",
    "fighter": "Brawler",
    "assassin": "Ninja",
    "glaiver": "Valkyrie",
}

EFFECT_TYPE_NAMES = {
    "1": "Power",
    "2": "Endurance",
    "3": "Speed",
    "4": "Stun",
    "5": "Balance",
    "6": "AttackSpeed",
    "7": "CritFactor",
    "8": "CritResist",
    "9": "PeriodicDamage",
    "10": "PeriodicHeal",
    "11": "PeriodicMp",
    "12": "Aggro",
    "13": "DamageAbsorb",
}

PLACEHOLDER_RE = re.compile(r"\$(value|tickInterval|time)(\d*)")
CALC_VALUE_RE = re.compile(r"\$calcValue\(([^)]*)\)")
UNRESOLVED_DATACENTER_TOKEN_RE = re.compile(r"\$(?:calcValue|value\d*|tickInterval\d*|time\d*)")
DELTA_PERCENT_EFFECT_TYPES = {"162"}
PARAMETER_VALUE_EFFECT_TYPES = {"1003"}


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def xml_files(table_dir: Path) -> list[Path]:
    return sorted(path for path in table_dir.glob("*.xml") if not path.name.endswith(".xsd"))


def elements(dc_dir: Path, table: str, element_name: str):
    table_dir = dc_dir / table
    if not table_dir.is_dir():
        return
    for path in xml_files(table_dir):
        root = ET.parse(path).getroot()
        for element in root.iter():
            if local_name(element.tag) == element_name:
                yield element


def clean(value: str | None) -> str:
    if value is None:
        return ""
    return value.replace("\t", " ").replace("\r\n", "\n").replace("\r", "\n").replace("\n", "&#xA")


def to_int(value: str | None, default: int = 0) -> int:
    try:
        return int(value or default)
    except ValueError:
        return default


def to_rounded_int(value: str | None, default: int = 0) -> int:
    try:
        return int(round(float(value or default)))
    except ValueError:
        return default


def normalize_bool(value: str | None) -> str:
    return "True" if str(value).lower() == "true" else "False"


def normalize_abnormality_visibility(value: str | None) -> str:
    return "True" if str(value).lower() in {"true", "onlyicon"} else "False"


def format_number(value: str | None) -> str:
    if value is None or value == "":
        return "0"
    try:
        parsed = float(value)
    except ValueError:
        return value

    if parsed.is_integer():
        return str(int(parsed))
    return f"{parsed:.6f}".rstrip("0").rstrip(".")


def format_effect_value(value: str | None) -> str:
    if value is None or value == "":
        return "0"
    try:
        parsed = float(value)
    except ValueError:
        return value

    if -3 < parsed < 0:
        return f"{format_number(str(abs(parsed) * 100))}%"
    if 0 < parsed < 0.5:
        return f"{format_number(str(parsed * 100))}%"
    if 0.5 <= parsed < 1:
        return f"{format_number(str((1 - parsed) * 100))}%"
    if 1 < parsed < 3:
        return f"{format_number(str((parsed - 1) * 100))}%"
    return format_number(value)


def format_percent(value: float) -> str:
    return f"{format_number(str(value))}%"


def format_time_ms(value: str | None) -> str:
    milliseconds = to_int(value)
    if milliseconds <= 0:
        return "0"
    seconds = milliseconds // 1000
    if seconds and milliseconds % 1000 == 0:
        if seconds % 3600 == 0:
            return f"{seconds // 3600}h"
        if seconds % 60 == 0:
            return f"{seconds // 60}min"
        return f"{seconds}s"
    return format_number(value)


def find_effect(effects: list[ET.Element], effect_type: str) -> ET.Element | None:
    return next((effect for effect in effects if effect.get("type") == effect_type), None)


def parse_calc_decimal(whole: str | None, fraction: str | None) -> float:
    left = (whole or "0").strip()
    right = (fraction or "0").strip()
    if "." in left:
        return float(left)
    sign = "-"
    if left.startswith("-"):
        left = left[1:]
    else:
        sign = ""
    return float(f"{sign}{left}.{right}")


def format_calc_value(mode: str, effect_type: str, parameter: float, effects: list[ET.Element]) -> str:
    effect = find_effect(effects, effect_type)
    raw_value = effect.get("value") if effect is not None else None

    try:
        effect_value = float(raw_value) if raw_value not in (None, "") else parameter
    except ValueError:
        return raw_value or format_number(str(parameter))

    if mode == "heal":
        return format_number(str(abs(effect_value)))

    if mode == "multiple":
        if effect_type in PARAMETER_VALUE_EFFECT_TYPES and (raw_value in (None, "") or effect_value == 0):
            return format_effect_value(str(parameter))
        if effect_type in DELTA_PERCENT_EFFECT_TYPES and 0.5 <= effect_value <= 3:
            return format_percent((effect_value - 1) * 100 * parameter)
        if -3 < effect_value < 0:
            return format_percent(abs(effect_value) * 100 * parameter)
        return format_number(str(abs(effect_value) * parameter))

    return format_number(str(effect_value))


def replace_tooltip_placeholders(tooltip: str, abnormal_time: str | None, effects: list[ET.Element]) -> str:
    if "$" not in tooltip:
        return tooltip

    def calc_value_replacement(match: re.Match[str]) -> str:
        args = [arg.strip() for arg in match.group(1).split(",")]
        if len(args) != 4:
            return match.group(0)

        mode = args[0]
        effect_type = args[1]
        parameter = parse_calc_decimal(args[2], args[3])
        return format_calc_value(mode, effect_type, parameter, effects)

    def replacement(match: re.Match[str]) -> str:
        token = match.group(1)
        index = to_int(match.group(2), 1) - 1

        if token == "time":
            return format_time_ms(abnormal_time)

        if index < 0:
            index = 0
        if not effects:
            return "0"
        if index >= len(effects):
            index = len(effects) - 1

        effect = effects[index]
        if token == "tickInterval":
            return format_number(effect.get("tickInterval"))
        return format_effect_value(effect.get("value"))

    tooltip = CALC_VALUE_RE.sub(calc_value_replacement, tooltip)
    return PLACEHOLDER_RE.sub(replacement, tooltip)


def normalize_class(value: str | None) -> str:
    if not value:
        return ""
    low = value.lower()
    if low in CLASS_ALIASES:
        return CLASS_ALIASES[low]
    return value[:1].upper() + value[1:].lower()


def has_disabled_content(*values: object) -> bool:
    return any(DISABLED_CONTENT_RE.search(str(value or "")) for value in values)


def row_has_disabled_content(row: list[object]) -> bool:
    return has_disabled_content(*row)


def is_over_level_cap(element: ET.Element) -> bool:
    for attr in ("requiredLevel", "level"):
        if attr in element.attrib and to_int(element.get(attr)) > MAX_CHARACTER_LEVEL:
            return True
    return False


def string_table(dc_dir: Path, table: str, text_attr: str = "string") -> dict[int, str]:
    out: dict[int, str] = {}
    for string in elements(dc_dir, table, "String"):
        key = to_int(string.get("id"))
        value = clean(string.get(text_attr))
        if key and value:
            out[key] = value
    return out


def resolve_ref(value: str | None, strings: dict[int, str]) -> tuple[int, str]:
    if not value:
        return 0, ""
    match = re.search(r":(\d+)$", value)
    if not match:
        return 0, clean(value)
    key = int(match.group(1))
    return key, strings.get(key, "")


def write_tsv(path: Path, rows: list[list[object]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    rows = [row for row in rows if not row_has_disabled_content(row)]
    text = "".join("\t".join(str(col) for col in row) + "\n" for row in rows)
    path.write_text(text, encoding="utf-8", newline="\n")


def write_xml(path: Path, root: ET.Element) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    ET.indent(root, space="  ")
    tree = ET.ElementTree(root)
    tree.write(path, encoding="utf-8", xml_declaration=True)


def build_simple_strsheet(dc_dir: Path, out_dir: Path, folder: str, lang: str, table: str) -> None:
    rows = [[key, value] for key, value in sorted(string_table(dc_dir, table).items())]
    write_tsv(out_dir / folder / f"{folder}-{lang}.tsv", rows)


def build_achievement_grades(dc_dir: Path, out_dir: Path, lang: str) -> None:
    rows = [
        [key, value]
        for key, value in sorted(string_table(dc_dir, "StrSheet_AchievementGradeInfo").items())
        if 100 <= key <= 106
    ]
    write_tsv(out_dir / "achi_grade" / f"achi_grade-{lang}.tsv", rows)


def build_achievements(dc_dir: Path, out_dir: Path, lang: str) -> None:
    strings = string_table(dc_dir, "StrSheet_Achievement")
    rows: list[list[object]] = []
    for achievement in elements(dc_dir, "AchievementList", "Achievement"):
        achievement_id = to_int(achievement.get("id"))
        name_id, name = resolve_ref(achievement.get("name"), strings)
        if achievement_id and name_id and name:
            rows.append([achievement_id, name_id, name])
    rows.sort(key=lambda row: int(row[0]))
    write_tsv(out_dir / "achievements" / f"achievements-{lang}.tsv", rows)


def build_guild_quests(dc_dir: Path, out_dir: Path, lang: str) -> None:
    rows = [
        [key, value]
        for key, value in sorted(string_table(dc_dir, "StrSheet_GuildQuest").items())
        if key % 2 == 1
    ]
    write_tsv(out_dir / "guild_quests" / f"guild_quests-{lang}.tsv", rows)


def build_social(dc_dir: Path, out_dir: Path, lang: str) -> None:
    rows = [
        [key, value]
        for key, value in sorted(string_table(dc_dir, "StrSheet_Social").items())
        if value.startswith("{")
    ]
    write_tsv(out_dir / "social" / f"social-{lang}.tsv", rows)


def build_system_messages(dc_dir: Path, out_dir: Path, lang: str) -> None:
    rows: list[list[object]] = []
    for msg in elements(dc_dir, "StrSheet_SystemMessage", "String"):
        channel = msg.get("chatChannel")
        readable_id = msg.get("readableId")
        text = clean(msg.get("string"))
        if channel and readable_id and text:
            rows.append([channel, readable_id, text])
    write_tsv(out_dir / "sys_msg" / f"sys_msg-{lang}.tsv", rows)


def build_items(dc_dir: Path, out_dir: Path, lang: str) -> None:
    names = string_table(dc_dir, "StrSheet_Item")
    rows: list[list[object]] = []
    for item in elements(dc_dir, "ItemData", "Item"):
        item_id = to_int(item.get("id"))
        name = names.get(item_id, "")
        if not item_id or not name:
            continue
        if is_over_level_cap(item) or has_disabled_content(name, *item.attrib.values()):
            continue
        rows.append(
            [
                item_id,
                to_int(item.get("rareGrade")),
                name,
                to_int(item.get("coolTime")),
                clean(item.get("icon")).lower(),
            ]
        )
    rows.sort(key=lambda row: int(row[0]))
    write_tsv(out_dir / "items" / f"items-{lang}.tsv", rows)


KNOWN_CLASSES = {
    "warrior": "Warrior",
    "lancer": "Lancer",
    "slayer": "Slayer",
    "berserker": "Berserker",
    "sorcerer": "Sorcerer",
    "archer": "Archer",
    "priest": "Priest",
    "mystic": "Mystic",
    "reaper": "Reaper",
    "gunner": "Gunner",
    "brawler": "Brawler",
    "ninja": "Ninja",
    "valkyrie": "Valkyrie",
    **CLASS_ALIASES,
}


def skill_key(skill_id: int, race: str, gender: str, klass: str) -> tuple[int, str, str, str]:
    return skill_id, race, gender, normalize_class(klass)


def is_apex_skill(name: str, tooltip: str, icon: str) -> bool:
    haystack = f"{name} {tooltip} {icon}".lower()
    return has_disabled_content(haystack)


def class_from_skill_data_name(name: str | None) -> str:
    if not name:
        return ""
    for part in re.split(r"[_\s]+", name):
        klass = KNOWN_CLASSES.get(part.lower())
        if klass:
            return klass
    return ""


def build_skills(dc_dir: Path, out_dir: Path, lang: str) -> None:
    icons: dict[tuple[int, str, str, str], str] = {}
    for icon in elements(dc_dir, "SkillIconData", "Icon"):
        key = skill_key(
            to_int(icon.get("skillId")),
            icon.get("race") or "",
            icon.get("gender") or "",
            icon.get("class") or "",
        )
        icon_name = clean(icon.get("iconName")).lower()
        if key[0] and icon_name:
            icons[key] = icon_name

    rows: list[list[object]] = []
    for skill in elements(dc_dir, "StrSheet_UserSkill", "String"):
        skill_id = to_int(skill.get("id"))
        race = skill.get("race") or ""
        gender = skill.get("gender") or ""
        klass = normalize_class(skill.get("class"))
        name = clean(skill.get("name"))
        tooltip = clean(skill.get("tooltip"))
        if not skill_id or not race or not gender or not klass or not name:
            continue
        icon_name = icons.get((skill_id, race, gender, klass), "")
        if not icon_name or is_apex_skill(name, tooltip, icon_name):
            continue
        rows.append([skill_id, race, gender, klass, name, "", "", icon_name])

    rows_by_key = {(int(row[0]), str(row[1]), str(row[2]), str(row[3])): row for row in rows}
    visible_rows_by_skill = {}
    for row in rows:
        visible_rows_by_skill.setdefault((int(row[0]), str(row[3])), []).append(row)

    for skill in elements(dc_dir, "SkillData", "Skill"):
        skill_id = to_int(skill.get("id"))
        if not skill_id or skill_id % 100 == 0:
            continue

        visible_id = skill_id - skill_id % 100
        klass = class_from_skill_data_name(skill.get("name"))
        if not klass:
            continue

        for visible_row in visible_rows_by_skill.get((visible_id, klass), []):
            key = (skill_id, str(visible_row[1]), str(visible_row[2]), klass)
            if key in rows_by_key:
                continue

            alias_row = [skill_id, visible_row[1], visible_row[2], klass, visible_row[4], "", "", visible_row[7]]
            rows.append(alias_row)
            rows_by_key[key] = alias_row

    rows.sort(key=lambda row: (str(row[3]), int(row[0]), str(row[1]), str(row[2])))
    write_tsv(out_dir / "skills" / f"skills-{lang}.tsv", rows)


def abnormality_type(property_value: str | None, is_buff: str | None) -> str:
    prop = to_int(property_value)
    if prop == 2:
        return "DOT"
    if prop == 3:
        return "Stun"
    if prop == 4 or str(is_buff).lower() == "true":
        return "Buff"
    return "Debuff"


def build_hotdot(dc_dir: Path, out_dir: Path, lang: str) -> None:
    strings: dict[int, tuple[str, str]] = {}
    for entry in elements(dc_dir, "StrSheet_Abnormality", "String"):
        key = to_int(entry.get("id"))
        if key:
            strings[key] = (clean(entry.get("name")), clean(entry.get("tooltip")))

    icons: dict[int, str] = {}
    for icon in elements(dc_dir, "AbnormalityIconData", "Icon"):
        key = to_int(icon.get("abnormalityId"))
        icon_name = clean(icon.get("iconName")).lower()
        if key and icon_name:
            icons[key] = icon_name

    rows: list[list[object]] = []
    for abnormal in elements(dc_dir, "Abnormality", "Abnormal"):
        abnormality_id = to_int(abnormal.get("id"))
        if not abnormality_id:
            continue
        name, tooltip = strings.get(abnormality_id, ("", ""))
        icon_name = icons.get(abnormality_id, "")
        effects = [x for x in abnormal if local_name(x.tag) == "AbnormalityEffect"]
        resolved_tooltip = replace_tooltip_placeholders(tooltip, abnormal.get("time"), effects)
        for effect in effects or [None]:
            effect_type_id = effect.get("type") if effect is not None else "0"
            effect_type = EFFECT_TYPE_NAMES.get(effect_type_id or "0", effect_type_id or "Unknown")
            value = effect.get("value") if effect is not None else "0"
            rows.append(
                [
                    abnormality_id,
                    effect_type,
                    abnormality_type(abnormal.get("property"), abnormal.get("isBuff")),
                    normalize_bool(abnormal.get("infinity")),
                    "seta",
                    abnormal.get("time") or "0",
                    effect.get("tickInterval") if effect is not None else "0",
                    value or "0",
                    name,
                    abnormal.get("kind") or "",
                    name,
                    resolved_tooltip,
                    icon_name,
                    icon_name,
                    normalize_abnormality_visibility(abnormal.get("isShow")),
                ]
            )
    rows.sort(key=lambda row: int(row[0]))
    write_tsv(out_dir / "hotdot" / f"hotdot-{lang}.tsv", rows)


def build_dungeons(dc_dir: Path, out_dir: Path, lang: str) -> dict[int, str]:
    names = string_table(dc_dir, "StrSheet_Dungeon")
    costs = {
        to_int(c.get("continentId")): to_int(c.get("requiredActPoint"))
        for c in elements(dc_dir, "DungeonConstraint", "Constraint")
    }
    rows = [[key, value, costs.get(key, 0)] for key, value in sorted(names.items()) if key in costs]
    write_tsv(out_dir / "dungeons" / f"dungeons-{lang}.tsv", rows)
    return dict(names)


def build_default_dungeon_defs(dc_dir: Path, out_dir: Path, dungeon_names: dict[int, str]) -> None:
    rows: list[list[object]] = []
    index = 0
    for dungeon in elements(dc_dir, "DungeonMatching", "Dungeon"):
        dungeon_id = to_int(dungeon.get("id"))
        if not dungeon_id or dungeon_id not in dungeon_names:
            continue
        item_level = to_int(dungeon.get("minItemLevel"))
        short_name = dungeon.get("name") or dungeon_names[dungeon_id]
        rows.append([dungeon_id, clean(short_name), 1, item_level, "False", index, "Daily"])
        index += 1
    write_tsv(out_dir / "default-dungeon-defs.tsv", rows)


def map_icon_name(map_id: str) -> str:
    name = re.sub(r"^WMap_", "", map_id or "")
    name = re.sub(r"[^A-Za-z0-9]+", "_", name).strip("_").lower()
    return name or "unknown"


def build_section_images(dc_dir: Path, out_dir: Path) -> None:
    rows: list[list[object]] = []
    for world in elements(dc_dir, "NewWorldMapData", "World"):
        world_id = to_int(world.get("id"))
        for guard in [x for x in world if local_name(x.tag) == "Guard"]:
            guard_id = to_int(guard.get("id"))
            for section in [x for x in guard if local_name(x.tag) == "Section"]:
                section_id = to_int(section.get("id"))
                if world_id and guard_id and section_id:
                    rows.append([world_id, guard_id, section_id, map_icon_name(section.get("mapId") or "")])
    write_tsv(out_dir / "section_images.tsv", rows)


def build_world_map(dc_dir: Path, out_dir: Path, lang: str) -> None:
    root = ET.Element("WorldMap")
    for source_world in elements(dc_dir, "NewWorldMapData", "World"):
        world_attrs = {
            "id": source_world.get("id") or "0",
            "mapId": source_world.get("mapId") or "",
            "nameId": source_world.get("nameId") or "0",
        }
        world = ET.SubElement(root, "World", world_attrs)
        for source_guard in [x for x in source_world if local_name(x.tag) == "Guard"]:
            guard_attrs = {
                "id": source_guard.get("id") or "0",
                "mapId": source_guard.get("mapId") or "",
                "nameId": source_guard.get("nameId") or "0",
                "continentId": source_guard.get("continentId") or "0",
            }
            guard = ET.SubElement(world, "Guard", guard_attrs)
            for source_section in [x for x in source_guard if local_name(x.tag) == "Section"]:
                section_attrs = {
                    "id": source_section.get("id") or "0",
                    "mapId": source_section.get("mapId") or "",
                    "nameId": source_section.get("nameId") or "0",
                }
                if source_section.get("type"):
                    section_attrs["type"] = source_section.get("type") or ""
                ET.SubElement(guard, "Section", section_attrs)
    write_xml(out_dir / "world_map" / f"world_map-{lang}.xml", root)


def build_monsters(dc_dir: Path, out_dir: Path, lang: str) -> None:
    template_data: dict[tuple[int, int], tuple[str, int, int, int]] = {}
    for npc_file in xml_files(dc_dir / "NpcData"):
        root = ET.parse(npc_file).getroot()
        hunting_zone_id = to_int(root.get("huntingZoneId"))
        for npc in root.iter():
            if local_name(npc.tag) != "Template":
                continue
            template_id = to_int(npc.get("id"))
            stat = next((x for x in npc if local_name(x.tag) == "Stat"), None)
            anger = next((x for x in npc if local_name(x.tag) == "Anger"), None)
            if template_id:
                template_data[(hunting_zone_id, template_id)] = (
                    normalize_bool(npc.get("elite")),
                    to_int(npc.get("speciesId")),
                    to_rounded_int(stat.get("maxHp") if stat is not None else None),
                    to_rounded_int(anger.get("gaugeSize") if anger is not None else None),
                )

    names: dict[tuple[int, int], str] = {}
    for creature_file in xml_files(dc_dir / "StrSheet_Creature"):
        root = ET.parse(creature_file).getroot()
        for zone in root.iter():
            if local_name(zone.tag) != "HuntingZone":
                continue
            zone_id = to_int(zone.get("id"))
            for string in [x for x in zone if local_name(x.tag) == "String"]:
                template_id = to_int(string.get("templateId"))
                name = clean(string.get("name"))
                if template_id and name:
                    names[(zone_id, template_id)] = name

    zones: dict[int, list[tuple[int, str, str, int, int, int]]] = {}
    for key, name in names.items():
        zone_id, template_id = key
        elite, species, hp, enrage_hp = template_data.get(key, ("False", 0, 0, 0))
        zones.setdefault(zone_id, []).append((template_id, name, elite, species, hp, enrage_hp))

    root = ET.Element("Zones")
    for zone_id in sorted(zones):
        zone = ET.SubElement(root, "Zone", {"id": str(zone_id), "name": f"zone {zone_id}"})
        for template_id, name, elite, species, hp, enrage_hp in sorted(zones[zone_id], key=lambda row: row[0]):
            ET.SubElement(
                zone,
                "Monster",
                {
                    "name": name,
                    "id": str(template_id),
                    "isBoss": elite,
                    "hp": str(hp),
                    "enrageHp": str(enrage_hp),
                    "speciesId": str(species),
                },
            )
    write_xml(out_dir / "monsters" / f"monsters-{lang}.xml", root)


def build_monster_override(out_dir: Path) -> None:
    write_xml(out_dir / "monsters" / "monsters-override.xml", ET.Element("Zones"))


def build_servers_file(out_dir: Path) -> None:
    (out_dir / "servers.txt").write_text("", encoding="utf-8")


def build_equip_exp(dc_dir: Path, out_dir: Path, lang: str) -> None:
    root = ET.Element("EquipmentExpData")
    for exp in elements(dc_dir, "EquipmentExpData", "EquipmentExp"):
        exp_out = ET.SubElement(root, "EquipmentExp", {"id": exp.get("id") or "0"})
        for child in [x for x in exp if local_name(x.tag) == "Exp"]:
            ET.SubElement(
                exp_out,
                "Exp",
                {
                    "enchantStep": child.get("enchantStep") or "0",
                    "maxExp": child.get("maxExp") or "0",
                    "supExp": child.get("supExp") or "0",
                },
            )
    write_xml(out_dir / "equip_exp" / f"equip_exp-{lang}.xml", root)


def build_language(dc_dir: Path, out_dir: Path, lang: str) -> dict[int, str]:
    build_simple_strsheet(dc_dir, out_dir, "acc_benefits", lang, "StrSheet_AccountBenefit")
    build_simple_strsheet(dc_dir, out_dir, "quests", lang, "StrSheet_Quest")
    build_simple_strsheet(dc_dir, out_dir, "regions", lang, "StrSheet_Region")
    build_achievement_grades(dc_dir, out_dir, lang)
    build_achievements(dc_dir, out_dir, lang)
    build_guild_quests(dc_dir, out_dir, lang)
    build_social(dc_dir, out_dir, lang)
    build_system_messages(dc_dir, out_dir, lang)
    build_items(dc_dir, out_dir, lang)
    build_skills(dc_dir, out_dir, lang)
    build_hotdot(dc_dir, out_dir, lang)
    dungeon_names = build_dungeons(dc_dir, out_dir, lang)
    build_world_map(dc_dir, out_dir, lang)
    build_monsters(dc_dir, out_dir, lang)
    build_equip_exp(dc_dir, out_dir, lang)
    return dungeon_names


def clean_output(out_dir: Path) -> None:
    for child in out_dir.iterdir() if out_dir.exists() else []:
        if child.name in OUTPUT_KEEP_NAMES:
            continue
        if child.is_dir():
            shutil.rmtree(child)
        else:
            child.unlink()


def element_has_disabled_content(element: ET.Element) -> bool:
    return has_disabled_content(element.text, *element.attrib.values())


def remove_disabled_xml_elements(parent: ET.Element) -> None:
    for child in list(parent):
        if element_has_disabled_content(child):
            parent.remove(child)
            continue
        remove_disabled_xml_elements(child)


def scrub_tsv(path: Path) -> None:
    lines = path.read_text(encoding="utf-8").splitlines()
    kept = [line for line in lines if not has_disabled_content(line)]
    path.write_text("".join(f"{line}\n" for line in kept), encoding="utf-8", newline="\n")


def scrub_xml(path: Path) -> None:
    tree = ET.parse(path)
    root = tree.getroot()
    remove_disabled_xml_elements(root)
    ET.indent(root, space="  ")
    tree.write(path, encoding="utf-8", xml_declaration=True)


def scrub_disabled_content(out_dir: Path) -> None:
    for path in sorted(out_dir.rglob("*")):
        if not path.is_file():
            continue
        suffix = path.suffix.lower()
        if suffix == ".tsv":
            scrub_tsv(path)
        elif suffix == ".xml":
            scrub_xml(path)


def audit_generated_output(out_dir: Path) -> None:
    failures: list[str] = []
    for path in sorted(out_dir.rglob("*")):
        if not path.is_file():
            continue
        rel = path.relative_to(out_dir).as_posix()
        if rel == DATABASE_HASH_FILE_NAME or rel.startswith(".git/") or rel.startswith(".github/"):
            continue
        if path.suffix.lower() not in {".tsv", ".xml", ".txt"}:
            continue

        text = path.read_text(encoding="utf-8")
        for line_no, line in enumerate(text.splitlines(), 1):
            if UNRESOLVED_DATACENTER_TOKEN_RE.search(line):
                failures.append(f"{rel}:{line_no}: {line[:160]}")
                if len(failures) >= 20:
                    break

    if failures:
        details = "\n".join(failures)
        raise ValueError(f"Generated data contains unresolved datacenter placeholders:\n{details}")


def write_hashes(out_dir: Path, hashes_path: Path) -> None:
    hashes: dict[str, str] = {}
    for path in sorted(p for p in out_dir.rglob("*") if p.is_file()):
        rel = path.relative_to(out_dir).as_posix()
        if (
            rel == DATABASE_HASH_FILE_NAME
            or rel == "README.md"
            or rel.startswith(".git/")
            or rel.startswith(".github/")
            or rel.startswith("opcodes/")
        ):
            continue
        hashes[rel] = hashlib.sha256(path.read_bytes()).hexdigest()
    text = json.dumps(hashes, indent=2, ensure_ascii=False) + "\n"
    hashes_path.write_text(text, encoding="utf-8")

    hosted_hashes_path = out_dir / DATABASE_HASH_FILE_NAME
    if hosted_hashes_path.resolve() != hashes_path.resolve():
        hosted_hashes_path.write_text(text, encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--elinu-root", required=True, help="Path to the Classic+ repository root.")
    parser.add_argument("--out-dir", default=r"TCC.Core\resources\data")
    parser.add_argument("--hashes", default="database-hashes.json")
    args = parser.parse_args()

    elinu_root = Path(args.elinu_root).resolve()
    client_dir = elinu_root / "Client"
    out_dir = Path(args.out_dir).resolve()
    hashes_path = Path(args.hashes).resolve()

    clean_output(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    first_dungeon_names: dict[int, str] | None = None
    for lang, dc_name in LANGUAGES.items():
        dc_dir = client_dir / dc_name
        if not dc_dir.is_dir():
            raise FileNotFoundError(dc_dir)
        dungeon_names = build_language(dc_dir, out_dir, lang)
        if first_dungeon_names is None:
            first_dungeon_names = dungeon_names
            build_default_dungeon_defs(dc_dir, out_dir, dungeon_names)
            build_section_images(dc_dir, out_dir)

    build_monster_override(out_dir)
    build_servers_file(out_dir)
    scrub_disabled_content(out_dir)
    audit_generated_output(out_dir)
    write_hashes(out_dir, hashes_path)
    print(f"Generated Classic+ TCC data in {out_dir}")
    print(f"Wrote hashes to {hashes_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
