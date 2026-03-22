export const categories = [
  { id: 'all',      label: 'All Resources',       emoji: '✨' },
  { id: 'pdf',      label: 'PDF Files',            emoji: '📄' },
  { id: 'word',     label: 'Word Files',           emoji: '📝' },
  { id: 'picture',  label: 'Pictures & Posters',   emoji: '🖼' },
  { id: 'physical', label: 'Physical Materials',   emoji: '📦' },
]

export const products = [
  // ── PDF ────────────────────────────────────────────────────
  {
    id: 1, name: 'Times Tables Fun Pack', type: 'PDF File', typeEmoji: '📄', category: 'pdf',
    emoji: '➕', bg: 'linear-gradient(135deg,#fff3e0,#ffe0b2)',
    desc: '60 pages of colourful times tables activities, puzzles and games for ages 5–11.',
    price: 3.99, badge: 'Bestseller', stars: '⭐⭐⭐⭐⭐ (142)'
  },
  {
    id: 2, name: 'Alphabet Adventure Workbook', type: 'PDF File', typeEmoji: '📄', category: 'pdf',
    emoji: '🔤', bg: 'linear-gradient(135deg,#e8f5e9,#c8e6c9)',
    desc: 'Letter recognition, tracing and early phonics — perfect for reception starters.',
    price: 2.99, badge: null, stars: '⭐⭐⭐⭐⭐ (89)'
  },
  {
    id: 3, name: 'Science Experiment Sheets', type: 'PDF File', typeEmoji: '📄', category: 'pdf',
    emoji: '🔬', bg: 'linear-gradient(135deg,#e3f2fd,#bbdefb)',
    desc: '20 simple at-home science experiments with observation sheets and discussion questions.',
    price: 4.49, badge: 'New', stars: '⭐⭐⭐⭐ (34)'
  },
  {
    id: 4, name: 'History Timeline Pack — Ancient World', type: 'PDF File', typeEmoji: '📄', category: 'pdf',
    emoji: '🏛', bg: 'linear-gradient(135deg,#fff8e1,#ffecb3)',
    desc: 'Printable timelines, fact cards and colouring pages covering Egypt, Greece & Rome.',
    price: 3.49, badge: null, stars: '⭐⭐⭐⭐ (57)'
  },
  // ── WORD ───────────────────────────────────────────────────
  {
    id: 5, name: 'Creative Writing Prompts', type: 'Word File', typeEmoji: '📝', category: 'word',
    emoji: '✍️', bg: 'linear-gradient(135deg,#fce4ec,#f8bbd0)',
    desc: '50 illustrated story-starter prompts, fully editable so you can personalise for your child.',
    price: 2.49, badge: 'Popular', stars: '⭐⭐⭐⭐⭐ (201)'
  },
  {
    id: 6, name: 'Home Education Planner', type: 'Word File', typeEmoji: '📝', category: 'word',
    emoji: '📅', bg: 'linear-gradient(135deg,#ede7f6,#d1c4e9)',
    desc: 'Weekly lesson planner, progress tracker and curriculum map — editable for any approach.',
    price: 4.99, badge: null, stars: '⭐⭐⭐⭐⭐ (78)'
  },
  {
    id: 7, name: 'Book Report Templates', type: 'Word File', typeEmoji: '📝', category: 'word',
    emoji: '📖', bg: 'linear-gradient(135deg,#e0f7fa,#b2ebf2)',
    desc: 'Five different book report formats from simple to detailed, editable for all ages.',
    price: 1.99, badge: null, stars: '⭐⭐⭐⭐ (45)'
  },
  // ── PICTURES ───────────────────────────────────────────────
  {
    id: 8, name: 'World Map Poster (A2)', type: 'Picture / Poster', typeEmoji: '🖼', category: 'picture',
    emoji: '🗺', bg: 'linear-gradient(135deg,#e8eaf6,#c5cae9)',
    desc: 'Bright and detailed world map with animal illustrations — print at A2 for the wall.',
    price: 2.99, badge: null, stars: '⭐⭐⭐⭐⭐ (115)'
  },
  {
    id: 9, name: 'Alphabet Animal Poster Set', type: 'Picture / Poster', typeEmoji: '🖼', category: 'picture',
    emoji: '🦒', bg: 'linear-gradient(135deg,#fff9c4,#fff59d)',
    desc: '26 adorable animal alphabet posters — A for Armadillo, B for Bear and so on!',
    price: 3.99, badge: 'Bestseller', stars: '⭐⭐⭐⭐⭐ (188)'
  },
  {
    id: 10, name: 'Human Body Chart Pack', type: 'Picture / Poster', typeEmoji: '🖼', category: 'picture',
    emoji: '🫁', bg: 'linear-gradient(135deg,#fce4ec,#ffccbc)',
    desc: 'Labelled diagrams of the skeleton, organs, senses and more. Bright and child-friendly.',
    price: 3.49, badge: 'New', stars: '⭐⭐⭐⭐ (29)'
  },
  // ── PHYSICAL ───────────────────────────────────────────────
  {
    id: 11, name: 'Multiplication Board Game', type: 'Physical', typeEmoji: '📦', category: 'physical',
    emoji: '🎲', bg: 'linear-gradient(135deg,#f3e5f5,#e1bee7)',
    desc: 'Printed & laminated board game for 2–4 players to practise times tables in a fun way.',
    price: 12.99, badge: 'Popular', stars: '⭐⭐⭐⭐⭐ (67)'
  },
  {
    id: 12, name: 'Nature Explore Kit', type: 'Physical', typeEmoji: '📦', category: 'physical',
    emoji: '🌿', bg: 'linear-gradient(135deg,#e8f5e9,#dcedc8)',
    desc: 'Magnifying glass, specimen bags, field journal and ID cards for outdoor adventures.',
    price: 18.99, badge: 'New', stars: '⭐⭐⭐⭐⭐ (41)'
  },
  {
    id: 13, name: 'Fraction Tiles Set', type: 'Physical', typeEmoji: '📦', category: 'physical',
    emoji: '🧩', bg: 'linear-gradient(135deg,#e0f7fa,#b2dfdb)',
    desc: 'Colourful foam fraction tiles — hands-on learning for fractions, decimals and percentages.',
    price: 9.99, badge: null, stars: '⭐⭐⭐⭐ (53)'
  },
  {
    id: 14, name: 'Art & Craft Mega Box', type: 'Physical', typeEmoji: '📦', category: 'physical',
    emoji: '🎨', bg: 'linear-gradient(135deg,#fff3e0,#ffe0b2)',
    desc: 'Monthly themed art project kit: paints, materials and step-by-step guide card included.',
    price: 22.99, badge: 'Bestseller', stars: '⭐⭐⭐⭐⭐ (93)'
  },
]
