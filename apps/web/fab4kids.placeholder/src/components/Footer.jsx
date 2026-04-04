import { Link } from 'react-router-dom';
import './Footer.css';

const Footer = () => {
  return (
    <footer className="footer">
      <div className="footer-content">
        <div className="footer-section">
          <h3>Fab4Kids</h3>
          <p>Quality educational equipment and materials for children under 10.</p>
          <p>© 2026 Fab4Kids. All rights reserved.</p>
        </div>

        <div className="footer-section">
          <h4>Quick Links</h4>
          <Link to="/about" className="footer-link">About Us</Link>
          <Link to="/products" className="footer-link">Products</Link>
          <Link to="/contact" className="footer-link">Contact</Link>
        </div>

        <div className="footer-section">
          <h4>Help & Support</h4>
          <Link to="/faq" className="footer-link">FAQ</Link>
          <Link to="/returns" className="footer-link">Returns & Delivery</Link>
        </div>

        <div className="footer-section">
          <h4>Contact Us</h4>
          <p>📧 hello@fab4kids.co.uk</p>
          <p>📞 0800 123 4567</p>
          <p>🏢 123 Learning Lane, London, UK</p>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
