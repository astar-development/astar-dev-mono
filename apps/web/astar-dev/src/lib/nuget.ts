import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'node:fs';
import { join } from 'node:path';

export interface PackageData {
  id: string;
  version: string;
  description: string;
  totalDownloads: number;
  projectUrl: string;
}

const CACHE_DIR = join(process.cwd(), '.cache', 'nuget');
const NUGET_SEARCH_BASE = 'https://azuresearch-usnc.nuget.org/query';

function cachePathFor(packageId: string): string {
  return join(CACHE_DIR, `${packageId.toLowerCase()}.json`);
}

function readCache(packageId: string): PackageData | null {
  const path = cachePathFor(packageId);
  if (!existsSync(path)) return null;
  try {
    return JSON.parse(readFileSync(path, 'utf-8')) as PackageData;
  } catch {
    return null;
  }
}

function writeCache(data: PackageData): void {
  try {
    mkdirSync(CACHE_DIR, { recursive: true });
    writeFileSync(cachePathFor(data.id), JSON.stringify(data, null, 2), 'utf-8');
  } catch {
    // Non-fatal — continue without caching
  }
}

async function fetchFromNuGet(packageId: string): Promise<PackageData | null> {
  const url = `${NUGET_SEARCH_BASE}?q=packageid:${encodeURIComponent(packageId)}&prerelease=false&take=1`;
  try {
    const res = await fetch(url, { signal: AbortSignal.timeout(8000) });
    if (!res.ok) return null;

    const json = (await res.json()) as {
      data: Array<{
        id: string;
        version: string;
        description: string;
        totalDownloads: number;
        projectUrl?: string;
      }>;
    };

    const entry = json.data?.find(
      (d) => d.id.toLowerCase() === packageId.toLowerCase()
    );
    if (!entry) return null;

    return {
      id: entry.id,
      version: entry.version,
      description: entry.description ?? '',
      totalDownloads: entry.totalDownloads ?? 0,
      projectUrl: entry.projectUrl ?? `https://www.nuget.org/packages/${entry.id}`,
    };
  } catch {
    return null;
  }
}

export async function getPackageData(packageId: string): Promise<PackageData> {
  const live = await fetchFromNuGet(packageId);

  if (live) {
    writeCache(live);
    return live;
  }

  const cached = readCache(packageId);
  if (cached) {
    console.warn(`[nuget] NuGet API unreachable for "${packageId}" — using cached data.`);
    return cached;
  }

  throw new Error(
    `[nuget] Cannot fetch "${packageId}" from NuGet and no cache exists. ` +
    `Run the build with network access at least once to populate the cache.`
  );
}

export async function getFeaturedPackages(ids: string[]): Promise<PackageData[]> {
  return Promise.all(ids.map((id) => getPackageData(id)));
}

export function formatDownloads(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`;
  return n.toLocaleString('en-GB');
}
