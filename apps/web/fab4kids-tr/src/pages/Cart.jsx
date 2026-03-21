import { Link } from 'react-router-dom';
import { useCart } from '../context/CartContext';
import './Cart.css';

const Cart = () => {
  const { cartItems, removeFromCart, updateQuantity, getCartTotal, clearCart } = useCart();

  if (cartItems.length === 0) {
    return (
      <div className="cart-page">
        <div className="empty-cart">
          <h2>Your Basket is Empty</h2>
          <p>Add some fantastic educational products for your little ones!</p>
          <Link to="/products" className="shop-btn">Browse Products</Link>
        </div>
      </div>
    );
  }

  const subtotal = getCartTotal();
  const deliveryFee = subtotal >= 25 ? 0 : 4.99;
  const total = subtotal + deliveryFee;

  return (
    <div className="cart-page">
      <h1>Shopping Basket</h1>

      <div className="cart-container">
        <div className="cart-items">
          {cartItems.map(item => (
            <div key={item.id} className="cart-item">
              <img src={item.image} alt={item.name} className="cart-item-image" />
              
              <div className="cart-item-info">
                <Link to={`/product/${item.id}`} className="cart-item-name">
                  {item.name}
                </Link>
                <p className="cart-item-category">{item.category}</p>
                <p className="cart-item-age">{item.ageGroup}</p>
              </div>

              <div className="cart-item-quantity">
                <button onClick={() => updateQuantity(item.id, item.quantity - 1)}>
                  -
                </button>
                <span>{item.quantity}</span>
                <button onClick={() => updateQuantity(item.id, item.quantity + 1)}>
                  +
                </button>
              </div>

              <div className="cart-item-price">
                <p className="unit-price">£{item.price.toFixed(2)} each</p>
                <p className="total-price">£{(item.price * item.quantity).toFixed(2)}</p>
              </div>

              <button 
                onClick={() => removeFromCart(item.id)}
                className="remove-btn"
                aria-label="Remove item"
              >
                ✕
              </button>
            </div>
          ))}

          <button onClick={clearCart} className="clear-cart-btn">
            Clear Basket
          </button>
        </div>

        <div className="cart-summary">
          <h2>Order Summary</h2>
          
          <div className="summary-row">
            <span>Subtotal:</span>
            <span>£{subtotal.toFixed(2)}</span>
          </div>

          <div className="summary-row">
            <span>Delivery:</span>
            <span>
              {deliveryFee === 0 ? (
                <span className="free-delivery">FREE</span>
              ) : (
                `£${deliveryFee.toFixed(2)}`
              )}
            </span>
          </div>

          {subtotal < 25 && (
            <div className="delivery-note">
              Add £{(25 - subtotal).toFixed(2)} more for free delivery!
            </div>
          )}

          <div className="summary-divider"></div>

          <div className="summary-row total-row">
            <span>Total:</span>
            <span>£{total.toFixed(2)}</span>
          </div>

          <button className="checkout-btn">
            Proceed to Checkout
          </button>

          <Link to="/products" className="continue-shopping">
            ← Continue Shopping
          </Link>

          <div className="cart-info">
            <p>✓ Secure checkout</p>
            <p>✓ 30-day returns</p>
            <p>✓ UK safety standards</p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Cart;
