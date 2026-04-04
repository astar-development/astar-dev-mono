import { createClient } from '@sanity/client';
import type { Product, SubjectInfo, Subject, KeyStage } from '@/types/index.ts';

const projectId = (import.meta.env.SANITY_PROJECT_ID as string | undefined) ?? '';

const isSanityConfigured = projectId.length > 0;

const client = isSanityConfigured
  ? createClient({
      projectId,
      dataset: (import.meta.env.SANITY_DATASET as string | undefined) ?? 'production',
      apiVersion: '2024-01-01',
      useCdn: true,
    })
  : null;

const PRODUCT_FIELDS = `
  _id,
  title,
  "slug": slug.current,
  description,
  subject,
  keyStages,
  fileFormats,
  price,
  blobPaths,
  stripeProductId,
  stripePriceId,
  image { "url": asset->url, alt },
  publishedAt
`;

const SUBJECT_FIELDS = `
  _id,
  title,
  "slug": slug.current,
  description,
  heroImage { "url": asset->url, alt }
`;

export async function getAllProducts(): Promise<Product[]> {
  if (!client) return [];

  return client.fetch<Product[]>(`*[_type == "product"] | order(publishedAt desc) { ${PRODUCT_FIELDS} }`);
}

export async function getProductBySlug(slug: string): Promise<Product | null> {
  if (!client) return null;

  return client.fetch<Product | null>(
    `*[_type == "product" && slug.current == $slug][0] { ${PRODUCT_FIELDS} }`,
    { slug },
  );
}

export async function getProductsBySubject(subject: Subject): Promise<Product[]> {
  if (!client) return [];

  return client.fetch<Product[]>(
    `*[_type == "product" && subject == $subject] | order(publishedAt desc) { ${PRODUCT_FIELDS} }`,
    { subject },
  );
}

export async function getProductsBySubjectAndKS(subject: Subject, ks: KeyStage): Promise<Product[]> {
  if (!client) return [];

  return client.fetch<Product[]>(
    `*[_type == "product" && subject == $subject && $ks in keyStages] | order(publishedAt desc) { ${PRODUCT_FIELDS} }`,
    { subject, ks },
  );
}

export async function getAllSubjects(): Promise<SubjectInfo[]> {
  if (!client) return [];

  return client.fetch<SubjectInfo[]>(`*[_type == "subject"] | order(title asc) { ${SUBJECT_FIELDS} }`);
}

export async function getSubjectBySlug(slug: Subject): Promise<SubjectInfo | null> {
  if (!client) return null;

  return client.fetch<SubjectInfo | null>(
    `*[_type == "subject" && slug.current == $slug][0] { ${SUBJECT_FIELDS} }`,
    { slug },
  );
}
