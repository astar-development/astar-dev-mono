export type KeyStage = 'ks1' | 'ks2' | 'ks3' | 'ks4';
export type Subject = 'maths' | 'english' | 'science' | 'history' | 'geography';
export type FileFormat = 'pdf' | 'word' | 'powerpoint';

export interface Product {
  _id: string;
  title: string;
  slug: string;
  description: string;
  subject: Subject;
  keyStages: KeyStage[];
  fileFormats: FileFormat[];
  price: number;
  blobPaths: string[];
  stripeProductId: string;
  stripePriceId: string;
  image?: {
    url: string;
    alt: string;
  };
  publishedAt: string;
}

export interface SubjectInfo {
  _id: string;
  title: string;
  slug: Subject;
  description: string;
  heroImage?: {
    url: string;
    alt: string;
  };
}

export interface CartItem {
  productId: string;
  slug: string;
  title: string;
  price: number;
  stripePriceId: string;
  quantity: number;
}

export interface DeliveryLink {
  productTitle: string;
  url: string;
  expiresAt: string;
}
