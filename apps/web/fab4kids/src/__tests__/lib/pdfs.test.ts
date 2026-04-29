import { vi } from 'vitest';

vi.mock('@/data/pdfs.json', () => ({
  default: {
    categories: [
      {
        id: 1,
        name: 'Maths',
        subcategories: [
          {
            id: 1,
            name: 'KS1',
            files: [
              { id: 1, name: 'Fractions Worksheet', url: 'pdfs/fractions.pdf', price: 1.23 },
              { id: 2, name: 'Number Bonds', url: 'pdfs/number-bonds.pdf', price: 1.23 },
            ],
          },
          { id: 2, name: 'KS2', files: [] },
        ],
      },
      {
        id: 2,
        name: 'English',
        subcategories: [
          {
            id: 3,
            name: 'KS1',
            files: [{ id: 3, name: 'Reading Comprehension', url: 'pdfs/reading.pdf', price: 1.23 }],
          },
        ],
      },
    ],
  },
}));

import { getAllCategories, getCategoryBySlug, getSubcategoryBySlug, toSlug } from '@/lib/pdfs.ts';

describe('toSlug', () => {
  it('when_given_single_word_then_lowercases', () => {
    expect(toSlug('Maths')).toBe('maths');
  });

  it('when_given_multi_word_then_lowercases_and_hyphenates', () => {
    expect(toSlug('Key Stage 1')).toBe('key-stage-1');
  });

  it('when_given_already_lowercase_then_returns_unchanged', () => {
    expect(toSlug('ks1')).toBe('ks1');
  });

  it('when_given_uppercase_abbreviation_then_lowercases', () => {
    expect(toSlug('KS1')).toBe('ks1');
  });
});

describe('getAllCategories', () => {
  it('when_called_then_returns_all_categories', () => {
    const result = getAllCategories();

    expect(result).toHaveLength(2);
  });

  it('when_called_then_categories_have_correct_names', () => {
    const result = getAllCategories();

    expect(result[0].name).toBe('Maths');
    expect(result[1].name).toBe('English');
  });

  it('when_called_then_categories_include_subcategories', () => {
    const result = getAllCategories();

    expect(result[0].subcategories).toHaveLength(2);
    expect(result[1].subcategories).toHaveLength(1);
  });
});

describe('getCategoryBySlug', () => {
  it('when_slug_matches_category_then_returns_category', () => {
    const result = getCategoryBySlug('maths');

    expect(result).not.toBeUndefined();
    expect(result?.name).toBe('Maths');
  });

  it('when_slug_matches_second_category_then_returns_correct_category', () => {
    const result = getCategoryBySlug('english');

    expect(result).not.toBeUndefined();
    expect(result?.name).toBe('English');
  });

  it('when_slug_does_not_match_then_returns_undefined', () => {
    const result = getCategoryBySlug('science');

    expect(result).toBeUndefined();
  });

  it('when_slug_is_empty_then_returns_undefined', () => {
    const result = getCategoryBySlug('');

    expect(result).toBeUndefined();
  });
});

describe('getSubcategoryBySlug', () => {
  it('when_category_and_subcategory_match_then_returns_subcategory', () => {
    const result = getSubcategoryBySlug('maths', 'ks1');

    expect(result).not.toBeUndefined();
    expect(result?.name).toBe('KS1');
  });

  it('when_subcategory_has_files_then_files_are_returned', () => {
    const result = getSubcategoryBySlug('maths', 'ks1');

    expect(result?.files).toHaveLength(2);
    expect(result?.files[0].name).toBe('Fractions Worksheet');
  });

  it('when_subcategory_has_no_files_then_returns_empty_files_array', () => {
    const result = getSubcategoryBySlug('maths', 'ks2');

    expect(result).not.toBeUndefined();
    expect(result?.files).toHaveLength(0);
  });

  it('when_subcategory_slug_does_not_match_then_returns_undefined', () => {
    const result = getSubcategoryBySlug('maths', 'ks4');

    expect(result).toBeUndefined();
  });

  it('when_category_slug_does_not_match_then_returns_undefined', () => {
    const result = getSubcategoryBySlug('science', 'ks1');

    expect(result).toBeUndefined();
  });
});
