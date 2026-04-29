import { useState } from 'react';
import { addToCart } from '@/lib/cartStore.ts';
import type { PdfFile } from '@/types/index.ts';

interface PdfCardProps {
  file: PdfFile;
}

export function PdfCard({ file }: PdfCardProps): React.JSX.Element {
  const [added, setAdded] = useState(false);

  function handleAddToBasket(): void {
    addToCart({ productId: String(file.id), slug: String(file.id), title: file.name, price: Math.round(file.price * 100), stripePriceId: '' });
    setAdded(true);
    setTimeout(() => setAdded(false), 1500);
  }

  return (
    <article className="pdf-card">
      <h3 className="pdf-card__name">{file.name}</h3>
      <p className="pdf-card__price">£{file.price.toFixed(2)}</p>
      <div className="pdf-card__actions">
        <a
          href={file.url}
          className="btn btn--secondary"
          target="_blank"
          rel="noopener noreferrer"
          aria-label={`View ${file.name}`}
        >
          View
        </a>
        <button
          type="button"
          className={`btn btn--primary${added ? ' btn--added' : ''}`}
          aria-label={added ? 'Added to basket' : `Add ${file.name} to basket`}
          onClick={handleAddToBasket}
        >
          {added ? 'Added ✓' : 'Add to basket'}
        </button>
      </div>
    </article>
  );
}
