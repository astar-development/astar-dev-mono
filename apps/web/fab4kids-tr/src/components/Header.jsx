import { Link } from 'react-router-dom';
import { useCart } from '../context/CartContext';
import { useTheme } from '../context/ThemeContext';
import './Header.css';

const Header = () => {
  const { getCartCount } = useCart();
  const { theme, toggleTheme } = useTheme();
  const cartCount = getCartCount();

  return (
    <header className="header">
      <div className="header-content">
        <Link to="/" className="logo-link">
          <img src="/logo.png" alt="Fab4Kids Logo" className="logo" />
          <h1 className="site-title">Fab4Kids</h1>
        </Link>

        <nav className="nav">
          <Link to="/" className="nav-link">Home</Link>
          <Link to="/products" className="nav-link">Products</Link>
          <Link to="/about" className="nav-link">About Us</Link>
          <Link to="/contact" className="nav-link">Contact</Link>
          <Link to="/faq" className="nav-link">FAQ</Link>
        </nav>

        <div className="header-actions">
          <button 
            onClick={toggleTheme}
            className="theme-toggle"
            aria-label="Toggle colour theme"
            title={theme === 'rainbow-spots' ? 'Switch to Rainbow Gradient Theme' : 'Switch to Rainbow Arcs Theme'}
          >
            {theme === 'rainbow-spots' ? '🌈 Gradient' : '🌈 Arcs'}
          </button>

          <Link to="/cart" className="cart-link">
            <span className="cart-icon">🛒</span>
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
