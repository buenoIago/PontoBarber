import { HttpInterceptorFn } from '@angular/common/http';

/** Propaga um identificador de correlação em todas as requisições à API. */
export const interceptorCorrelacao: HttpInterceptorFn = (requisicao, proximo) =>
  proximo(
    requisicao.clone({
      setHeaders: { 'X-Correlation-Id': crypto.randomUUID() },
    }),
  );
