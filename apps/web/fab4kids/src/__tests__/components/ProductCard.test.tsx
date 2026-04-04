import { render, screen } from '@testing-library/react';
import { ProductCard } from '@/components/ProductCard.tsx';
import type { KeyStage, Subject, FileFormat } from '@/types/index.ts';

const baseProduct = {
  _id: 'prod-001',
  title: 'Fractions Worksheet Pack',
  slug: 'fractions-worksheet-pack',
  subject: 'maths' as Subject,
  keyStages: ['ks2', 'ks3'] as KeyStage[],
  fileFormats: ['pdf', 'word'] as FileFormat[],
  price: 399,
  image: undefined,
};

describe('ProductCard', () => {
  beforeEach(() => vi.resetAllMocks());

  it('renders the product title as a link to the product page', () => {
    render(<ProductCard product={baseProduct} />);

    const titleLink = screen.getByRole('heading', { name: /fractions worksheet pack/i })
      .querySelector('a');

    expect(titleLink).toHaveAttribute('href', '/product/fractions-worksheet-pack');
  });

  it('renders a "View resource" link pointing to the product page', () => {
    render(<ProductCard product={baseProduct} />);

    const viewLink = screen.getByRole('link', { name: /view resource/i });

    expect(viewLink).toHaveAttribute('href', '/product/fractions-worksheet-pack');
  });

  it('formats a price in pence as pounds and pence', () => {
    render(<ProductCard product={baseProduct} />);

    expect(screen.getByText('£3.99')).toBeInTheDocument();
  });

  it('formats a price with zero pence correctly', () => {
    render(<ProductCard product={{ ...baseProduct, price: 500 }} />);

    expect(screen.getByText('£5.00')).toBeInTheDocument();
  });

  it('renders the subject badge with the correct label', () => {
    const { container } = render(<ProductCard product={baseProduct} />);

    const subjectBadge = container.querySelector('.badge--subject');

    expect(subjectBadge).toHaveTextContent('Maths');
  });

  it('renders one badge per key stage', () => {
    render(<ProductCard product={baseProduct} />);

    expect(screen.getByText('KS2')).toBeInTheDocument();
    expect(screen.getByText('KS3')).toBeInTheDocument();
  });

  it('renders one badge per file format', () => {
    render(<ProductCard product={baseProduct} />);

    expect(screen.getByText('PDF')).toBeInTheDocument();
    expect(screen.getByText('Word')).toBeInTheDocument();
  });

  it('renders the product image when provided', () => {
    const productWithImage = {
      ...baseProduct,
      image: { url: 'https://cdn.example.com/img.jpg', alt: 'Fractions pack cover' },
    };
    const { container } = render(<ProductCard product={productWithImage} />);

    const img = container.querySelector<HTMLImageElement>('.product-card__image');

    expect(img).not.toBeNull();
    expect(img?.src).toBe('https://cdn.example.com/img.jpg');
    expect(img?.alt).toBe('Fractions pack cover');
  });

  it('renders a subject-coloured placeholder when no image is provided', () => {
    const { container } = render(<ProductCard product={baseProduct} />);

    expect(container.querySelector('.product-card__placeholder')).toBeInTheDocument();
    expect(container.querySelector('.product-card__image')).not.toBeInTheDocument();
  });
});
