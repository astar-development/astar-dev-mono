import './ReturnsPolicy.css';

const ReturnsPolicy = () => {
  return (
    <div className="returns-page">
      <div className="returns-header">
        <h1>Returns & Delivery Policy</h1>
        <p>How we deliver downloadable worksheets and physical Montessori materials</p>
      </div>

      <section className="policy-section">
        <h2>Delivery Information</h2>
        
        <h3>Delivery Charges</h3>
        <ul>
          <li><strong>Digital worksheet packs:</strong> instant delivery by download</li>
          <li><strong>Physical materials:</strong> free standard UK delivery on orders over £45</li>
          <li>£5.99 flat rate for UK physical orders under £45</li>
        </ul>

        <h3>Delivery Times</h3>
        <ul>
          <li><strong>Digital products:</strong> immediate access after payment confirmation</li>
          <li><strong>Standard physical delivery:</strong> 3-5 working days</li>
          <li><strong>Express physical delivery:</strong> 1-2 working days (£8.99)</li>
        </ul>

        <h3>Order Processing</h3>
        <p>
          Physical orders are typically processed and dispatched within 1-2 working days. You'll
          receive a dispatch email with tracking details once your parcel leaves our warehouse.
        </p>

        <h3>Delivery Areas</h3>
        <p>
          We deliver to all UK addresses including England, Scotland, Wales, and Northern Ireland.
          Please note that delivery to Scottish Highlands, Islands, and some remote areas may
          take an additional 1-2 days.
        </p>
      </section>

      <section className="policy-section highlight">
        <h2>Returns Policy</h2>
        
        <h3>30-Day Confidence Guarantee (Physical Items)</h3>
        <p>
          If you're not satisfied with a physical product, you can return it within 30 days of receipt
          for a refund or exchange.
        </p>

        <h3>Digital Product Policy</h3>
        <p>
          Downloadable worksheet products are non-returnable once accessed. If your files fail to
          download or open correctly, we will replace them immediately at no charge.
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
            <strong>Contact Us:</strong> Email support@fab4kids.co.uk with your order number
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
        <h2>Exchanges</h2>
        <p>
          If you'd like to exchange an item for a different product or size, please return
          the original item following our returns process and place a new order for the
          replacement item. This ensures you receive your new item as quickly as possible.
        </p>
      </section>

      <section className="policy-section">
        <h2>Damaged or Faulty Items</h2>
        <p>
          Whilst we take great care in packaging and dispatching orders, sometimes things
          can go wrong in transit. If you receive a damaged or faulty item:
        </p>
        <ol className="numbered-list">
          <li>Contact us immediately at support@fab4kids.co.uk or call 0800 123 4567</li>
          <li>Provide your order number and photos of the damage if possible</li>
          <li>We'll arrange a replacement or full refund at no cost to you</li>
          <li>Return postage will be covered by us</li>
        </ol>
      </section>

      <section className="policy-section">
        <h2>Gift Returns</h2>
        <p>
          If you've received an item as a gift and would like to return it, we're happy to
          help! Please contact us with the order details, and we can arrange a return for
          store credit or exchange. Refunds for gift purchases can only be made to the
          original purchaser.
        </p>
      </section>

      <section className="policy-section">
        <h2>Need Help?</h2>
        <p>
          If you have any questions about our delivery or returns policy, our customer
          service team is here to help:
        </p>
        <div className="contact-info">
          <p>Email: <a href="mailto:support@fab4kids.co.uk">support@fab4kids.co.uk</a></p>
          <p>Phone: <a href="tel:08001234567">0800 123 4567</a></p>
          <p>Hours: Monday-Friday 9am-5pm, Saturday 10am-4pm</p>
        </div>
      </section>
    </div>
  );
};

export default ReturnsPolicy;
