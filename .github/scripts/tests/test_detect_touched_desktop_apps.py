import json
import pathlib
import sys
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent.parent))

from detect_touched_desktop_apps import detect_touched_apps, parse_changed_files


def an_app(name, *directories):
    return {"name": name, "directories": list(directories)}


class GivenDesktopApps(unittest.TestCase):
    def setUp(self):
        self.apps = [
            an_app("clock", "apps/desktop/AStarDev.Clock/"),
            an_app("onedrive-sync", "apps/desktop/AStarDev.OneDriveSyncClient/", "apps/desktop/AStarDev.OneDriveSyncClient.TestsUnit/"),
            an_app("scraper", "apps/desktop/Scraper/"),
        ]

    def test_when_one_app_file_changed_then_only_that_app_is_returned(self):
        touched = detect_touched_apps(["apps/desktop/AStarDev.Clock/AStarDev.Clock/App.axaml"], self.apps)

        self.assertEqual(["clock"], [app["name"] for app in touched])

    def test_when_several_apps_changed_then_all_are_returned_in_config_order(self):
        changed = ["apps/desktop/Scraper/AStarDev.WallpaperScraper/Program.cs", "apps/desktop/AStarDev.Clock/Readme.md"]

        touched = detect_touched_apps(changed, self.apps)

        self.assertEqual(["clock", "scraper"], [app["name"] for app in touched])

    def test_when_same_app_changed_many_times_then_it_is_returned_once(self):
        changed = ["apps/desktop/AStarDev.Clock/A.cs", "apps/desktop/AStarDev.Clock/B.cs"]

        touched = detect_touched_apps(changed, self.apps)

        self.assertEqual(1, len(touched))

    def test_when_only_unrelated_files_changed_then_nothing_is_returned(self):
        changed = ["packages/core/AStarDev.Utilities/Strings.cs", "README.md", "apps/web/site/index.ts"]

        touched = detect_touched_apps(changed, self.apps)

        self.assertEqual([], touched)

    def test_when_no_files_changed_then_nothing_is_returned(self):
        self.assertEqual([], detect_touched_apps([], self.apps))

    def test_when_sibling_directory_shares_a_name_prefix_then_it_does_not_match(self):
        changed = ["apps/desktop/AStarDev.OneDriveSyncClient.TestsIntegration/Test.cs"]

        touched = detect_touched_apps(changed, self.apps)

        self.assertEqual([], touched)

    def test_when_an_app_test_project_changed_then_the_app_is_returned(self):
        changed = ["apps/desktop/AStarDev.OneDriveSyncClient.TestsUnit/Test.cs"]

        touched = detect_touched_apps(changed, self.apps)

        self.assertEqual(["onedrive-sync"], [app["name"] for app in touched])

    def test_when_the_matching_app_has_extra_settings_then_they_are_preserved(self):
        apps = [{"name": "clock", "directories": ["apps/desktop/AStarDev.Clock/"], "pack_id": "AStarDev.Clock"}]

        touched = detect_touched_apps(["apps/desktop/AStarDev.Clock/A.cs"], apps)

        self.assertEqual("AStarDev.Clock", touched[0]["pack_id"])


class GivenChangedFileOutput(unittest.TestCase):
    def test_when_output_has_blank_lines_then_they_are_ignored(self):
        self.assertEqual(["a.cs", "b.cs"], parse_changed_files("a.cs\n\nb.cs\n"))

    def test_when_output_is_empty_then_no_files_are_returned(self):
        self.assertEqual([], parse_changed_files(""))

    def test_when_output_has_windows_line_endings_then_paths_are_clean(self):
        self.assertEqual(["a.cs", "b.cs"], parse_changed_files("a.cs\r\nb.cs\r\n"))


class GivenTheDesktopAppsConfig(unittest.TestCase):
    def setUp(self):
        config_path = pathlib.Path(__file__).resolve().parents[2] / "desktop-apps.json"
        self.apps = json.loads(config_path.read_text(encoding="utf-8"))

    def test_when_config_is_loaded_then_all_four_desktop_apps_are_present(self):
        self.assertEqual({"clock", "file-app", "onedrive-sync", "wallpaper-scraper"}, {app["channel_prefix"] for app in self.apps})

    def test_when_config_is_loaded_then_every_app_has_every_release_input(self):
        required = {"display_name", "pack_id", "pack_title", "main_exe", "project_path", "test_project_path", "tag_prefix", "channel_prefix", "directories"}

        for app in self.apps:
            self.assertTrue(required.issubset(app.keys()), app["channel_prefix"])

    def test_when_config_is_loaded_then_every_project_path_exists_in_the_repo(self):
        repo_root = pathlib.Path(__file__).resolve().parents[3]

        for app in self.apps:
            self.assertTrue((repo_root / app["project_path"]).is_file(), app["project_path"])
            self.assertTrue((repo_root / app["test_project_path"]).is_file(), app["test_project_path"])

    def test_when_config_is_loaded_then_project_paths_sit_inside_the_app_directories(self):
        for app in self.apps:
            self.assertTrue(any(app["project_path"].startswith(directory) for directory in app["directories"]), app["channel_prefix"])


if __name__ == "__main__":
    unittest.main()
