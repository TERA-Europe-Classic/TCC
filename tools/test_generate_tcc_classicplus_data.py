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
            (out_dir / "old").mkdir()
            (out_dir / "old.txt").write_text("old", encoding="utf-8")

            generator.clean_output(out_dir)

            self.assertTrue((out_dir / ".git").is_dir())
            self.assertTrue((out_dir / ".github").is_dir())
            self.assertTrue((out_dir / "opcodes").is_dir())
            self.assertTrue((out_dir / "README.md").is_file())
            self.assertFalse((out_dir / "old").exists())
            self.assertFalse((out_dir / "old.txt").exists())


if __name__ == "__main__":
    unittest.main()
