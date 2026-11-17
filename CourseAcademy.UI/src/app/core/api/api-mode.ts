import { InjectionToken, inject, isDevMode } from '@angular/core';
import { environment } from '../../../environments/environment';

/**
 * True when sign-in, users, roles and access come from the ASP.NET Core API instead of browser storage.
 * Features move to the API step by step; those not moved yet keep using the demo data.
 * A token (not a direct import) so tests can run both modes.
 */
export const API_MODE = new InjectionToken<boolean>('API_MODE', {
  providedIn: 'root',
  factory: () => environment.useApi,
});

/**
 * True where the demo set-up applies, so demo notices (demo accounts, simulated checkout) are shown:
 * browser-storage demo mode, or the API in development, which seeds the demo accounts and is the only place
 * the API allows the demo payment gateway. Production builds that use the API hide them.
 */
export const DEMO_SETUP = new InjectionToken<boolean>('DEMO_SETUP', {
  providedIn: 'root',
  factory: () => !inject(API_MODE) || isDevMode(),
});

/** The API base URL, from the environment. A token so tests can point it elsewhere. */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => environment.apiBaseUrl,
});
