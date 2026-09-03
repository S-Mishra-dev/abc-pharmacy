import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideHttpClient } from '@angular/common/http';

/**
 * Angular 22 apps are zoneless by default (no zone.js dependency).
 * The older provideExperimentalZonelessChangeDetection() API is no longer required.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(),
  ],
};