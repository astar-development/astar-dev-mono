import type { SchemaTypeDefinition } from 'sanity';
import { productSchema } from './product.ts';
import { subjectSchema } from './subject.ts';

export const schemas: SchemaTypeDefinition[] = [productSchema, subjectSchema];
