import pathlib
import sys
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent.parent))

from compute_next_desktop_version import compute_next_version, latest_stable_version, bump_kind, split_messages

PREFIX = "clock-v"


class GivenCommitMessages(unittest.TestCase):
    def test_when_message_is_a_feature_then_bump_is_minor(self):
        self.assertEqual("minor", bump_kind(["feat(clock): add alarms (#12)"]))

    def test_when_message_is_a_fix_then_bump_is_patch(self):
        self.assertEqual("patch", bump_kind(["fix(clock): stop drift (#13)"]))

    def test_when_message_is_not_conventional_then_bump_is_patch(self):
        self.assertEqual("patch", bump_kind(["Update the thing"]))

    def test_when_subject_has_a_bang_then_bump_is_major(self):
        self.assertEqual("major", bump_kind(["feat(clock)!: drop settings file (#14)"]))

    def test_when_body_has_a_breaking_change_footer_then_bump_is_major(self):
        self.assertEqual("major", bump_kind(["fix(clock): rename option (#15)\n\nBREAKING CHANGE: option renamed"]))

    def test_when_messages_mix_types_then_the_highest_bump_wins(self):
        self.assertEqual("minor", bump_kind(["fix: a", "feat: b", "chore: c"]))

    def test_when_there_are_no_messages_then_bump_is_patch(self):
        self.assertEqual("patch", bump_kind([]))

    def test_when_subject_mentions_feat_only_in_prose_then_bump_is_patch(self):
        self.assertEqual("patch", bump_kind(["chore: document feat: usage"]))

    def test_when_text_is_nul_separated_then_it_is_split_into_messages(self):
        self.assertEqual(["feat: a\n\nbody", "fix: b"], split_messages("feat: a\n\nbody\0fix: b\0\n"))

    def test_when_text_is_empty_then_no_messages_are_returned(self):
        self.assertEqual([], split_messages(""))


class GivenExistingTags(unittest.TestCase):
    def test_when_there_are_no_tags_then_there_is_no_latest_version(self):
        self.assertIsNone(latest_stable_version([], PREFIX))

    def test_when_tags_exist_then_the_highest_numeric_version_is_latest(self):
        tags = ["clock-v0.9.0", "clock-v0.10.0", "clock-v0.2.5"]

        self.assertEqual((0, 10, 0), latest_stable_version(tags, PREFIX))

    def test_when_prerelease_tags_exist_then_they_are_ignored(self):
        tags = ["clock-v1.0.0", "clock-v2.0.0-beta.1"]

        self.assertEqual((1, 0, 0), latest_stable_version(tags, PREFIX))

    def test_when_tags_belong_to_other_apps_then_they_are_ignored(self):
        tags = ["file-app-v9.9.9", "clock-v0.1.0", "clock-vnext"]

        self.assertEqual((0, 1, 0), latest_stable_version(tags, PREFIX))

    def test_when_a_longer_prefix_shares_the_start_then_it_is_ignored(self):
        self.assertIsNone(latest_stable_version(["onedrive-sync-client-v5.0.0"], "onedrive-sync-v"))


class GivenANextVersionRequest(unittest.TestCase):
    def test_when_no_tag_exists_then_the_first_version_is_0_1_0(self):
        self.assertEqual("0.1.0", compute_next_version([], PREFIX, ["feat: first"]))

    def test_when_a_feature_is_merged_then_minor_is_bumped_and_patch_reset(self):
        self.assertEqual("0.35.0", compute_next_version(["clock-v0.34.7"], PREFIX, ["feat: x"]))

    def test_when_a_fix_is_merged_then_patch_is_bumped(self):
        self.assertEqual("0.34.8", compute_next_version(["clock-v0.34.7"], PREFIX, ["fix: x"]))

    def test_when_a_breaking_change_is_merged_then_major_is_bumped_and_the_rest_reset(self):
        self.assertEqual("1.0.0", compute_next_version(["clock-v0.34.7"], PREFIX, ["feat!: x"]))


if __name__ == "__main__":
    unittest.main()
