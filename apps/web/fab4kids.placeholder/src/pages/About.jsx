import './About.css';

const About = () => {
  return (
    <div className="about-page">
      <div className="about-header">
        <h1>About Fab4Kids</h1>
        <p className="tagline">Inspiring Young Minds Through Play Since 2010</p>
      </div>

      <section className="about-section">
        <h2>Our Story</h2>
        <p>
          Fab4Kids was founded by a group of parents and educators who believed that learning
          should be fun, engaging, and accessible to every child. What started as a small
          online shop has grown into the UK's favourite destination for quality educational
          equipment and materials.
        </p>
        <p>
          We understand that the early years are crucial for development, and we're passionate
          about providing products that spark curiosity, encourage creativity, and build
          essential skills – all whilst keeping children entertained and engaged.
        </p>
      </section>

      <section className="about-section">
        <h2>Our Mission</h2>
        <p>
          To make high-quality educational resources accessible to families across the UK,
          supporting children's development through carefully curated products that combine
          learning with fun.
        </p>
      </section>

      <section className="values-section">
        <h2>Our Values</h2>
        <div className="values-grid">
          <div className="value-card">
            <span className="value-icon">🎯</span>
            <h3>Quality First</h3>
            <p>
              Every product is carefully selected and tested to ensure it meets our high
              standards for safety, durability, and educational value.
            </p>
          </div>

          <div className="value-card">
            <span className="value-icon">🌱</span>
            <h3>Sustainability</h3>
            <p>
              We prioritise eco-friendly products and sustainable practices, working with
              suppliers who share our commitment to the environment.
            </p>
          </div>

          <div className="value-card">
            <span className="value-icon">👶</span>
            <h3>Child-Centred</h3>
            <p>
              Everything we do is guided by what's best for children's development, safety,
              and happiness.
            </p>
          </div>

          <div className="value-card">
            <span className="value-icon">🤝</span>
            <h3>Support & Service</h3>
            <p>
              We're here to help! Our friendly team is always ready to assist with product
              recommendations and any questions you may have.
            </p>
          </div>
        </div>
      </section>

      <section className="about-section">
        <h2>Why Choose Us?</h2>
        <ul className="benefits-list">
          <li>
            <strong>Expert Curation:</strong> Our team of educators and parents carefully
            selects each product based on educational value and child appeal.
          </li>
          <li>
            <strong>Safety Assured:</strong> All products meet or exceed UK safety standards
            and are age-appropriate.
          </li>
          <li>
            <strong>Quality Guarantee:</strong> We stand behind every product with our
            30-day satisfaction guarantee.
          </li>
          <li>
            <strong>Fast, Free Delivery:</strong> Free UK delivery on orders over £25, with
            quick dispatch times.
          </li>
          <li>
            <strong>Excellent Customer Service:</strong> Our UK-based team is here to help
            with any questions or concerns.
          </li>
          <li>
            <strong>Educational Resources:</strong> Free activity guides and educational tips
            with many of our products.
          </li>
        </ul>
      </section>

      <section className="about-section">
        <h2>Supporting Schools & Nurseries</h2>
        <p>
          We work with schools, nurseries, and childcare providers across the UK, offering
          special bulk pricing and dedicated account management. If you're an educator or
          run an educational institution, please get in touch to learn more about our
          educational services programme.
        </p>
      </section>

      <section className="team-section">
        <h2>Our Commitment to You</h2>
        <p>
          At Fab4Kids, we believe that every child deserves the opportunity to learn, grow,
          and thrive. We're committed to providing the tools and resources that make this
          possible, backed by exceptional customer service and a genuine passion for
          children's education.
        </p>
        <p>
          Thank you for choosing Fab4Kids. Together, we're building brighter futures, one
          toy at a time!
        </p>
      </section>
    </div>
  );
};

export default About;
