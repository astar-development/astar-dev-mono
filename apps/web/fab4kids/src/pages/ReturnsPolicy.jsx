import './ReturnsPolicy.css';

const ReturnsPolicy = () => {
  return (
    <div className="returns-page">
      <div className="returns-header">
        <h1>Returns & Delivery Policy</h1>
        <p>Everything you need to know about shipping and returns at Fab4Kids</p>
      </div>

      <section className="policy-section">
        <h2>📦 Delivery Information</h2>
        
        <h3>Delivery Charges</h3>
        <ul>
          <li><strong>FREE standard UK delivery</strong> on orders over £25</li>
          <li>£4.99 flat rate for orders under £25</li>
          <li>Express delivery options available at checkout</li>
        </ul>

        <h3>Delivery Times</h3>
        <ul>
          <li><strong>Standard Delivery:</strong> 3-5 working days</li>
          <li><strong>Express Delivery:</strong> 1-2 working days (£7.99)</li>
          <li><strong>Next Day Delivery:</strong> Order before 2pm for next working day delivery (£9.99)</li>
        </ul>

        <h3>Order Processing</h3>
        <p>
          Orders are typically processed and dispatched within 1-2 working days. You'll receive
          an email confirmation when your order is dispatched, including tracking information.
        </p>

        <h3>Delivery Areas</h3>
        <p>
          We deliver to all UK addresses including England, Scotland, Wales, and Northern Ireland.
          Please note that delivery to Scottish Highlands, Islands, and some remote areas may
          take an additional 1-2 days.
        </p>
      </section>

      <section className="policy-section highlight">
        <h2>↩️ Returns Policy</h2>
        
        <h3>30-Day Money-Back Guarantee</h3>
        <p>
          We want you to be completely satisfied with your purchase. If you're not happy for
          any reason, you can return items within 30 days of receipt for a full refund or exchange.
        </p>

        <h3>Return Conditions</h3>
        <p>To be eligible for a return, items must be:</p>
        <ul>
          <li>Returned within 30 days of receipt</li>
          <li>Unused and in original condition</li>
          <li>In their original packaging with all tags and labels attached</li>
          <li>In a resalable condition</li>
        </ul>

        <h3>How to Return an Item</h3>
        <ol className="numbered-list">
          <li>
            <strong>Contact Us:</strong> Email hello@fab4kids.co.uk with your order number
            and the item(s) you wish to return. We'll provide you with return instructions
            and a returns authorisation number.
          </li>
          <li>
            <strong>Pack Your Item:</strong> Securely package the item in its original
            packaging if possible. Include your returns authorisation number.
          </li>
          <li>
            <strong>Send It Back:</strong> Return the item to the address provided. We
            recommend using a tracked service for your protection.
          </li>
          <li>
            <strong>Receive Your Refund:</strong> Once we receive and inspect your return,
            we'll process your refund within 5-7 working days.
          </li>
        </ol>

        <h3>Return Postage Costs</h3>
        <ul>
          <li>
            <strong>Faulty or incorrect items:</strong> We'll cover all return postage costs.
            Contact us before returning the item.
          </li>
          <li>
            <strong>Change of mind:</strong> You'll need to cover the cost of return postage.
          </li>
        </ul>
      </section>

      <section className="policy-section">
        <h2>🔄 Exchanges</h2>
        <p>
          If you'd like to exchange an item for a different product or size, please return
          the original item following our returns process and place a new order for the
          replacement item. This ensures you receive your new item as quickly as possible.
        </p>
      </section>

      <section className="policy-section">
        <h2>⚠️ Damaged or Faulty Items</h2>
        <p>
          Whilst we take great care in packaging and dispatching orders, sometimes things
          can go wrong in transit. If you receive a damaged or faulty item:
        </p>
        <ol className="numbered-list">
          <li>Contact us immediately at hello@fab4kids.co.uk or call 0800 123 4567</li>
          <li>Provide your order number and photos of the damage if possible</li>
          <li>We'll arrange a replacement or full refund at no cost to you</li>
          <li>Return postage will be covered by us</li>
        </ol>
      </section>

      <section className="policy-section">
        <h2>🎁 Gift Returns</h2>
        <p>
          If you've received an item as a gift and would like to return it, we're happy to
          help! Please contact us with the order details, and we can arrange a return for
          store credit or exchange. Refunds for gift purchases can only be made to the
          original purchaser.
        </p>
      </section>

      <section className="policy-section">
        <h2>📞 Need Help?</h2>
        <p>
          If you have any questions about our delivery or returns policy, our customer
          service team is here to help:
        </p>
        <div className="contact-info">
          <p>📧 Email: <a href="mailto:hello@fab4kids.co.uk">hello@fab4kids.co.uk</a></p>
          <p>📞 Phone: <a href="tel:08001234567">0800 123 4567</a></p>
          <p>⏰ Hours: Monday-Friday 9am-5pm, Saturday 10am-4pm</p>
        </div>
      </section>
    </div>
  );
};

export default ReturnsPolicy;
