import type { FileFormat, KeyStage, Subject } from '@/types/index.ts';

export const SUBJECTS: Subject[] = ['maths', 'english', 'science', 'history', 'geography'];

export const KEY_STAGES: KeyStage[] = ['ks1', 'ks2', 'ks3', 'ks4'];

export const KEY_STAGE_LABELS: Record<KeyStage, string> = {
  ks1: 'KS1',
  ks2: 'KS2',
  ks3: 'KS3',
  ks4: 'KS4',
} satisfies Record<KeyStage, string>;

export const SUBJECT_LABELS: Record<Subject, string> = {
  maths: 'Maths',
  english: 'English',
  science: 'Science',
  history: 'History',
  geography: 'Geography',
} satisfies Record<Subject, string>;

export const FILE_FORMAT_LABELS: Record<FileFormat, string> = {
  pdf: 'PDF',
  word: 'Word',
  powerpoint: 'PowerPoint',
} satisfies Record<FileFormat, string>;

export const SIGNED_URL_TTL_MINUTES = 15;
