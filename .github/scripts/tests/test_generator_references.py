import pathlib
import re
import unittest

REPO_ROOT = pathlib.Path(__file__).resolve().parents[3]
GENERATOR_CALL = re.compile(r'<MSBuild\s+Projects="[^"]*SourceGenerator[^"]*"[^>]*Properties="([^"]*)"')


class GivenProjectsThatLoadSourceGeneratorsAsAnalyzers(unittest.TestCase):
    def test_when_published_for_a_runtime_then_the_generator_lookup_ignores_the_runtime(self):
        calls = [(project, properties) for project in sorted(REPO_ROOT.glob("apps/**/*.csproj")) for properties in GENERATOR_CALL.findall(project.read_text(encoding="utf-8"))]

        self.assertTrue(calls)
        for project, properties in calls:
            with self.subTest(project=str(project.relative_to(REPO_ROOT))):
                self.assertIn("RuntimeIdentifier=;", properties + ";")
                self.assertIn("SelfContained=false", properties)


if __name__ == "__main__":
    unittest.main()
