import type { SchemaTypeDefinition } from 'sanity';

export const subjectSchema: SchemaTypeDefinition = {
  name: 'subject',
  type: 'document',
  title: 'Subject',
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
      name: 'heroImage',
      type: 'image',
      title: 'Hero Image',
      fields: [
        {
          name: 'alt',
          type: 'string',
          title: 'Alt Text',
        },
      ],
    },
  ],
};
