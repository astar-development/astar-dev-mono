import { useState } from 'react';
import './FAQ.css';

const FAQ = () => {
  const [openIndex, setOpenIndex] = useState(null);

  const faqs = [
    {
      question: 'What age ranges do your products cater to?',
      answer: 'Our products are specifically designed for children under 10 years old. We categorise items by age groups: 0-2 years, 3-5 years, 6-8 years, and 9-10 years. Each product page clearly displays the recommended age range to help you choose the most appropriate items for your child.'
    },
    {
      question: 'Are all your products safe for children?',
      answer: 'Absolutely! Safety is our top priority. All products meet or exceed UK safety standards and regulations. We ensure that items are age-appropriate, made from non-toxic materials, and thoroughly tested. Each product listing includes relevant safety certifications and warnings where applicable.'
    },
    {
      question: 'Do you offer free delivery?',
      answer: 'Yes! We offer free standard UK delivery on all orders over £25. For orders under £25, a flat delivery charge of £4.99 applies. We typically dispatch orders within 1-2 working days, and delivery takes 3-5 working days for standard shipping.'
    },
    {
      question: 'What is your returns policy?',
      answer: 'We offer a 30-day money-back guarantee. If you\'re not completely satisfied with your purchase, you can return it within 30 days of receipt for a full refund or exchange. Items must be unused, in their original packaging, and in resalable condition. See our Returns & Delivery page for full details.'
    },
    {
      question: 'Can I track my order?',
      answer: 'Yes! Once your order is dispatched, you\'ll receive a confirmation email with tracking information. You can use this to monitor your delivery\'s progress. If you have any concerns about your order, our customer service team is here to help.'
    },
    {
      question: 'Do you offer gift wrapping?',
      answer: 'Whilst we don\'t currently offer gift wrapping, all our products arrive in secure, attractive packaging. We can include a gift message with your order – just add your message in the notes section during checkout.'
    },
    {
      question: 'Are your products environmentally friendly?',
      answer: 'We\'re committed to sustainability and prioritise eco-friendly products whenever possible. We work with suppliers who share our environmental values and actively seek out items made from sustainable materials, recyclable packaging, and ethical manufacturing processes.'
    },
    {
      question: 'Do you have physical shops?',
      answer: 'Fab4Kids is primarily an online retailer, but we do have a showroom at our London headquarters. Visits are by appointment only. Please call 0800 123 4567 to arrange a visit during our opening hours (Monday-Friday, 10:00 AM - 4:00 PM).'
    },
    {
      question: 'Can schools or nurseries order from you?',
      answer: 'Absolutely! We work with schools, nurseries, and childcare providers across the UK. We offer special bulk pricing and dedicated account management for educational institutions. Please contact us at hello@fab4kids.co.uk or call 0800 123 4567 to discuss your requirements.'
    },
    {
      question: 'What payment methods do you accept?',
      answer: 'We accept all major credit and debit cards (Visa, Mastercard, American Express), PayPal, and Apple Pay. All transactions are processed securely using industry-standard encryption.'
    },
    {
      question: 'How can I contact customer service?',
      answer: 'You can reach us by email at hello@fab4kids.co.uk or by phone at 0800 123 4567. Our customer service hours are Monday-Friday, 9:00 AM - 5:00 PM, and Saturday, 10:00 AM - 4:00 PM. We typically respond to emails within 24 hours.'
    },
    {
      question: 'Do you ship outside the UK?',
      answer: 'Currently, we only ship within the United Kingdom (England, Scotland, Wales, and Northern Ireland). We\'re working on expanding our delivery options and hope to offer international shipping in the future.'
    }
  ];

  const toggleFAQ = (index) => {
    setOpenIndex(openIndex === index ? null : index);
  };

  return (
    <div className="faq-page">
      <div className="faq-header">
        <h1>Frequently Asked Questions</h1>
        <p>Find answers to common questions about Fab4Kids products and services</p>
      </div>

      <div className="faq-intro">
        <p>
          Can't find what you're looking for? Our friendly customer service team is here to help!
          Contact us at <a href="mailto:hello@fab4kids.co.uk">hello@fab4kids.co.uk</a> or call{' '}
          <a href="tel:08001234567">0800 123 4567</a>.
        </p>
      </div>

      <div className="faq-list">
        {faqs.map((faq, index) => (
          <div key={index} className={`faq-item ${openIndex === index ? 'open' : ''}`}>
            <button
              className="faq-question"
              onClick={() => toggleFAQ(index)}
              aria-expanded={openIndex === index}
            >
              <span>{faq.question}</span>
              <span className="faq-icon">{openIndex === index ? '−' : '+'}</span>
            </button>
            {openIndex === index && (
              <div className="faq-answer">
                <p>{faq.answer}</p>
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
};

export default FAQ;
