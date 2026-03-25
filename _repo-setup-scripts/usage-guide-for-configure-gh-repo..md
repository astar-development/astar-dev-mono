# How to use

## Before running

```bash
chmod +x configure-gh-repo.sh
```

Make sure you’re authenticated with gh auth login and have repo scope.

## How to run

Run either:

- Inside a cloned repo:

```bash
    ./configure-gh-repo.sh
```

- Or explicitly:

```bash
    ./configure-gh-repo.sh owner/repo
```

### Dry Run

Whichever approach you decide to use for running this script, you can add:

```bash
 --dry-run
```
