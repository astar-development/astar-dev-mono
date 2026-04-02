import { defineMiddleware } from 'astro:middleware';
import { trackTrace } from './lib/telemetry';

export const onRequest = defineMiddleware(async (context, next) => {
  const start = Date.now();
  const response = await next();
  const duration = Date.now() - start;

  trackTrace('page/view', {
    method: context.request.method,
    path: context.url.pathname,
    status: String(response.status),
    durationMs: String(duration),
  });

  return response;
});
