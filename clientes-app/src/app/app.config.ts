import { ApplicationConfig, provideBrowserGlobalErrorListeners, LOCALE_ID } from '@angular/core';
import { provideRouter, withViewTransitions } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { registerLocaleData } from '@angular/common';
import localeEs from '@angular/common/locales/es';

import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';
import { sessionInterceptor } from './interceptors/session.interceptor';
import { retryInterceptor } from './interceptors/retry.interceptor';

registerLocaleData(localeEs);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    { provide: LOCALE_ID, useValue: 'es' },
    provideHttpClient(
      withFetch(),
      withInterceptors([authInterceptor, sessionInterceptor, retryInterceptor])
    ),
    provideRouter(
      routes,
      withViewTransitions({ skipInitialTransition: true })
    )
  ]
};
