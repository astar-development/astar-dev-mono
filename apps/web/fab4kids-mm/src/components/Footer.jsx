import { Link } from 'react-router-dom';
import './Footer.css';

const Footer = () => {
  return (
    <footer className="footer">
      <div className="footer-content">
        <div className="footer-section">
          <h3>Fab4Kids Studio</h3>
          <p>Montessori-first learning resources for homes, classrooms, and micro-schools.</p>
          <p>© 2026 Fab4Kids Studio. All rights reserved.</p>
        </div>

        <div className="footer-section">
          <h4>Quick Links</h4>
          <Link to="/about" className="footer-link">Our Approach</Link>
          <Link to="/products" className="footer-link">Catalogue</Link>
          <Link to="/contact" className="footer-link">Contact</Link>
        </div>

        <div className="footer-section">
          <h4>Help & Support</h4>
          <Link to="/faq" className="footer-link">FAQ</Link>
          <Link to="/returns" className="footer-link">Returns & Delivery</Link>
        </div>

        <div className="footer-section">
          <h4>Studio Desk</h4>
          <p>support@fab4kids.co.uk</p>
          <p>0800 123 4567</p>
          <p>24 Via dei Bambini, London, UK</p>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
