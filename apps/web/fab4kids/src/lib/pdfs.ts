import pdfsData from '@/data/pdfs.json';
import type { PdfCategory, PdfSubcategory } from '@/types/index.ts';

export function toSlug(name: string): string {
  return name.toLowerCase().replace(/\s+/g, '-');
}

export function getAllCategories(): PdfCategory[] {
  return pdfsData.categories as PdfCategory[];
}

export function getCategoryBySlug(slug: string): PdfCategory | undefined {
  return (pdfsData.categories as PdfCategory[]).find((c) => toSlug(c.name) === slug);
}

export function getSubcategoryBySlug(categorySlug: string, subcategorySlug: string): PdfSubcategory | undefined {
  return getCategoryBySlug(categorySlug)?.subcategories.find((s) => toSlug(s.name) === subcategorySlug);
}
