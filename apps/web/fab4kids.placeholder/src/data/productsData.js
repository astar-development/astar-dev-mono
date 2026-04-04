export const categories = [
  'All',
  'STEM Toys',
  'Art Supplies',
  'Books',
  'Outdoor Equipment',
  'Musical Instruments'
];

export const ageGroups = [
  'All Ages',
  '0-2 years',
  '3-5 years',
  '6-8 years',
  '9-10 years'
];

export const products = [
  // STEM Toys
  {
    id: 1,
    name: 'Colourful Building Blocks Set',
    category: 'STEM Toys',
    price: 24.99,
    ageGroup: '3-5 years',
    description: 'A vibrant set of 100 building blocks in various shapes and colours. Perfect for developing fine motor skills, spatial awareness, and creativity. Made from child-safe, durable plastic.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 2,
    name: 'Junior Science Discovery Kit',
    category: 'STEM Toys',
    price: 34.99,
    ageGroup: '6-8 years',
    description: 'Explore the wonders of science with 15 exciting experiments! Includes volcano eruptions, crystal growing, and colour-mixing activities. All materials are non-toxic and supervised by safety standards.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 3,
    name: 'Magnetic Tile Construction Set',
    category: 'STEM Toys',
    price: 39.99,
    ageGroup: '3-5 years',
    description: '52-piece magnetic building tiles that click together to create 3D structures. Develops mathematical thinking and engineering skills whilst having fun building castles, cars, and more!',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 4,
    name: 'Coding Robot for Kids',
    category: 'STEM Toys',
    price: 44.99,
    ageGroup: '6-8 years',
    description: 'Introduce programming concepts with this friendly coding robot. Simple drag-and-drop interface helps children learn logic, sequencing, and problem-solving skills.',
    image: '/logo.png',
    inStock: true
  },

  // Art Supplies
  {
    id: 5,
    name: 'Jumbo Crayon Set - 48 Colours',
    category: 'Art Supplies',
    price: 12.99,
    ageGroup: '3-5 years',
    description: 'Non-toxic, washable crayons in a rainbow of 48 brilliant colours. Perfect size for little hands to grip comfortably. Encourages artistic expression and colour recognition.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 6,
    name: 'Watercolour Paint Set with Brushes',
    category: 'Art Supplies',
    price: 18.99,
    ageGroup: '6-8 years',
    description: 'Complete watercolour set includes 24 vibrant colours, 3 quality brushes, mixing palette, and watercolour paper. Perfect for budding artists to explore colour blending and painting techniques.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 7,
    name: 'Craft Activity Mega Box',
    category: 'Art Supplies',
    price: 29.99,
    ageGroup: '6-8 years',
    description: 'Over 1000 pieces including pom-poms, googly eyes, pipe cleaners, foam shapes, glue sticks, and scissors. Everything needed for hours of creative craft projects!',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 8,
    name: 'Finger Painting Kit',
    category: 'Art Supplies',
    price: 15.99,
    ageGroup: '0-2 years',
    description: 'Safe, washable finger paints in 6 bright colours. Includes apron and activity guide. Encourages sensory exploration and early mark-making skills.',
    image: '/logo.png',
    inStock: true
  },

  // Books
  {
    id: 9,
    name: 'The Rainbow Adventures Collection',
    category: 'Books',
    price: 16.99,
    ageGroup: '3-5 years',
    description: 'A beautifully illustrated set of 5 picture books featuring colourful characters on exciting adventures. Perfect for bedtime stories and developing early reading interest.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 10,
    name: 'First Numbers and Counting Book',
    category: 'Books',
    price: 8.99,
    ageGroup: '0-2 years',
    description: 'Sturdy board book with bright illustrations to help little ones learn numbers 1-10. Touch-and-feel elements make learning interactive and fun!',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 11,
    name: 'Amazing Animal Facts Encyclopedia',
    category: 'Books',
    price: 19.99,
    ageGroup: '6-8 years',
    description: 'Discover fascinating facts about animals from around the world! Full-colour photographs and easy-to-read text make learning about nature exciting.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 12,
    name: 'Space Explorers Adventure Series',
    category: 'Books',
    price: 22.99,
    ageGroup: '9-10 years',
    description: 'Join young astronauts on thrilling missions through space! This 3-book series combines adventure stories with real science facts about planets and stars.',
    image: '/logo.png',
    inStock: true
  },

  // Outdoor Equipment
  {
    id: 13,
    name: 'Bubble Blowing Mega Set',
    category: 'Outdoor Equipment',
    price: 14.99,
    ageGroup: '3-5 years',
    description: 'Create giant bubbles with this fantastic set! Includes bubble solution, wands in various shapes, and a bubble blowing machine. Perfect for outdoor fun and parties.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 14,
    name: 'Junior Gardening Tool Kit',
    category: 'Outdoor Equipment',
    price: 21.99,
    ageGroup: '6-8 years',
    description: 'Real working garden tools sized perfectly for children! Includes trowel, fork, watering can, gloves, and seed packets. Learn about plants whilst having fun outdoors.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 15,
    name: 'Rainbow Parachute Play Set',
    category: 'Outdoor Equipment',
    price: 27.99,
    ageGroup: '3-5 years',
    description: '3-metre rainbow parachute with handles for group play. Perfect for parties, playdates, and PE activities. Encourages cooperative play and physical activity.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 16,
    name: 'Explorer\'s Nature Discovery Kit',
    category: 'Outdoor Equipment',
    price: 24.99,
    ageGroup: '6-8 years',
    description: 'Everything needed for outdoor adventures! Includes magnifying glass, bug catcher, binoculars, compass, and field guide. Spark curiosity about the natural world.',
    image: '/logo.png',
    inStock: true
  },

  // Musical Instruments
  {
    id: 17,
    name: 'Rainbow Xylophone',
    category: 'Musical Instruments',
    price: 19.99,
    ageGroup: '3-5 years',
    description: 'Beautifully crafted wooden xylophone with 8 colourful keys. Includes songbook with simple tunes. Develops musical skills and hand-eye coordination.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 18,
    name: 'Junior Percussion Set',
    category: 'Musical Instruments',
    price: 32.99,
    ageGroup: '6-8 years',
    description: '10-piece percussion set including tambourine, maracas, triangle, castanets, and more! Perfect for introducing rhythm and musical expression.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 19,
    name: 'Child-Safe Recorder with Lesson Book',
    category: 'Musical Instruments',
    price: 12.99,
    ageGroup: '9-10 years',
    description: 'High-quality descant recorder with beginner\'s lesson book. Easy-to-clean, durable construction. Perfect first wind instrument for aspiring musicians.',
    image: '/logo.png',
    inStock: true
  },
  {
    id: 20,
    name: 'Baby\'s First Drum',
    category: 'Musical Instruments',
    price: 16.99,
    ageGroup: '0-2 years',
    description: 'Soft, safe drum with gentle sounds perfect for little ones. Develops rhythm awareness and motor skills. Includes easy-grip drumsticks.',
    image: '/logo.png',
    inStock: true
  }
];
