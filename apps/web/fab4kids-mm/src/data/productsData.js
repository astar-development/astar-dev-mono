export const categories = [
  'All',
  'Worksheet Packs',
  'Practical Life',
  'Sensorial Materials',
  'Language & Literacy',
  'Maths & Geometry'
];

export const ageGroups = [
  'All Ages',
  '0-2 years',
  '3-5 years',
  '6-8 years',
  '9-10 years'
];

export const products = [
  // Worksheet Packs
  {
    id: 1,
    name: 'Printable Sandpaper Letter Tracing Set',
    category: 'Worksheet Packs',
    price: 9.99,
    ageGroup: '3-5 years',
    description: 'A downloadable PDF pack with textured-style tracing cards, sound prompts, and quick lesson scripts for home or classroom use.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 2,
    name: 'Montessori Practical Life Checklist Bundle',
    category: 'Worksheet Packs',
    price: 11.99,
    ageGroup: '3-5 years',
    description: 'Editable routine charts and observation checklists that align with pouring, spooning, folding, and care-of-environment activities.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 3,
    name: 'Lower Elementary Grammar Symbol Worksheets',
    category: 'Worksheet Packs',
    price: 14.99,
    ageGroup: '6-8 years',
    description: 'Colour-coded printable grammar symbol cards with sentence analysis templates and extension work for independent stations.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 4,
    name: 'Continents and Biomes Research Pack',
    category: 'Worksheet Packs',
    price: 13.49,
    ageGroup: '9-10 years',
    description: 'A printable geography project set featuring map work, biome comparison sheets, and short writing prompts for guided research.',
    image: '/logo.png',
    inStock: true
  },

  // Practical Life
  {
    id: 5,
    name: 'Pouring and Transferring Tray Set',
    category: 'Practical Life',
    price: 27.99,
    ageGroup: '3-5 years',
    description: 'Solid beechwood tray with two ceramic pitchers, transfer spoons, and starter lesson cards to build hand control and concentration.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 6,
    name: 'Child-Sized Dressing Frame Trio',
    category: 'Practical Life',
    price: 39.99,
    ageGroup: '3-5 years',
    description: 'Three classroom dressing frames (zip, buckle, and button) with reinforced cotton panels and presentation sequence guide.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 7,
    name: 'Table Washing Work Station',
    category: 'Practical Life',
    price: 44.5,
    ageGroup: '6-8 years',
    description: 'Compact table washing station with basin, pitcher, soap dish, and laminated process cards for independent practical work.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 8,
    name: 'Grace and Courtesy Role-Play Cards',
    category: 'Practical Life',
    price: 12.49,
    ageGroup: '0-2 years',
    description: 'Illustrated prompts for greetings, interruptions, snack table routines, and conflict resolution language in toddler communities.',
    image: '/logo.png',
    inStock: true
  },

  // Sensorial Materials
  {
    id: 9,
    name: 'Pink Tower Hardwood Set',
    category: 'Sensorial Materials',
    price: 69.99,
    ageGroup: '3-5 years',
    description: 'Ten precision-cut cubes finished in classroom pink lacquer for visual discrimination and introductory volume exploration.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 10,
    name: 'Broad Stair (Brown Stair) Classroom Edition',
    category: 'Sensorial Materials',
    price: 74.99,
    ageGroup: '3-5 years',
    description: 'Ten varnished prisms for grading by thickness and constructing comparison patterns, supplied with control layout card.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 11,
    name: 'Colour Tablets Box 2 and 3 Combo',
    category: 'Sensorial Materials',
    price: 58.0,
    ageGroup: '6-8 years',
    description: 'Advanced grading tablets with protective case for colour matching, sequencing, and descriptive vocabulary development.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 12,
    name: 'Sound Cylinders and Mystery Bag Kit',
    category: 'Sensorial Materials',
    price: 36.25,
    ageGroup: '3-5 years',
    description: 'A listening and tactile discrimination set with matching sound cylinders, mystery bag objects, and extension cards.',
    image: '/logo.png',
    inStock: true
  },

  // Language & Literacy
  {
    id: 13,
    name: 'Moveable Alphabet with Storage Box',
    category: 'Language & Literacy',
    price: 52.99,
    ageGroup: '3-5 years',
    description: 'Durable wooden letter set with colour-coded consonants and vowels, designed for phonetic word building before pencil writing.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 14,
    name: 'Phonics Command Cards - Printable Deck',
    category: 'Language & Literacy',
    price: 10.5,
    ageGroup: '6-8 years',
    description: 'Download-ready command cards for action reading, including blended sounds and digraph practice with auto-check pages.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 15,
    name: 'Reading Analysis Symbol Mat',
    category: 'Language & Literacy',
    price: 18.99,
    ageGroup: '9-10 years',
    description: 'Large felt analysis mat with grammar symbol tokens to support sentence study and advanced composition lessons.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 16,
    name: 'Object Boxes for Early Sounds',
    category: 'Language & Literacy',
    price: 31.99,
    ageGroup: '6-8 years',
    description: 'Three graded object boxes with miniature items for first sounds, ending sounds, and blending practice.',
    image: '/logo.png',
    inStock: true
  },

  // Maths & Geometry
  {
    id: 17,
    name: 'Golden Bead Introduction Set',
    category: 'Maths & Geometry',
    price: 82.99,
    ageGroup: '3-5 years',
    description: 'Unit, ten, hundred, and thousand bead materials for decimal hierarchy presentations and concrete quantity exploration.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 18,
    name: 'Bead Stair and Hanging Chains',
    category: 'Maths & Geometry',
    price: 63.5,
    ageGroup: '6-8 years',
    description: 'A combined set for skip counting and squaring/cubing patterns, including labels and control chart cards.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 19,
    name: 'Fraction Circle Inset Trays',
    category: 'Maths & Geometry',
    price: 33.99,
    ageGroup: '9-10 years',
    description: 'Colour-coded wooden fraction circles with work cards that bridge concrete manipulation to worksheet notation.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 20,
    name: 'Geometry Cabinet Card Companion (Digital)',
    category: 'Maths & Geometry',
    price: 8.49,
    ageGroup: '6-8 years',
    description: 'Printable naming cards and matching worksheet extensions designed to pair with the classroom geometry cabinet.',
    image: '/logo.png',
    inStock: true
  }
];
