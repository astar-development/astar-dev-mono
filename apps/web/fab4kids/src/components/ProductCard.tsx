import React from 'react';
import type { FileFormat, KeyStage, Subject } from '@/types/index.ts';
import { SUBJECT_LABELS, KEY_STAGE_LABELS, FILE_FORMAT_LABELS } from '@/lib/constants.ts';

interface ProductCardProps {
  product: {
    _id: string;
    title: string;
    slug: string;
    subject: Subject;
    keyStages: KeyStage[];
    fileFormats: FileFormat[];
    price: number;
    image?: { url: string; alt: string };
  };
}

const SUBJECT_PLACEHOLDER_COLOURS: Record<Subject, string> = {
  maths: '#4f46e5',
  english: '#7c3aed',
  science: '#15803d',
  history: '#b45309',
  geography: '#0369a1',
} satisfies Record<Subject, string>;

const KS_BADGE_VARS: Record<KeyStage, string> = {
  ks1: 'var(--color-badge-ks1)',
  ks2: 'var(--color-badge-ks2)',
  ks3: 'var(--color-badge-ks3)',
  ks4: 'var(--color-badge-ks4)',
} satisfies Record<KeyStage, string>;

function formatPrice(pence: number): string {
  const pounds = Math.floor(pence / 100);
  const remainder = pence % 100;
  const paddedPence = remainder.toString().padStart(2, '0');

  return `£${pounds}.${paddedPence}`;
}

export function ProductCard({ product }: ProductCardProps): React.JSX.Element {
  const { title, slug, subject, keyStages, fileFormats, price, image } = product;
  const productUrl = `/product/${slug}`;
  const placeholderColour = SUBJECT_PLACEHOLDER_COLOURS[subject];

  return (
    <article className="product-card">
      <a href={productUrl} className="product-card__image-link" tabIndex={-1} aria-hidden="true">
        {image ? (
          <img
            src={image.url}
            alt={image.alt}
            className="product-card__image"
            loading="lazy"
            decoding="async"
          />
        ) : (
          <div
            className="product-card__placeholder"
            style={{ backgroundColor: placeholderColour }}
            aria-hidden="true"
          >
            <span className="product-card__placeholder-label">{SUBJECT_LABELS[subject]}</span>
          </div>
        )}
      </a>

      <div className="product-card__body">
        <div className="product-card__badges">
          <span className="badge badge--subject">{SUBJECT_LABELS[subject]}</span>
          {keyStages.map((ks) => (
            <span
              key={ks}
              className="badge badge--ks"
              style={{ backgroundColor: KS_BADGE_VARS[ks] }}
            >
              {KEY_STAGE_LABELS[ks]}
            </span>
          ))}
          {fileFormats.map((fmt) => (
            <span key={fmt} className="badge badge--format">
              {FILE_FORMAT_LABELS[fmt]}
            </span>
          ))}
        </div>

        <h3 className="product-card__title">
          <a href={productUrl}>{title}</a>
        </h3>

        <div className="product-card__footer">
          <span className="product-card__price">{formatPrice(price)}</span>
          <a href={productUrl} className="btn btn--primary">
            View resource
          </a>
        </div>
      </div>
    </article>
  );
}
