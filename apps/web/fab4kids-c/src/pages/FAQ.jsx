import { useState } from 'react';
import './FAQ.css';

const FAQ = () => {
  const [openIndex, setOpenIndex] = useState(null);

  const faqs = [
    {
      question: 'What does Version 2 of Fab4Kids focus on?',
      answer: 'This version focuses on two things: downloadable Montessori-style worksheet packs and physical Montessori materials. The catalogue is organised to help adults combine hands-on apparatus with follow-up paper work.'
    },
    {
      question: 'Are worksheet products delivered instantly?',
      answer: 'Yes. Digital worksheet packs are delivered immediately after checkout as downloadable files. You will receive access details by email and can print as needed for your environment.'
    },
    {
      question: 'How are physical Montessori materials shipped?',
      answer: 'Physical items are packed and dispatched from our UK warehouse. Standard tracked delivery is available, and larger classroom bundles may ship in multiple parcels to protect wooden components.'
    },
    {
      question: 'Can I return digital downloads?',
      answer: 'Because downloadable files are delivered instantly, digital products are generally non-returnable once accessed. If a file is corrupted or incomplete, we will replace it immediately.'
    },
    {
      question: 'Can I track physical orders?',
      answer: 'Yes. You will receive tracking details once your parcel leaves the warehouse. Contact support if the tracking does not update within 48 hours.'
    },
    {
      question: 'Do your materials follow authentic Montessori sequences?',
      answer: 'Our physical equipment and worksheet language are designed around common Montessori classroom progressions. We include setup notes to help adults present each material clearly and consistently.'
    },
    {
      question: 'What is the Maria Montessori theme?',
      answer: 'The Maria Montessori theme is a contemporary interpretation of how the site might feel if Maria Montessori were alive today: calm tones, practical guidance, and a focus on purposeful independent work.'
    },
    {
      question: 'Can schools and nurseries buy in bulk?',
      answer: 'Yes. We support institutional orders, recurring restocks, and starter room kits. Contact us for pricing and lead times.'
    },
    {
      question: 'How can I get setup advice?',
      answer: 'Email our team with the child age range and your available shelf space. We can suggest a first sequence and matching worksheet pack.'
    },
    {
      question: 'What payment methods do you accept?',
      answer: 'We accept major debit and credit cards, PayPal, and Apple Pay. All transactions are securely processed.'
    },
    {
      question: 'How can I contact customer service?',
      answer: 'You can reach us at support@fab4kids.co.uk or 0800 123 4567. We usually reply to emails within one business day.'
    },
    {
      question: 'Do you ship outside the UK?',
      answer: 'Digital downloads are available globally. Physical shipping is currently UK-focused while we expand logistics options.'
    }
  ];

  const toggleFAQ = (index) => {
    setOpenIndex(openIndex === index ? null : index);
  };

  return (
    <div className="faq-page">
      <div className="faq-header">
        <h1>Frequently Asked Questions</h1>
        <p>Answers about worksheets, Montessori materials, delivery, and support</p>
      </div>

      <div className="faq-intro">
        <p>
          Can't find what you're looking for? Our friendly customer service team is here to help!
          Contact us at <a href="mailto:support@fab4kids.co.uk">support@fab4kids.co.uk</a> or call{' '}
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
