<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';

interface Post {
  slug: string;
  title: string;
  date: string;       // ISO date string
  summary: string;
  tags: string[];
  readingTime: number;
}

const props = defineProps<{ posts: Post[] }>();

const activeTag = ref<string | null>(null);

onMounted(() => {
  const params = new URLSearchParams(window.location.search);
  const tag = params.get('tag');
  if (tag && allTags.value.includes(tag)) {
    activeTag.value = tag;
  }
});

const allTags = computed<string[]>(() => {
  const set = new Set<string>();
  for (const post of props.posts) {
    for (const tag of post.tags) set.add(tag);
  }
  return [...set].sort();
});

const filtered = computed(() =>
  activeTag.value
    ? props.posts.filter((p) => p.tags.includes(activeTag.value!))
    : props.posts
);

function selectTag(tag: string) {
  activeTag.value = activeTag.value === tag ? null : tag;
  const url = new URL(window.location.href);
  if (activeTag.value) {
    url.searchParams.set('tag', activeTag.value);
  } else {
    url.searchParams.delete('tag');
  }
  window.history.replaceState({}, '', url.toString());
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
}
</script>

<template>
  <div>
    <!-- Tag filter bar -->
    <nav aria-label="Filter posts by tag" class="tag-filter">
      <ul role="list" class="tag-list">
        <li>
          <button
            :class="['tag-btn', { active: activeTag === null }]"
            :aria-pressed="activeTag === null"
            @click="activeTag = null; $nextTick(() => { const u = new URL(window.location.href); u.searchParams.delete('tag'); window.history.replaceState({}, '', u.toString()); })"
          >
            All
          </button>
        </li>
        <li v-for="tag in allTags" :key="tag">
          <button
            :class="['tag-btn', { active: activeTag === tag }]"
            :aria-pressed="activeTag === tag"
            @click="selectTag(tag)"
          >
            {{ tag }}
          </button>
        </li>
      </ul>
    </nav>

    <!-- Post list -->
    <ul role="list" class="post-list">
      <li v-for="post in filtered" :key="post.slug">
        <a :href="`/blog/${post.slug}`" class="post-card-link">
          <article class="post-card">
            <div class="post-meta">
              <time :datetime="post.date" class="post-date">{{ formatDate(post.date) }}</time>
              <span class="meta-sep" aria-hidden="true">&middot;</span>
              <span class="read-time">{{ post.readingTime }} min read</span>
            </div>
            <h2 class="post-title">{{ post.title }}</h2>
            <p class="post-summary">{{ post.summary }}</p>
            <ul role="list" class="post-tags">
              <li v-for="tag in post.tags" :key="tag" class="post-tag">{{ tag }}</li>
            </ul>
            <span class="read-link">Read post &rarr;</span>
          </article>
        </a>
      </li>
    </ul>

    <p v-if="filtered.length === 0" class="empty-state">
      No posts tagged "{{ activeTag }}".
    </p>
  </div>
</template>

<style scoped>
/* Tag filter */
.tag-filter {
  margin-bottom: 40px;
}

.tag-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  list-style: none;
  margin: 0;
  padding: 0;
}

.tag-btn {
  cursor: pointer;
  padding: 5px 14px;
  border: 1px solid var(--border);
  border-radius: 999px;
  background: var(--surface-raised);
  color: var(--text-muted);
  font-size: 0.85rem;
  font-family: inherit;
  transition: color 0.2s, border-color 0.2s, background-color 0.2s;
}

.tag-btn:hover,
.tag-btn.active {
  color: var(--accent);
  border-color: var(--accent);
}

.tag-btn:focus-visible {
  outline: 2px solid var(--focus-ring);
  outline-offset: 2px;
  border-radius: 999px;
}

/* Post list */
.post-list {
  display: flex;
  flex-direction: column;
  gap: 20px;
  list-style: none;
  margin: 0;
  padding: 0;
}

.post-card-link {
  display: block;
  text-decoration: none;
  border-radius: 12px;
  transition: none;
}

.post-card-link:focus-visible {
  outline: 2px solid var(--focus-ring);
  outline-offset: 2px;
  border-radius: 12px;
}

.post-card-link:hover .post-card {
  border-color: var(--accent);
}

.post-card {
  background: var(--surface-raised);
  border: 1px solid var(--border);
  border-radius: 12px;
  padding: 24px 28px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  transition: background-color 0.25s, border-color 0.25s;
}

.post-meta {
  display: flex;
  align-items: center;
  gap: 8px;
}

.post-date,
.read-time,
.meta-sep {
  font-size: 0.82rem;
  color: var(--text-muted);
  transition: color 0.25s;
}

.post-title {
  font-size: 1.1rem;
  font-weight: 600;
  color: var(--text);
  transition: color 0.25s;
}

.post-summary {
  font-size: 0.9rem;
  color: var(--text-muted);
  line-height: 1.6;
  transition: color 0.25s;
}

.post-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  list-style: none;
  margin: 0;
  padding: 0;
}

.post-tag {
  border: 1px solid var(--border);
  color: var(--text-muted);
  font-size: 0.75rem;
  border-radius: 999px;
  padding: 3px 10px;
  transition: border-color 0.25s, color 0.25s;
}

.read-link {
  font-size: 0.85rem;
  color: var(--accent);
  transition: color 0.25s;
}

.empty-state {
  color: var(--text-muted);
  font-size: 0.95rem;
  padding: 32px 0;
  transition: color 0.25s;
}
</style>
