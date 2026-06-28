import tempfile
import unittest
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_tcc_classicplus_data as generator


class ClassicPlusGeneratorTests(unittest.TestCase):
    def test_build_items_removes_apex_awaken_and_over_level_cap_items(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            dc_dir = root / "dc"
            out_dir = root / "out"
            (dc_dir / "StrSheet_Item").mkdir(parents=True)
            (dc_dir / "ItemData").mkdir(parents=True)

            (dc_dir / "StrSheet_Item" / "StrSheet_Item-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<StrSheet_Item>
  <String id="100" string="Health Potion" />
  <String id="101" string="Apex Scroll" />
  <String id="102" string="Level 66 Charm" />
  <String id="103" string="Twin Axe" />
</StrSheet_Item>
""",
                encoding="utf-8",
            )
            (dc_dir / "ItemData" / "ItemData-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<ItemData>
  <Item id="100" rareGrade="1" coolTime="30" icon="Icon_Items.Potion_Tex" requiredLevel="65" />
  <Item id="101" rareGrade="1" coolTime="0" icon="Icon_Items.Scroll_Tex" requiredLevel="65" />
  <Item id="102" rareGrade="1" coolTime="0" icon="Icon_Items.Charm_Tex" requiredLevel="66" />
  <Item id="103" name="awaken_TwoHandAxe" rareGrade="3" coolTime="0" icon="Icon_Equipments.Axe_Tex" requiredLevel="34" />
</ItemData>
""",
                encoding="utf-8",
            )

            generator.build_items(dc_dir, out_dir, "EU-EN")

            generated = (out_dir / "items" / "items-EU-EN.tsv").read_text(encoding="utf-8")
            self.assertIn("100\t1\tHealth Potion\t30\ticon_items.potion_tex", generated)
            self.assertNotIn("Apex", generated)
            self.assertNotIn("Level 66", generated)
            self.assertNotIn("Twin Axe", generated)

    def test_build_skills_adds_packet_child_aliases_from_skill_data(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            dc_dir = root / "dc"
            out_dir = root / "out"
            (dc_dir / "SkillIconData").mkdir(parents=True)
            (dc_dir / "StrSheet_UserSkill").mkdir(parents=True)
            (dc_dir / "SkillData").mkdir(parents=True)

            (dc_dir / "SkillIconData" / "SkillIconData-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<SkillIconData>
  <Icon skillId="30800" race="Common" gender="Common" class="Assassin" iconName="Icon_Skills.C12_RapidShot" />
</SkillIconData>
""",
                encoding="utf-8",
            )
            (dc_dir / "StrSheet_UserSkill" / "StrSheet_UserSkill-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<StrSheet_UserSkill>
  <String id="30800" race="Common" gender="Common" class="Assassin" name="Leaves on the Wind VIII" tooltip="Throw spikes." />
</StrSheet_UserSkill>
""",
                encoding="utf-8",
            )
            (dc_dir / "SkillData" / "SkillData-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<SkillData>
  <Skill id="30830" name="Popori_M_Assassin_Lv08_Rapid Shot_Continuous" />
  <Skill id="30820" name="Popori_M_Assassin_Lv08_Rapid Shot Projectile" />
</SkillData>
""",
                encoding="utf-8",
            )

            generator.build_skills(dc_dir, out_dir, "EU-EN")

            generated = (out_dir / "skills" / "skills-EU-EN.tsv").read_text(encoding="utf-8")
            self.assertIn(
                "30800\tCommon\tCommon\tNinja\tLeaves on the Wind VIII\t\t\ticon_skills.c12_rapidshot\n",
                generated,
            )
            self.assertIn(
                "30820\tCommon\tCommon\tNinja\tLeaves on the Wind VIII\t\t\ticon_skills.c12_rapidshot\n",
                generated,
            )
            self.assertIn(
                "30830\tCommon\tCommon\tNinja\tLeaves on the Wind VIII\t\t\ticon_skills.c12_rapidshot\n",
                generated,
            )

    def test_build_monsters_uses_elinu_hp_and_anger_gauge(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            dc_dir = root / "dc"
            out_dir = root / "out"
            (dc_dir / "NpcData").mkdir(parents=True)
            (dc_dir / "StrSheet_Creature").mkdir(parents=True)

            (dc_dir / "NpcData" / "NpcData-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<NpcData huntingZoneId="950">
  <Template id="1000" elite="true" speciesId="7">
    <Anger gaugeSize="151234.349719337" />
    <Stat level="65" maxHp="302468.699438673" />
  </Template>
</NpcData>
""",
                encoding="utf-8",
            )
            (dc_dir / "StrSheet_Creature" / "StrSheet_Creature-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<StrSheet_Creature>
  <HuntingZone id="950">
    <String templateId="1000" name="Classic Boss" />
  </HuntingZone>
</StrSheet_Creature>
""",
                encoding="utf-8",
            )

            generator.build_monsters(dc_dir, out_dir, "EU-EN")

            generated = (out_dir / "monsters" / "monsters-EU-EN.xml").read_text(encoding="utf-8")
            self.assertIn('name="Classic Boss"', generated)
            self.assertIn('hp="302469"', generated)
            self.assertIn('enrageHp="151234"', generated)

    def test_build_hotdot_resolves_datacenter_tooltip_placeholders(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            dc_dir = root / "dc"
            out_dir = root / "out"
            (dc_dir / "StrSheet_Abnormality").mkdir(parents=True)
            (dc_dir / "AbnormalityIconData").mkdir(parents=True)
            (dc_dir / "Abnormality").mkdir(parents=True)

            (dc_dir / "StrSheet_Abnormality" / "StrSheet_Abnormality-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<StrSheet_Abnormality>
  <String id="4830" name="Bravery" tooltip="Increases skill damage by $H_W_GOOD$value2$COLOR_END. Additionally increases Attack Speed by $H_W_GOOD$value$COLOR_END." />
</StrSheet_Abnormality>
""",
                encoding="utf-8",
            )
            (dc_dir / "AbnormalityIconData" / "AbnormalityIconData-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<AbnormalityIconData>
  <Icon abnormalityId="4830" iconName="Icon_Items.Potion10_Tex" />
</AbnormalityIconData>
""",
                encoding="utf-8",
            )
            (dc_dir / "Abnormality" / "Abnormality-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<Abnormality>
  <Abnormal id="4830" property="4" isBuff="true" infinity="false" time="1800000" kind="44802" isShow="true">
    <AbnormalityEffect type="24" value="1.04" tickInterval="0" />
    <AbnormalityEffect type="162" value="1.1" tickInterval="0" />
  </Abnormal>
</Abnormality>
""",
                encoding="utf-8",
            )

            generator.build_hotdot(dc_dir, out_dir, "EU-EN")

            generated = (out_dir / "hotdot" / "hotdot-EU-EN.tsv").read_text(encoding="utf-8")
            self.assertIn(
                "Increases skill damage by $H_W_GOOD10%$COLOR_END. Additionally increases Attack Speed by $H_W_GOOD4%$COLOR_END.",
                generated,
            )
            self.assertNotIn("$value", generated)

    def test_build_hotdot_resolves_tick_interval_and_time_placeholders(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            dc_dir = root / "dc"
            out_dir = root / "out"
            (dc_dir / "StrSheet_Abnormality").mkdir(parents=True)
            (dc_dir / "AbnormalityIconData").mkdir(parents=True)
            (dc_dir / "Abnormality").mkdir(parents=True)

            (dc_dir / "StrSheet_Abnormality" / "StrSheet_Abnormality-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<StrSheet_Abnormality>
  <String id="905" name="Regeneration" tooltip="Replenishes MP by $H_W_GOOD$value$COLOR_END every $H_W_GOOD$tickInterval$COLOR_END for $time." />
</StrSheet_Abnormality>
""",
                encoding="utf-8",
            )
            (dc_dir / "AbnormalityIconData" / "AbnormalityIconData-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<AbnormalityIconData>
  <Icon abnormalityId="905" iconName="Icon_Status.PlusMp_Tex" />
</AbnormalityIconData>
""",
                encoding="utf-8",
            )
            (dc_dir / "Abnormality" / "Abnormality-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<Abnormality>
  <Abnormal id="905" property="4" isBuff="true" infinity="false" time="10000" kind="48005" isShow="true">
    <AbnormalityEffect type="52" value="100" tickInterval="2" />
  </Abnormal>
</Abnormality>
""",
                encoding="utf-8",
            )

            generator.build_hotdot(dc_dir, out_dir, "EU-EN")

            generated = (out_dir / "hotdot" / "hotdot-EU-EN.tsv").read_text(encoding="utf-8")
            self.assertIn(
                "Replenishes MP by $H_W_GOOD100$COLOR_END every $H_W_GOOD2$COLOR_END for 10s.",
                generated,
            )
            self.assertNotIn("$tickInterval", generated)
            self.assertNotIn("$time", generated)

    def test_build_hotdot_treats_only_icon_abnormalities_as_visible(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            dc_dir = root / "dc"
            out_dir = root / "out"
            (dc_dir / "StrSheet_Abnormality").mkdir(parents=True)
            (dc_dir / "AbnormalityIconData").mkdir(parents=True)
            (dc_dir / "Abnormality").mkdir(parents=True)

            (dc_dir / "StrSheet_Abnormality" / "StrSheet_Abnormality-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<StrSheet_Abnormality>
  <String id="32058" name="Glyph of Blaze" tooltip="Speeds casting of Burning Heart and Fire Avalanche by $H_W_GOOD$value$COLOR_END." />
</StrSheet_Abnormality>
""",
                encoding="utf-8",
            )
            (dc_dir / "AbnormalityIconData" / "AbnormalityIconData-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<AbnormalityIconData>
  <Icon abnormalityId="32058" iconName="Icon_Crest.crestnextskillattackspeedup_Tex" />
</AbnormalityIconData>
""",
                encoding="utf-8",
            )
            (dc_dir / "Abnormality" / "Abnormality-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<Abnormality>
  <Abnormal id="32058" property="4" isBuff="true" infinity="false" time="10000" kind="19212" isShow="onlyIcon">
    <AbnormalityEffect type="235" value="1.3" tickInterval="0" />
  </Abnormal>
</Abnormality>
""",
                encoding="utf-8",
            )

            generator.build_hotdot(dc_dir, out_dir, "EU-EN")

            generated = (out_dir / "hotdot" / "hotdot-EU-EN.tsv").read_text(encoding="utf-8")
            self.assertIn(
                "32058\t235\tBuff\tFalse\tseta\t10000\t0\t1.3\tGlyph of Blaze\t19212\tGlyph of Blaze\t"
                "Speeds casting of Burning Heart and Fire Avalanche by $H_W_GOOD30%$COLOR_END.\t"
                "icon_crest.crestnextskillattackspeedup_tex\ticon_crest.crestnextskillattackspeedup_tex\tTrue\n",
                generated,
            )

    def test_build_hotdot_uses_last_effect_for_out_of_range_tooltip_placeholders(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            dc_dir = root / "dc"
            out_dir = root / "out"
            (dc_dir / "StrSheet_Abnormality").mkdir(parents=True)
            (dc_dir / "AbnormalityIconData").mkdir(parents=True)
            (dc_dir / "Abnormality").mkdir(parents=True)

            (dc_dir / "StrSheet_Abnormality" / "StrSheet_Abnormality-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<StrSheet_Abnormality>
  <String id="43100007" name="Bleeding" tooltip="Reduces HP every $H_W_BAD$tickInterval2$COLOR_END and then $H_W_BAD$value2$COLOR_END." />
</StrSheet_Abnormality>
""",
                encoding="utf-8",
            )
            (dc_dir / "AbnormalityIconData" / "AbnormalityIconData-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<AbnormalityIconData>
  <Icon abnormalityId="43100007" iconName="Icon_Status.Bleed_Tex" />
</AbnormalityIconData>
""",
                encoding="utf-8",
            )
            (dc_dir / "Abnormality" / "Abnormality-00000.xml").write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<Abnormality>
  <Abnormal id="43100007" property="2" isBuff="false" infinity="false" time="15000" kind="25608" isShow="true">
    <AbnormalityEffect type="51" value="-0.05" tickInterval="3" />
  </Abnormal>
</Abnormality>
""",
                encoding="utf-8",
            )

            generator.build_hotdot(dc_dir, out_dir, "EU-EN")

            generated = (out_dir / "hotdot" / "hotdot-EU-EN.tsv").read_text(encoding="utf-8")
            self.assertIn(
                "Reduces HP every $H_W_BAD3$COLOR_END and then $H_W_BAD5%$COLOR_END.",
                generated,
            )
            self.assertNotIn("$tickInterval", generated)
            self.assertNotIn("$value", generated)

    def test_scrub_generated_output_removes_apex_awaken_terms_from_tsv_and_xml(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            out_dir = Path(temp_dir)
            tsv_path = out_dir / "items" / "items-EU-EN.tsv"
            xml_path = out_dir / "monsters" / "monsters-EU-EN.xml"
            tsv_path.parent.mkdir(parents=True)
            xml_path.parent.mkdir(parents=True)

            tsv_path.write_text(
                "100\t1\tHealth Potion\t30\ticon_items.potion_tex\n"
                "101\t1\tApex Scroll\t0\ticon_items.scroll_tex\n",
                encoding="utf-8",
            )
            xml_path.write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<Zones>
  <Zone id="1" name="zone 1">
    <Monster name="Argon" id="100" />
    <Monster name="Death Awakener" id="101" />
  </Zone>
</Zones>
""",
                encoding="utf-8",
            )

            generator.scrub_disabled_content(out_dir)

            self.assertEqual(
                "100\t1\tHealth Potion\t30\ticon_items.potion_tex\n",
                tsv_path.read_text(encoding="utf-8"),
            )
            xml_text = xml_path.read_text(encoding="utf-8")
            self.assertIn("Argon", xml_text)
            self.assertNotIn("Death Awakener", xml_text)

    def test_clean_output_preserves_repo_metadata_and_readme(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            out_dir = Path(temp_dir)
            for keep_name in [".git", ".github", "opcodes"]:
                (out_dir / keep_name).mkdir()
            (out_dir / "README.md").write_text("docs", encoding="utf-8")
            (out_dir / "database-hashes.json").write_text("{}", encoding="utf-8")
            (out_dir / "old").mkdir()
            (out_dir / "old.txt").write_text("old", encoding="utf-8")

            generator.clean_output(out_dir)

            self.assertTrue((out_dir / ".git").is_dir())
            self.assertTrue((out_dir / ".github").is_dir())
            self.assertTrue((out_dir / "opcodes").is_dir())
            self.assertTrue((out_dir / "README.md").is_file())
            self.assertTrue((out_dir / "database-hashes.json").is_file())
            self.assertFalse((out_dir / "old").exists())
            self.assertFalse((out_dir / "old.txt").exists())

    def test_write_hashes_updates_packaged_and_hosted_hash_files_without_hashing_itself(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            out_dir = root / "out"
            out_dir.mkdir()
            (out_dir / "items").mkdir()
            (out_dir / ".git").mkdir()
            (out_dir / ".github").mkdir()
            (out_dir / "opcodes").mkdir()
            (out_dir / "items" / "items-EU-EN.tsv").write_text("100\tPotion\n", encoding="utf-8")
            (out_dir / ".git" / "index").write_text("local git state", encoding="utf-8")
            (out_dir / ".github" / "workflow.yml").write_text("ci", encoding="utf-8")
            (out_dir / "opcodes" / "protocol.map").write_text("C_CHECK_VERSION 1", encoding="utf-8")
            (out_dir / "README.md").write_text("docs", encoding="utf-8")
            (out_dir / "database-hashes.json").write_text("stale", encoding="utf-8")
            hashes_path = root / "packaged-hashes.json"

            generator.write_hashes(out_dir, hashes_path)

            packaged_hashes = hashes_path.read_text(encoding="utf-8")
            hosted_hashes = (out_dir / "database-hashes.json").read_text(encoding="utf-8")
            self.assertEqual(packaged_hashes, hosted_hashes)
            self.assertIn("items/items-EU-EN.tsv", packaged_hashes)
            self.assertNotIn("database-hashes.json", packaged_hashes)
            self.assertNotIn(".git/", packaged_hashes)
            self.assertNotIn(".github/", packaged_hashes)
            self.assertNotIn("opcodes/", packaged_hashes)
            self.assertNotIn("README.md", packaged_hashes)


if __name__ == "__main__":
    unittest.main()
