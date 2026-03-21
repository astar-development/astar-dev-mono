import { useParams, Link } from 'react-router-dom';
import { useState } from 'react';
import { products } from '../data/productsData';
import { useCart } from '../context/CartContext';
import './ProductDetail.css';

const ProductDetail = () => {
  const { id } = useParams();
  const { addToCart } = useCart();
  const [quantity, setQuantity] = useState(1);
  const [addedToCart, setAddedToCart] = useState(false);

  const product = products.find(p => p.id === parseInt(id));

  if (!product) {
    return (
      <div className="product-detail-page">
        <div className="not-found">
          <h2>Product Not Found</h2>
          <p>Sorry, we couldn't find the product you're looking for.</p>
          <Link to="/products" className="back-link">Browse All Products</Link>
        </div>
      </div>
    );
  }

  const handleAddToCart = () => {
    for (let i = 0; i < quantity; i++) {
      addToCart(product);
    }
    setAddedToCart(true);
    setTimeout(() => setAddedToCart(false), 3000);
  };

  const handleQuantityChange = (change) => {
    const newQuantity = quantity + change;
    if (newQuantity >= 1 && newQuantity <= 10) {
      setQuantity(newQuantity);
    }
  };

  const relatedProducts = products
    .filter(p => p.category === product.category && p.id !== product.id)
    .slice(0, 4);

  return (
    <div className="product-detail-page">
      <Link to="/products" className="back-link">← Back to Products</Link>

      <div className="product-detail">
        <div className="product-image-section">
          <img src={product.image} alt={product.name} className="detail-image" />
        </div>

        <div className="product-info-section">
          <h1>{product.name}</h1>
          
          <div className="product-meta">
            <span className="category-badge">{product.category}</span>
            <span className="age-badge">{product.ageGroup}</span>
          </div>

          <div className="price-section">
            <p className="price">£{product.price.toFixed(2)}</p>
            {product.inStock ? (
              <span className="in-stock">✓ In Stock</span>
            ) : (
              <span className="out-of-stock">Out of Stock</span>
            )}
          </div>

          <div className="description">
            <h3>Product Description</h3>
            <p>{product.description}</p>
          </div>

          {product.inStock && (
            <div className="purchase-section">
              <div className="quantity-selector">
                <label>Quantity:</label>
                <div className="quantity-controls">
                  <button onClick={() => handleQuantityChange(-1)}>-</button>
                  <span>{quantity}</span>
                  <button onClick={() => handleQuantityChange(1)}>+</button>
                </div>
              </div>

              <button onClick={handleAddToCart} className="add-to-basket-btn">
                Add to Basket - £{(product.price * quantity).toFixed(2)}
              </button>

              {addedToCart && (
                <div className="added-notification">
                  ✓ Added to basket!
                </div>
              )}
            </div>
          )}

          <div className="product-features">
            <h3>Key Features</h3>
            <ul>
              <li>Safe and non-toxic materials</li>
              <li>Meets UK safety standards</li>
              <li>Educational and engaging</li>
              <li>Durable construction</li>
              <li>Perfect for age group: {product.ageGroup}</li>
            </ul>
          </div>
        </div>
      </div>

      {relatedProducts.length > 0 && (
        <div className="related-products">
          <h2>You May Also Like</h2>
          <div className="related-grid">
            {relatedProducts.map(relatedProduct => (
              <Link 
                key={relatedProduct.id} 
                to={`/product/${relatedProduct.id}`}
                className="related-product-card"
              >
                <img src={relatedProduct.image} alt={relatedProduct.name} />
                <h4>{relatedProduct.name}</h4>
                <p>£{relatedProduct.price.toFixed(2)}</p>
              </Link>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

export default ProductDetail;
