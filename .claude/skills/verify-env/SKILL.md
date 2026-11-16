---
name: verify-env
description: Check workstation prerequisites after an OS rebuild - gcc, uv Python, GPG signing key, gh auth scopes, drive mounts, libssl, docker/libvirt. Use for "verify env", at session start after a Fedora reinstall.
---

Run each check, report pass/fail table, fix nothing without asking:

1. `command -v gcc`
2. `uv python list --only-installed` - tool installs pinned with `--python 3.13`
3. `gpg --list-secret-keys` and `git config user.signingkey`; gpg-agent running
4. `gh auth status` - scopes include `repo`, `delete_repo`
5. `findmnt /mnt/tbdrive` - mounted by UUID, not under `/run/media`
6. `ldconfig -p | grep libssl`
7. `systemctl is-active docker libvirtd`; VM networking not blocked by Docker iptables rules
8. `git config core.fileMode`
9. `command -v uvx`; `grep -E '^agent_interface:' ~/.serena/serena_config.yml` is `tools` not `REPL`; repo `.mcp.json` has `--project-from-cwd`; newest `~/.serena/logs/*/mcp_*.txt` says "Number of exposed tools: 29"
