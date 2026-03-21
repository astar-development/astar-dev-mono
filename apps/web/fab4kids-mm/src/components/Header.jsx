import { Link } from 'react-router-dom';
import { useCart } from '../context/CartContext';
import { useTheme } from '../context/ThemeContext';
import './Header.css';

const Header = () => {
  const { getCartCount } = useCart();
  const { theme, setTheme, toggleTheme } = useTheme();
  const cartCount = getCartCount();

  return (
    <header className="header">
      <div className="header-topline">
        <p>
          Downloadable Montessori worksheets and authentic classroom apparatus for home and school.
        </p>
      </div>
      <div className="header-content">
        <Link to="/" className="brand-link">
          <img src="/logo.png" alt="Fab4Kids Logo" className="logo" />
          <div className="brand-block">
            <h1 className="site-title">Fab4Kids Studio</h1>
            <p className="site-subtitle">Montessori Worksheets + Materials</p>
          </div>
        </Link>

        <nav className="nav">
          <Link to="/" className="nav-link">Home</Link>
          <Link to="/products" className="nav-link">Catalogue</Link>
          <Link to="/about" className="nav-link">Our Approach</Link>
          <Link to="/contact" className="nav-link">Contact</Link>
          <Link to="/faq" className="nav-link">FAQ</Link>
        </nav>

        <div className="header-actions">
          <button
            onClick={toggleTheme}
            className="theme-toggle"
            aria-label="Toggle colour theme"
            title={theme === 'italian-flag' ? 'Switch to Maria Montessori Theme' : 'Switch to Italian Flag Theme'}
          >
            {theme === 'italian-flag' ? 'Maria Montessori Theme' : 'Italian Flag Theme'}
          </button>

          <div className="theme-dots" aria-label="Theme selection">
            <button
              onClick={() => setTheme('italian-flag')}
              className={`theme-dot ${theme === 'italian-flag' ? 'active' : ''}`}
              aria-label="Use Italian Flag theme"
              title="Italian Flag"
            />
            <button
              onClick={() => setTheme('maria-montessori')}
              className={`theme-dot ${theme === 'maria-montessori' ? 'active' : ''}`}
              aria-label="Use Maria Montessori theme"
              title="Maria Montessori"
            />
          </div>

          <Link to="/cart" className="cart-link">
            <span className="cart-icon">Cart</span>
            {cartCount > 0 && (
              <span className="cart-badge">{cartCount}</span>
            )}
          </Link>
        </div>
      </div>
    </header>
  );
};

export default Header;
