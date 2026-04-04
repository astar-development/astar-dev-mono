import type { SchemaTypeDefinition } from 'sanity';

export const productSchema: SchemaTypeDefinition = {
  name: 'product',
  type: 'document',
  title: 'Product',
  fields: [
    {
      name: 'title',
      type: 'string',
      title: 'Title',
      validation: (rule) => rule.required(),
    },
    {
      name: 'slug',
      type: 'slug',
      title: 'Slug',
      options: { source: 'title' },
    },
    {
      name: 'description',
      type: 'text',
      title: 'Description',
    },
    {
      name: 'subject',
      type: 'string',
      title: 'Subject',
      options: {
        list: [
          { title: 'Maths', value: 'maths' },
          { title: 'English', value: 'english' },
          { title: 'Science', value: 'science' },
          { title: 'History', value: 'history' },
          { title: 'Geography', value: 'geography' },
        ],
      },
    },
    {
      name: 'keyStages',
      type: 'array',
      title: 'Key Stages',
      of: [{ type: 'string' }],
      options: {
        list: [
          { title: 'KS1', value: 'ks1' },
          { title: 'KS2', value: 'ks2' },
          { title: 'KS3', value: 'ks3' },
          { title: 'KS4', value: 'ks4' },
        ],
      },
    },
    {
      name: 'fileFormats',
      type: 'array',
      title: 'File Formats',
      of: [{ type: 'string' }],
      options: {
        list: [
          { title: 'PDF', value: 'pdf' },
          { title: 'Word', value: 'word' },
          { title: 'PowerPoint', value: 'powerpoint' },
        ],
      },
    },
    {
      name: 'price',
      type: 'number',
      title: 'Price',
      description: 'Price in pence (GBP)',
    },
    {
      name: 'blobPaths',
      type: 'array',
      title: 'Blob Paths',
      of: [{ type: 'string' }],
    },
    {
      name: 'stripeProductId',
      type: 'string',
      title: 'Stripe Product ID',
    },
    {
      name: 'stripePriceId',
      type: 'string',
      title: 'Stripe Price ID',
    },
    {
      name: 'image',
      type: 'image',
      title: 'Image',
      fields: [
        {
          name: 'alt',
          type: 'string',
          title: 'Alt Text',
        },
      ],
    },
    {
      name: 'publishedAt',
      type: 'datetime',
      title: 'Published At',
    },
  ],
};
