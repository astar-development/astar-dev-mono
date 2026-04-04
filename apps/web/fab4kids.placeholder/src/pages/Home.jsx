import { Link } from 'react-router-dom';
import './Home.css';

const Home = () => {
  return (
    <div className="home">
      <section className="hero">
        <div className="hero-content">
          <h1 className="hero-title">Welcome to Fab4Kids!</h1>
          <p className="hero-subtitle">
            The UK's favourite destination for quality educational equipment and materials
          </p>
          <p className="hero-text">
            Inspiring young minds through play! Discover our carefully curated collection
            of STEM toys, art supplies, books, outdoor equipment, and musical instruments
            designed specifically for children under 10.
          </p>
          <Link to="/products" className="hero-btn">
            Explore Our Products
          </Link>
        </div>
      </section>

      <section className="features">
        <div className="feature-card">
          <div className="feature-icon">🎨</div>
          <h3>Creative Learning</h3>
          <p>Foster creativity and imagination with our wide range of art supplies and craft materials.</p>
        </div>
        
        <div className="feature-card">
          <div className="feature-icon">🔬</div>
          <h3>STEM Education</h3>
          <p>Build problem-solving skills with engaging science, technology, engineering, and maths toys.</p>
        </div>
        
        <div className="feature-card">
          <div className="feature-icon">📚</div>
          <h3>Reading Adventures</h3>
          <p>Ignite a love for reading with our carefully selected children's books and educational stories.</p>
        </div>
        
        <div className="feature-card">
          <div className="feature-icon">🎵</div>
          <h3>Musical Discovery</h3>
          <p>Introduce young learners to the joy of music with child-safe instruments and rhythm toys.</p>
        </div>
      </section>

      <section className="categories-preview">
        <h2>Shop by Category</h2>
        <div className="category-grid">
          <Link to="/products?category=STEM Toys" className="category-item">
            <span className="category-emoji">🔬</span>
            <h3>STEM Toys</h3>
          </Link>
          <Link to="/products?category=Art Supplies" className="category-item">
            <span className="category-emoji">🎨</span>
            <h3>Art Supplies</h3>
          </Link>
          <Link to="/products?category=Books" className="category-item">
            <span className="category-emoji">📚</span>
            <h3>Books</h3>
          </Link>
          <Link to="/products?category=Outdoor Equipment" className="category-item">
            <span className="category-emoji">⚽</span>
            <h3>Outdoor Equipment</h3>
          </Link>
          <Link to="/products?category=Musical Instruments" className="category-item">
            <span className="category-emoji">🎵</span>
            <h3>Musical Instruments</h3>
          </Link>
        </div>
      </section>

      <section className="info-banner">
        <h2>Why Choose Fab4Kids?</h2>
        <div className="info-grid">
          <div className="info-item">
            <span className="info-icon">✅</span>
            <h4>Safety First</h4>
            <p>All products meet UK safety standards and are age-appropriate</p>
          </div>
          <div className="info-item">
            <span className="info-icon">🚚</span>
            <h4>Free UK Delivery</h4>
            <p>On orders over £25</p>
          </div>
          <div className="info-item">
            <span className="info-icon">↩️</span>
            <h4>Easy Returns</h4>
            <p>30-day money-back guarantee</p>
          </div>
          <div className="info-item">
            <span className="info-icon">💚</span>
            <h4>Eco-Friendly</h4>
            <p>We prioritise sustainable and eco-conscious products</p>
          </div>
        </div>
      </section>
    </div>
  );
};

export default Home;
