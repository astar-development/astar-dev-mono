import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import ProductCard from '../components/ProductCard';
import { products, categories, ageGroups } from '../data/productsData';
import './Products.css';

const Products = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const [selectedCategory, setSelectedCategory] = useState(searchParams.get('category') || 'All');
  const [selectedAge, setSelectedAge] = useState('All Ages');
  const [sortBy, setSortBy] = useState('name');

  useEffect(() => {
    const category = searchParams.get('category');
    if (category) {
      setSelectedCategory(category);
    }
  }, [searchParams]);

  const filteredProducts = products.filter(product => {
    const categoryMatch = selectedCategory === 'All' || product.category === selectedCategory;
    const ageMatch = selectedAge === 'All Ages' || product.ageGroup === selectedAge;
    return categoryMatch && ageMatch;
  });

  const sortedProducts = [...filteredProducts].sort((a, b) => {
    switch (sortBy) {
      case 'name':
        return a.name.localeCompare(b.name);
      case 'price-low':
        return a.price - b.price;
      case 'price-high':
        return b.price - a.price;
      default:
        return 0;
    }
  });

  const handleCategoryChange = (category) => {
    setSelectedCategory(category);
    if (category !== 'All') {
      setSearchParams({ category });
    } else {
      setSearchParams({});
    }
  };

  return (
    <div className="products-page">
      <div className="products-header">
        <h1>Montessori Catalogue</h1>
        <p>Downloadable worksheet collections and physical classroom materials in one curated shop.</p>
      </div>

      <div className="products-container">
        <aside className="filters">
          <div className="filter-section">
            <h3>Category</h3>
            {categories.map(category => (
              <label key={category} className="filter-option">
                <input
                  type="radio"
                  name="category"
                  value={category}
                  checked={selectedCategory === category}
                  onChange={(e) => handleCategoryChange(e.target.value)}
                />
                {category}
              </label>
            ))}
          </div>

          <div className="filter-section">
            <h3>Age Group</h3>
            {ageGroups.map(age => (
              <label key={age} className="filter-option">
                <input
                  type="radio"
                  name="age"
                  value={age}
                  checked={selectedAge === age}
                  onChange={(e) => setSelectedAge(e.target.value)}
                />
                {age}
              </label>
            ))}
          </div>

          <div className="filter-section">
            <h3>Sort By</h3>
            <select 
              value={sortBy} 
              onChange={(e) => setSortBy(e.target.value)}
              className="sort-select"
            >
              <option value="name">Name (A-Z)</option>
              <option value="price-low">Price (Low to High)</option>
              <option value="price-high">Price (High to Low)</option>
            </select>
          </div>
        </aside>

        <div className="products-grid">
          <div className="products-info">
            <p>Showing {sortedProducts.length} item{sortedProducts.length !== 1 ? 's' : ''}</p>
          </div>
          
          {sortedProducts.length > 0 ? (
            <div className="grid">
              {sortedProducts.map(product => (
                <ProductCard key={product.id} product={product} />
              ))}
            </div>
          ) : (
            <div className="no-products">
              <p>No products found matching your criteria.</p>
              <p>Try adjusting your filters!</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default Products;
