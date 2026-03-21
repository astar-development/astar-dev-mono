import { Link } from 'react-router-dom';
import { useTheme } from '../context/ThemeContext';
import './Home.css';

const Home = () => {
  const { theme } = useTheme();
  const isMariaTheme = theme === 'maria-montessori';

  return (
    <div className="home">
      <section className="hero">
        <div className="hero-content">
          <h1 className="hero-title">
            {isMariaTheme ? 'Casa Maria Learning Studio' : 'Fab4Kids Montessori Collection'}
          </h1>
          <p className="hero-subtitle">
            {isMariaTheme
              ? 'A contemporary Montessori schoolhouse inspired by Maria Montessori in the present day.'
              : 'Downloadable worksheet packs and handcrafted Montessori equipment for children under 10.'}
          </p>
          <p className="hero-text">
            Build independent routines with practical-life trays, sensorial tools, and printable
            lesson companions. Every set includes setup notes, progression ideas, and home-friendly
            classroom workflows.
          </p>
          <Link to="/products" className="hero-btn">
            Explore the Catalogue
          </Link>
        </div>
      </section>

      <section className="features">
        <div className="feature-card">
          <div className="feature-icon">📄</div>
          <h3>Worksheet Studio</h3>
          <p>Printable literacy, numeracy, and geography packs with control cards and extension prompts.</p>
        </div>
        
        <div className="feature-card">
          <div className="feature-icon">🧩</div>
          <h3>Physical Apparatus</h3>
          <p>Wooden materials from pink towers to movable alphabets, prepared for real Montessori cycles.</p>
        </div>
        
        <div className="feature-card">
          <div className="feature-icon">🏫</div>
          <h3>Classroom Bundles</h3>
          <p>Ready-made shelf plans for homes, nurseries, and lower-elementary environments.</p>
        </div>
        
        <div className="feature-card">
          <div className="feature-icon">🕊️</div>
          <h3>Guide Notes</h3>
          <p>Short training notes for adults on observation, minimal prompting, and respectful correction.</p>
        </div>
      </section>

      <section className="categories-preview">
        <h2>Browse by Learning Path</h2>
        <div className="category-grid">
          <Link to="/products?category=Worksheet Packs" className="category-item">
            <span className="category-emoji">📝</span>
            <h3>Worksheet Packs</h3>
          </Link>
          <Link to="/products?category=Practical Life" className="category-item">
            <span className="category-emoji">🫙</span>
            <h3>Practical Life</h3>
          </Link>
          <Link to="/products?category=Sensorial Materials" className="category-item">
            <span className="category-emoji">🧱</span>
            <h3>Sensorial Materials</h3>
          </Link>
          <Link to="/products?category=Language & Literacy" className="category-item">
            <span className="category-emoji">🔠</span>
            <h3>Language & Literacy</h3>
          </Link>
          <Link to="/products?category=Maths & Geometry" className="category-item">
            <span className="category-emoji">➗</span>
            <h3>Maths & Geometry</h3>
          </Link>
        </div>
      </section>

      <section className="info-banner">
        <h2>Version 2 Highlights</h2>
        <div className="info-grid">
          <div className="info-item">
            <span className="info-icon">🧠</span>
            <h4>Sequence-Based Learning</h4>
            <p>Worksheet packs map directly to apparatus progression for faster independent practice.</p>
          </div>
          <div className="info-item">
            <span className="info-icon">🚚</span>
            <h4>Smart Shipping</h4>
            <p>Tracked UK delivery on physical sets, instant downloads for digital packs.</p>
          </div>
          <div className="info-item">
            <span className="info-icon">🏛️</span>
            <h4>Montessori Authenticity</h4>
            <p>Material proportions and terminology follow classroom norms used by trained guides.</p>
          </div>
          <div className="info-item">
            <span className="info-icon">🎯</span>
            <h4>Simple for Adults</h4>
            <p>Each order includes setup cards so parents and teachers can start in under 20 minutes.</p>
          </div>
        </div>
      </section>
    </div>
  );
};

export default Home;
