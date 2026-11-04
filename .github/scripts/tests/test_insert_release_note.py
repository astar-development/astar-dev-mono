import pathlib
import subprocess
import sys
import tempfile
import unittest
import xml.dom.minidom

SCRIPT = pathlib.Path(__file__).resolve().parent.parent / "insert_release_note.py"

PROJECT = "<Project>\n    <PropertyGroup>\n        <Authors>Someone</Authors>\n    </PropertyGroup>\n</Project>\n"


def run_insert(title):
    with tempfile.TemporaryDirectory() as directory:
        csproj = pathlib.Path(directory) / "Sample.csproj"
        csproj.write_text(PROJECT, encoding="utf-8")
        subprocess.run([sys.executable, "-I", str(SCRIPT), str(csproj), "1.2.3", title], check=True, capture_output=True)

        return csproj.read_text(encoding="utf-8")


class GivenATitleWithXmlSpecialCharacters(unittest.TestCase):
    def test_when_the_title_contains_angle_brackets_then_the_csproj_is_still_valid_xml(self):
        text = run_insert("fix(db): map NULL Option<string> columns & more")

        xml.dom.minidom.parseString(text)

    def test_when_the_title_contains_angle_brackets_then_the_entry_is_escaped(self):
        text = run_insert("fix(db): map Option<string>")

        self.assertIn("v1.2.3 fix(db): map Option&lt;string&gt;", text)

    def test_when_the_same_title_is_inserted_twice_then_the_second_run_is_unchanged(self):
        with tempfile.TemporaryDirectory() as directory:
            csproj = pathlib.Path(directory) / "Sample.csproj"
            csproj.write_text(PROJECT, encoding="utf-8")
            command = [sys.executable, "-I", str(SCRIPT), str(csproj), "1.2.3", "fix: Option<string> & more"]
            subprocess.run(command, check=True, capture_output=True)

            second = subprocess.run(command, check=True, capture_output=True, text=True)

        self.assertEqual("unchanged", second.stdout.strip())


class GivenAnEntryForTheSameVersionWithAnOlderTitle(unittest.TestCase):
    def run_title_edit(self):
        with tempfile.TemporaryDirectory() as directory:
            csproj = pathlib.Path(directory) / "Sample.csproj"
            csproj.write_text(PROJECT, encoding="utf-8")
            subprocess.run([sys.executable, "-I", str(SCRIPT), str(csproj), "1.2.2", "fix: earlier release"], check=True, capture_output=True)
            subprocess.run([sys.executable, "-I", str(SCRIPT), str(csproj), "1.2.3", "feat: first title"], check=True, capture_output=True)
            subprocess.run([sys.executable, "-I", str(SCRIPT), str(csproj), "1.2.3", "feat: edited title"], check=True, capture_output=True)

            return csproj.read_text(encoding="utf-8")

    def test_when_the_title_is_edited_then_the_version_appears_once(self):
        text = self.run_title_edit()

        self.assertEqual(1, text.count("v1.2.3 "))

    def test_when_the_title_is_edited_then_the_edited_title_is_the_top_entry(self):
        text = self.run_title_edit()

        self.assertIn("<PackageReleaseNotes>\n        v1.2.3 feat: edited title\n        v1.2.2 fix: earlier release\n", text)

    def test_when_the_title_is_edited_then_a_similar_longer_version_is_kept(self):
        with tempfile.TemporaryDirectory() as directory:
            csproj = pathlib.Path(directory) / "Sample.csproj"
            csproj.write_text(PROJECT, encoding="utf-8")
            subprocess.run([sys.executable, "-I", str(SCRIPT), str(csproj), "1.2.30", "fix: other version"], check=True, capture_output=True)
            subprocess.run([sys.executable, "-I", str(SCRIPT), str(csproj), "1.2.3", "feat: new"], check=True, capture_output=True)

            text = csproj.read_text(encoding="utf-8")

        self.assertIn("v1.2.30 fix: other version", text)


if __name__ == "__main__":
    unittest.main()
