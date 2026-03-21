import './About.css';

const About = () => {
  return (
    <div className="about-page">
      <div className="about-header">
        <h1>About Fab4Kids Studio</h1>
        <p className="tagline">A Montessori-first platform for downloadable lessons and physical materials</p>
      </div>

      <section className="about-section">
        <h2>Version 2 Story</h2>
        <p>
          Fab4Kids Studio started as a toy and activity shop, but this second version of our
          site is built for families and educators specifically looking for Montessori learning
          structure. We now pair printable worksheet systems with authentic physical apparatus,
          so children can move between hand work and paper work naturally.
        </p>
        <p>
          Every collection is arranged to support independent work cycles, calm concentration,
          and real-world practical life routines in both home and classroom environments.
        </p>
      </section>

      <section className="about-section">
        <h2>Our Mission</h2>
        <p>
          To make high-quality Montessori resources accessible across the UK by combining digital
          convenience with durable physical equipment that can be used for years.
        </p>
      </section>

      <section className="values-section">
        <h2>Our Values</h2>
        <div className="values-grid">
          <div className="value-card">
            <span className="value-icon">🧭</span>
            <h3>Prepared Environment</h3>
            <p>
              We design shelves and bundles as complete learning environments, not isolated products.
            </p>
          </div>

          <div className="value-card">
            <span className="value-icon">🪵</span>
            <h3>Material Integrity</h3>
            <p>
              We prioritise tactile, proportion-accurate materials and practical worksheet formats
              that support repetition without visual overload.
            </p>
          </div>

          <div className="value-card">
            <span className="value-icon">👁️</span>
            <h3>Observation-Led</h3>
            <p>
              We build resources around what children actually do independently, not around trends.
            </p>
          </div>

          <div className="value-card">
            <span className="value-icon">🏫</span>
            <h3>School + Home Ready</h3>
            <p>
              Our resources are written so parents, guides, and assistants can all present the same lesson clearly.
            </p>
          </div>
        </div>
      </section>

      <section className="about-section">
        <h2>Why Choose This Version?</h2>
        <ul className="benefits-list">
          <li>
            <strong>Worksheet + Apparatus Matching:</strong> Digital packs connect directly to physical material lessons.
          </li>
          <li>
            <strong>Classroom Terminology:</strong> Naming and sequence align with Montessori practice.
          </li>
          <li>
            <strong>Flexible Delivery:</strong> Download in minutes or receive physical sets with tracked delivery.
          </li>
          <li>
            <strong>Quality Guarantee:</strong> 30-day confidence period on all eligible physical materials.
          </li>
          <li>
            <strong>Supportive Team:</strong> UK-based help desk for setup and implementation questions.
          </li>
          <li>
            <strong>Educator Friendly:</strong> Bulk and institutional support for schools and nurseries.
          </li>
        </ul>
      </section>

      <section className="about-section">
        <h2>If Maria Montessori Were Here Today</h2>
        <p>
          The Maria Montessori theme imagines this platform as her modern digital schoolhouse:
          precise materials, elegant simplicity, and practical tools that respect the child.
          It is not historical reenactment; it is a present-day interpretation of her core principles
          adapted for current families and classrooms.
        </p>
      </section>

      <section className="team-section">
        <h2>Our Commitment to You</h2>
        <p>
          We are committed to helping adults set up calm, clear learning environments where children
          can work independently with purpose.
        </p>
        <p>
          Thank you for building with us. This version of Fab4Kids is designed to feel more like a
          studio for serious early learning than a generic toy catalogue.
        </p>
      </section>
    </div>
  );
};

export default About;
