import { useState } from 'react';
import { addToCart } from '@/lib/cartStore.ts';

interface AddToCartButtonProps {
  productId: string;
  slug: string;
  title: string;
  price: number;
  stripePriceId: string;
}

export function AddToCartButton({ productId, slug, title, price, stripePriceId }: AddToCartButtonProps): React.JSX.Element {
  const [added, setAdded] = useState(false);

  function handleClick(): void {
    addToCart({ productId, slug, title, price, stripePriceId });
    setAdded(true);
    setTimeout(() => setAdded(false), 1500);
  }

  return (
    <button
      type="button"
      onClick={handleClick}
      className={`btn-add-to-cart${added ? ' added' : ''}`}
      aria-label={added ? 'Added to cart' : `Add ${title} to cart`}
    >
      {added ? 'Added ✓' : 'Add to cart'}
    </button>
  );
}
