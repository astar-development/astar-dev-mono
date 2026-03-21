import { Link } from 'react-router-dom';
import { useCart } from '../context/CartContext';
import './ProductCard.css';

const ProductCard = ({ product }) => {
  const { addToCart } = useCart();
  const isDigital = product.category === 'Worksheet Packs' || product.name.includes('(Digital)');

  const handleAddToCart = (e) => {
    e.preventDefault();
    e.stopPropagation();
    addToCart(product);
  };

  return (
    <Link to={`/product/${product.id}`} className="product-card">
      <div className="product-image-wrapper">
        <img src={product.image} alt={product.name} className="product-image" />
        {!product.inStock && <span className="out-of-stock-badge">Out of Stock</span>}
      </div>
      
      <div className="product-info">
        <h3 className="product-name">{product.name}</h3>
        <p className="product-category">{product.category}</p>
        <p className="product-format">{isDigital ? 'Digital Download' : 'Physical Material'}</p>
        <p className="product-age">{product.ageGroup}</p>
        <p className="product-price">£{product.price.toFixed(2)}</p>
        
        <button 
          onClick={handleAddToCart}
          disabled={!product.inStock}
          className="add-to-cart-btn"
        >
          {product.inStock ? (isDigital ? 'Add Download Pack' : 'Add to Basket') : 'Out of Stock'}
        </button>
      </div>
    </Link>
  );
};

export default ProductCard;
