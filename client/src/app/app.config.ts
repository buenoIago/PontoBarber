import { provideHttpClient, withInterceptors, withXsrfConfiguration } from '@angular/common/http';
import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';

import { interceptorCorrelacao } from './core/http/interceptor-correlacao';
import { interceptorErro } from './core/http/interceptor-erro';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(
      withInterceptors([interceptorCorrelacao, interceptorErro]),
      withXsrfConfiguration({ cookieName: '__Host-pb.xsrf', headerName: 'X-CSRF-TOKEN' }),
    ),
  ],
};
