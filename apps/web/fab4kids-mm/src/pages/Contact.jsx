import { useState } from 'react';
import './Contact.css';

const Contact = () => {
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    subject: '',
    message: ''
  });
  const [submitted, setSubmitted] = useState(false);

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    setSubmitted(true);
    setTimeout(() => {
      setSubmitted(false);
      setFormData({ name: '', email: '', subject: '', message: '' });
    }, 3000);
  };

  return (
    <div className="contact-page">
      <div className="contact-header">
        <h1>Contact the Studio</h1>
        <p>Need help choosing worksheet packs or setting up physical Montessori shelves? We can help.</p>
      </div>

      <div className="contact-container">
        <div className="contact-info">
          <div className="info-card">
            <h3>Email Support</h3>
            <p>support@fab4kids.co.uk</p>
            <p>We typically respond within 24 hours</p>
          </div>

          <div className="info-card">
            <h3>Phone Support</h3>
            <p>0800 123 4567</p>
            <p>Monday - Friday: 9:00 AM - 5:00 PM</p>
            <p>Saturday: 10:00 AM - 4:00 PM</p>
            <p>Sunday: Closed</p>
          </div>

          <div className="info-card">
            <h3>Visit the Studio</h3>
            <p>Fab4Kids Studio</p>
            <p>24 Via dei Bambini</p>
            <p>London, EC1A 1BB</p>
            <p>United Kingdom</p>
          </div>

          <div className="info-card">
            <h3>Appointments</h3>
            <p>Showroom visits by appointment only</p>
            <p>Monday - Friday: 10:00 AM - 4:00 PM</p>
            <p>Please call ahead to arrange a visit</p>
          </div>
        </div>

        <div className="contact-form-container">
          <h2>Send a Message</h2>
          {submitted ? (
            <div className="success-message">
              <h3>✓ Message Sent!</h3>
              <p>Thank you for contacting us. We'll get back to you as soon as possible.</p>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="contact-form">
              <div className="form-group">
                <label htmlFor="name">Your Name *</label>
                <input
                  type="text"
                  id="name"
                  name="name"
                  value={formData.name}
                  onChange={handleChange}
                  required
                  placeholder="Enter your name"
                />
              </div>

              <div className="form-group">
                <label htmlFor="email">Email Address *</label>
                <input
                  type="email"
                  id="email"
                  name="email"
                  value={formData.email}
                  onChange={handleChange}
                  required
                  placeholder="your.email@example.com"
                />
              </div>

              <div className="form-group">
                <label htmlFor="subject">Subject *</label>
                <select
                  id="subject"
                  name="subject"
                  value={formData.subject}
                  onChange={handleChange}
                  required
                >
                  <option value="">Select a subject</option>
                  <option value="worksheet-enquiry">Worksheet Enquiry</option>
                  <option value="material-enquiry">Material Enquiry</option>
                  <option value="order-status">Order Status</option>
                  <option value="returns">Returns & Refunds</option>
                  <option value="bulk-order">School / Nursery Orders</option>
                  <option value="feedback">Feedback</option>
                  <option value="other">Other</option>
                </select>
              </div>

              <div className="form-group">
                <label htmlFor="message">Your Message *</label>
                <textarea
                  id="message"
                  name="message"
                  value={formData.message}
                  onChange={handleChange}
                  required
                  rows="6"
                  placeholder="Tell us how we can help..."
                />
              </div>

              <button type="submit" className="submit-btn">
                Send Message
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  );
};

export default Contact;
