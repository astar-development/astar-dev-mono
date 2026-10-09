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


if __name__ == "__main__":
    unittest.main()
