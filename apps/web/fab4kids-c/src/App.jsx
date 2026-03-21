import { BrowserRouter as Router, Routes, Route, useLocation } from 'react-router-dom';
import { CartProvider } from './context/CartContext';
import { ThemeProvider } from './context/ThemeContext';
import Header from './components/Header';
import Footer from './components/Footer';
import Home from './pages/Home';
import Products from './pages/Products';
import ProductDetail from './pages/ProductDetail';
import Cart from './pages/Cart';
import Contact from './pages/Contact';
import About from './pages/About';
import FAQ from './pages/FAQ';
import ReturnsPolicy from './pages/ReturnsPolicy';
import './App.css';

const getPageClass = (pathname) => {
  if (pathname === '/') return 'page-home';
  if (pathname.startsWith('/products')) return 'page-products';
  if (pathname.startsWith('/product/')) return 'page-detail';
  if (pathname.startsWith('/cart')) return 'page-cart';
  if (pathname.startsWith('/contact')) return 'page-contact';
  if (pathname.startsWith('/about')) return 'page-about';
  if (pathname.startsWith('/faq')) return 'page-faq';
  if (pathname.startsWith('/returns')) return 'page-returns';
  return 'page-generic';
};

const AppShell = () => {
  const location = useLocation();
  const pageClass = getPageClass(location.pathname);

  return (
    <div className={`app ${pageClass}`}>
      <Header />
      <main className="main-content">
        <div className="route-stage" key={location.pathname}>
          <Routes location={location}>
            <Route path="/" element={<Home />} />
            <Route path="/products" element={<Products />} />
            <Route path="/product/:id" element={<ProductDetail />} />
            <Route path="/cart" element={<Cart />} />
            <Route path="/contact" element={<Contact />} />
            <Route path="/about" element={<About />} />
            <Route path="/faq" element={<FAQ />} />
            <Route path="/returns" element={<ReturnsPolicy />} />
          </Routes>
        </div>
      </main>
      <Footer />
    </div>
  );
};

function App() {
  return (
    <ThemeProvider>
      <CartProvider>
        <Router>
          <AppShell />
        </Router>
      </CartProvider>
    </ThemeProvider>
  );
}

export default App;
